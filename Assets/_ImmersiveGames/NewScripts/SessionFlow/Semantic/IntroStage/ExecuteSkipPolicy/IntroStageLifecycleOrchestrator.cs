#nullable enable
using System;
using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Orchestration.GameLoop.IntroStage;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Runtime;
using UnityEngine.SceneManagement;

namespace ImmersiveGames.GameJam2025.Orchestration.GameLoop.IntroStage.Runtime
{
    public sealed class IntroStageLifecycleOrchestrator : IDisposable
    {
        private readonly IIntroStageLifecycleStateService _stateService;
        private readonly IIntroStageLifecycleDispatchService _dispatchService;
        private readonly EventBinding<IntroStageEntryEvent> _introStageEntryBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;

        public IntroStageLifecycleOrchestrator(
            IIntroStageLifecycleStateService stateService,
            IIntroStageLifecycleDispatchService dispatchService)
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _dispatchService = dispatchService ?? throw new ArgumentNullException(nameof(dispatchService));
            _introStageEntryBinding = new EventBinding<IntroStageEntryEvent>(OnIntroStageEntry);
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            EventBus<IntroStageEntryEvent>.Register(_introStageEntryBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);
        }

        public void Dispose()
        {
            EventBus<IntroStageEntryEvent>.Unregister(_introStageEntryBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
        }

        private void OnIntroStageEntry(IntroStageEntryEvent evt)
        {
            if (!evt.Session.IsValid)
            {
                HardFailFastH1.Trigger(typeof(IntroStageLifecycleOrchestrator),
                    "[FATAL][H1][IntroStage] Invalid IntroStageEntryEvent received.");
            }

            if (!_stateService.TryAcceptIntroStageEntry(evt, out bool shouldDefer))
            {
                return;
            }

            if (shouldDefer)
            {
                return;
            }

            _dispatchService.DispatchIntroStage(
                evt.Source,
                evt.Session,
                evt.RouteKind,
                Normalize(evt.Session.Reason));
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!_stateService.TryReleasePendingGameplayIntro(evt, out IntroStagePendingGameplayIntro pendingGameplayIntro))
            {
                return;
            }

            _dispatchService.DispatchIntroStage(
                pendingGameplayIntro.Source,
                pendingGameplayIntro.Session,
                evt.context.RouteKind,
                pendingGameplayIntro.Reason);
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }

    public interface IIntroStageLifecycleStateService
    {
        bool TryAcceptIntroStageEntry(IntroStageEntryEvent evt, out bool shouldDefer);
        bool TryReleasePendingGameplayIntro(SceneTransitionCompletedEvent evt, out IntroStagePendingGameplayIntro pendingGameplayIntro);
    }

    public interface IIntroStageLifecycleDispatchService
    {
        void DispatchIntroStage(string source, IntroStageSession session, SceneRouteKind routeKind, string reason);
    }

    public readonly struct IntroStagePendingGameplayIntro
    {
        public IntroStagePendingGameplayIntro(IntroStageSession session, string source, string reason)
        {
            Session = session;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public IntroStageSession Session { get; }
        public string Source { get; }
        public string Reason { get; }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageLifecycleStateService : IIntroStageLifecycleStateService
    {
        private readonly object _sync = new();
        private int _lastProcessedPhaseLocalEntrySequence;
        private IntroStageSession _pendingGameplaySession;
        private string _pendingGameplaySource = string.Empty;
        private string _pendingGameplayReason = string.Empty;
        private bool _hasPendingGameplayIntro;

        public bool TryAcceptIntroStageEntry(IntroStageEntryEvent evt, out bool shouldDefer)
        {
            shouldDefer = false;

            if (!TryAdvanceDedupe(evt.Session.PhaseLocalEntrySequence, evt.Session.SelectionVersion, evt.Source))
            {
                return false;
            }

            if (ShouldDeferGameplayIntro(evt))
            {
                QueuePendingGameplayIntro(evt);
                shouldDefer = true;
            }

            return true;
        }

        public bool TryReleasePendingGameplayIntro(
            SceneTransitionCompletedEvent evt,
            out IntroStagePendingGameplayIntro pendingGameplayIntro)
        {
            pendingGameplayIntro = default;

            if (evt.context.RouteKind != SceneRouteKind.Gameplay)
            {
                return false;
            }

            IntroStageSession pendingSession;
            string pendingSource;
            string pendingReason;

            lock (_sync)
            {
                if (!_hasPendingGameplayIntro)
                {
                    return false;
                }

                pendingSession = _pendingGameplaySession;
                pendingSource = _pendingGameplaySource;
                pendingReason = _pendingGameplayReason;
                _hasPendingGameplayIntro = false;
                _pendingGameplaySession = default;
                _pendingGameplaySource = string.Empty;
                _pendingGameplayReason = string.Empty;
            }

            DebugUtility.Log<IntroStageLifecycleStateService>(
                $"[OBS][IntroStage] IntroStageReleasedOnSceneTransitionCompleted source='{pendingSource}' contentName='{DescribeSessionContentName(pendingSession)}' v='{pendingSession.SelectionVersion}' reason='{Normalize(pendingReason)}' sessionSignature='{Normalize(pendingSession.SessionSignature)}' routeKind='{evt.context.RouteKind}' sceneTransitionSignature='{SceneTransitionSignature.Compute(evt.context)}'.",
                DebugUtility.Colors.Info);

            pendingGameplayIntro = new IntroStagePendingGameplayIntro(
                pendingSession,
                pendingSource,
                pendingReason);
            return true;
        }

        private bool TryAdvanceDedupe(int phaseLocalEntrySequence, int selectionVersion, string source)
        {
            if (phaseLocalEntrySequence <= 0)
            {
                HardFailFastH1.Trigger(typeof(IntroStageLifecycleStateService),
                    "[FATAL][H1][IntroStage] PhaseLocalEntrySequence is required to dedupe intro stage reentry.");
            }

            if (phaseLocalEntrySequence <= _lastProcessedPhaseLocalEntrySequence)
            {
                DebugUtility.LogVerbose<IntroStageLifecycleStateService>(
                    $"[IntroStage] skipped reason='dedupe_phase_local_entry_sequence' selectionVersion='{selectionVersion}' source='{source}' phaseLocalEntrySequence='{phaseLocalEntrySequence}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            _lastProcessedPhaseLocalEntrySequence = phaseLocalEntrySequence;
            return true;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static string DescribeSessionContentName(IntroStageSession session)
        {
            if (session.PhaseDefinitionRef != null)
            {
                return session.PhaseDefinitionRef.name;
            }

            return "<none>";
        }

        private static bool ShouldDeferGameplayIntro(IntroStageEntryEvent evt)
        {
            return evt.RouteKind == SceneRouteKind.Gameplay
                   && string.Equals(evt.Source, "GameplaySessionFlow", StringComparison.Ordinal);
        }

        private void QueuePendingGameplayIntro(IntroStageEntryEvent evt)
        {
            lock (_sync)
            {
                _pendingGameplaySession = evt.Session;
                _pendingGameplaySource = evt.Source;
                _pendingGameplayReason = Normalize(evt.Session.Reason);
                _hasPendingGameplayIntro = true;
            }

            DebugUtility.Log<IntroStageLifecycleStateService>(
                $"[OBS][IntroStage] IntroStageDeferred source='{evt.Source}' contentName='{DescribeSessionContentName(evt.Session)}' v='{evt.Session.SelectionVersion}' hasIntroStage='{evt.Session.HasIntroStage}' reason='{Normalize(evt.Session.Reason)}' sessionSignature='{Normalize(evt.Session.SessionSignature)}' gate='SceneTransitionCompletedEvent'.",
                DebugUtility.Colors.Info);
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageLifecycleDispatchService : IIntroStageLifecycleDispatchService
    {
        private readonly IIntroStagePresenterRegistry _presenterRegistry;
        private readonly IIntroStageCoordinator _introStageCoordinator;

        public IntroStageLifecycleDispatchService(
            IIntroStagePresenterRegistry presenterRegistry,
            IIntroStageCoordinator introStageCoordinator)
        {
            _presenterRegistry = presenterRegistry ?? throw new ArgumentNullException(nameof(presenterRegistry));
            _introStageCoordinator = introStageCoordinator ?? throw new ArgumentNullException(nameof(introStageCoordinator));
        }

        public void DispatchIntroStage(string source, IntroStageSession session, SceneRouteKind routeKind, string reason)
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            string contentName = DescribeSessionContentName(session);

            if (!session.HasIntroStage)
            {
                DebugUtility.Log<IntroStageLifecycleDispatchService>(
                    $"[OBS][IntroStage] IntroStageSkipped reason='no_content' source='{source}' contentName='{contentName}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' reason='{reason}' sessionSignature='{session.SessionSignature}'.",
                    DebugUtility.Colors.Info);

                IntroStageSession noContentSession = CreateNoContentSession(session);
                PublishIntroStageCompleted(noContentSession, "GameplaySessionFlow", wasSkipped: true, reason: "no_content");
                RunIntroStage(noContentSession, routeKind, activeSceneName, reason);
                return;
            }

            if (!_presenterRegistry.TryEnsureCurrentPresenter(session, source, out _))
            {
                DebugUtility.Log<IntroStageLifecycleDispatchService>(
                    $"[OBS][IntroStage] IntroStageSkipped reason='no_content' source='{source}' contentName='{contentName}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' reason='{reason}' sessionSignature='{session.SessionSignature}' detail='presenter_unavailable'.",
                    DebugUtility.Colors.Info);

                IntroStageSession noContentSession = CreateNoContentSession(session);
                PublishIntroStageCompleted(noContentSession, "GameplaySessionFlow", wasSkipped: true, reason: "no_content");
                RunIntroStage(noContentSession, routeKind, activeSceneName, reason);
                return;
            }

            DebugUtility.Log<IntroStageLifecycleDispatchService>(
                $"[OBS][IntroStage] IntroStageStartRequested source='{source}' contentName='{contentName}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' reason='{reason}' sessionSignature='{session.SessionSignature}'.",
                DebugUtility.Colors.Info);

            RunIntroStage(session, routeKind, activeSceneName, reason);
        }

        private void RunIntroStage(IntroStageSession session, SceneRouteKind routeKind, string activeSceneName, string reason)
        {
            var context = new IntroStageContext(
                session: session,
                routeKind: routeKind,
                targetScene: activeSceneName,
                reason: reason);

            _ = _introStageCoordinator.RunIntroStageAsync(context);
        }

        private static void PublishIntroStageCompleted(IntroStageSession session, string source, bool wasSkipped, string reason)
        {
            string contentName = DescribeSessionContentName(session);
            DebugUtility.Log<IntroStageLifecycleDispatchService>(
                $"[OBS][IntroStage] IntroStageCompletedPublished source='{source}' contentName='{contentName}' v='{session.SelectionVersion}' signature='{session.SessionSignature}' skipped='{wasSkipped.ToString().ToLowerInvariant()}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            EventBus<IntroStageCompletedEvent>.Raise(new IntroStageCompletedEvent(session, source, wasSkipped, reason));
        }

        private static IntroStageSession CreateNoContentSession(IntroStageSession session)
        {
            return new IntroStageSession(
                session.PhaseDefinitionRef,
                session.LocalContentId,
                session.Reason,
                session.SelectionVersion,
                session.PhaseLocalEntrySequence,
                session.SessionSignature,
                hasIntroStage: false,
                entrySignature: session.EntrySignature);
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static string DescribeSessionContentName(IntroStageSession session)
        {
            if (session.PhaseDefinitionRef != null)
            {
                return session.PhaseDefinitionRef.name;
            }

            return "<none>";
        }
    }
}

