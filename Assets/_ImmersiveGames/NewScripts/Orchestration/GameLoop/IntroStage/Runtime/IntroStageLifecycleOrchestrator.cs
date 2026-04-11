#nullable enable
using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime
{
    public sealed class IntroStageLifecycleOrchestrator : IDisposable
    {
        private readonly EventBinding<IntroStageEntryEvent> _introStageEntryBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private readonly object _sync = new();
        private int _lastProcessedSelectionVersion = -1;
        private IntroStageSession _pendingGameplaySession;
        private string _pendingGameplaySource = string.Empty;
        private string _pendingGameplayReason = string.Empty;
        private bool _hasPendingGameplayIntro;

        public IntroStageLifecycleOrchestrator()
        {
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

            if (!TryAdvanceDedupe(evt.Session.SelectionVersion, evt.Session.SessionSignature, evt.Source))
            {
                return;
            }

            if (ShouldDeferGameplayIntro(evt))
            {
                QueuePendingGameplayIntro(evt);
                return;
            }

            DispatchIntroStage(
                evt.Source,
                evt.Session,
                evt.RouteKind,
                Normalize(evt.Session.Reason));
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (evt.context.RouteKind != SceneRouteKind.Gameplay)
            {
                return;
            }

            IntroStageSession pendingSession;
            string pendingSource;
            string pendingReason;

            lock (_sync)
            {
                if (!_hasPendingGameplayIntro)
                {
                    return;
                }

                pendingSession = _pendingGameplaySession;
                pendingSource = _pendingGameplaySource;
                pendingReason = _pendingGameplayReason;
                _hasPendingGameplayIntro = false;
                _pendingGameplaySession = default;
                _pendingGameplaySource = string.Empty;
                _pendingGameplayReason = string.Empty;
            }

            DebugUtility.Log<IntroStageLifecycleOrchestrator>(
                $"[OBS][IntroStage] IntroStageReleasedOnSceneTransitionCompleted source='{pendingSource}' contentName='{DescribeSessionContentName(pendingSession)}' v='{pendingSession.SelectionVersion}' reason='{Normalize(pendingReason)}' sessionSignature='{Normalize(pendingSession.SessionSignature)}' routeKind='{evt.context.RouteKind}' sceneTransitionSignature='{SceneTransitionSignature.Compute(evt.context)}'.",
                DebugUtility.Colors.Info);

            DispatchIntroStage(
                pendingSource,
                pendingSession,
                evt.context.RouteKind,
                pendingReason);
        }

        private bool TryAdvanceDedupe(int selectionVersion, string sessionSignature, string source)
        {
            if (selectionVersion < _lastProcessedSelectionVersion)
            {
                _lastProcessedSelectionVersion = -1;
            }

            if (selectionVersion <= _lastProcessedSelectionVersion)
            {
                DebugUtility.LogVerbose<IntroStageLifecycleOrchestrator>(
                    $"[IntroStage] skipped reason='dedupe_selection_version' selectionVersion='{selectionVersion}' source='{source}' sessionSignature='{Normalize(sessionSignature)}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            _lastProcessedSelectionVersion = selectionVersion;
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

            DebugUtility.Log<IntroStageLifecycleOrchestrator>(
                $"[OBS][IntroStage] IntroStageDeferred source='{evt.Source}' contentName='{DescribeSessionContentName(evt.Session)}' v='{evt.Session.SelectionVersion}' hasIntroStage='{evt.Session.HasIntroStage}' hasRunResultStage='{evt.Session.HasRunResultStage}' reason='{Normalize(evt.Session.Reason)}' sessionSignature='{Normalize(evt.Session.SessionSignature)}' gate='SceneTransitionCompletedEvent'.",
                DebugUtility.Colors.Info);
        }

        private static bool TryResolvePresenterRegistry(out IIntroStagePresenterRegistry presenterRegistry)
        {
            presenterRegistry = null;

            if (!DependencyManager.Provider.TryGetGlobal(out presenterRegistry) ||
                presenterRegistry == null)
            {
                HardFailFastH1.Trigger(typeof(IntroStageLifecycleOrchestrator),
                    "[FATAL][H1][IntroStage] Missing IIntroStagePresenterRegistry for intro entry.");
                return false;
            }

            return true;
        }

        private void DispatchIntroStage(
            string source,
            IntroStageSession session,
            SceneRouteKind routeKind,
            string reason)
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            string contentName = DescribeSessionContentName(session);

            if (!session.HasIntroStage)
            {
                DebugUtility.Log<IntroStageLifecycleOrchestrator>(
                    $"[OBS][IntroStage] IntroStageSkipped reason='no_content' source='{source}' contentName='{contentName}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' hasRunResultStage='{session.HasRunResultStage}' reason='{reason}' sessionSignature='{session.SessionSignature}'.",
                    DebugUtility.Colors.Info);

                IntroStageSession noContentSession = CreateNoContentSession(session);
                PublishIntroStageCompleted(noContentSession, "GameplaySessionFlow", wasSkipped: true, reason: "no_content");

                var noIntroContext = new IntroStageContext(
                    session: noContentSession,
                    routeKind: routeKind,
                    targetScene: activeSceneName,
                    reason: reason);

                if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var noIntroCoordinator) || noIntroCoordinator == null)
                {
                    HardFailFastH1.Trigger(typeof(IntroStageLifecycleOrchestrator),
                        $"[FATAL][H1][IntroStage] Missing IIntroStageCoordinator for no-content intro skip. source='{source}' sessionSignature='{session.SessionSignature}'.");
                }

                if (noIntroCoordinator != null)
                {
                    _ = noIntroCoordinator.RunIntroStageAsync(noIntroContext);
                }

                return;
            }

            if (!TryResolvePresenterRegistry(out var presenterRegistry))
            {
                return;
            }

            if (!presenterRegistry.TryEnsureCurrentPresenter(session, source, out _))
            {
                DebugUtility.Log<IntroStageLifecycleOrchestrator>(
                    $"[OBS][IntroStage] IntroStageSkipped reason='no_content' source='{source}' contentName='{contentName}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' hasRunResultStage='{session.HasRunResultStage}' reason='{reason}' sessionSignature='{session.SessionSignature}' detail='presenter_unavailable'.",
                    DebugUtility.Colors.Info);

                IntroStageSession noContentSession = CreateNoContentSession(session);
                PublishIntroStageCompleted(noContentSession, "GameplaySessionFlow", wasSkipped: true, reason: "no_content");

                var noContentContext = new IntroStageContext(
                    session: noContentSession,
                    routeKind: routeKind,
                    targetScene: activeSceneName,
                    reason: reason);

                if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var noContentCoordinator) || noContentCoordinator == null)
                {
                    HardFailFastH1.Trigger(typeof(IntroStageLifecycleOrchestrator),
                        $"[FATAL][H1][IntroStage] Missing IIntroStageCoordinator for presenterless intro skip. source='{source}' sessionSignature='{session.SessionSignature}'.");
                }

                if (noContentCoordinator != null)
                {
                    _ = noContentCoordinator.RunIntroStageAsync(noContentContext);
                }

                return;
            }

            DebugUtility.Log<IntroStageLifecycleOrchestrator>(
                $"[OBS][IntroStage] IntroStageStartRequested source='{source}' contentName='{contentName}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' hasRunResultStage='{session.HasRunResultStage}' reason='{reason}' sessionSignature='{session.SessionSignature}'.",
                DebugUtility.Colors.Info);

            var context = new IntroStageContext(
                session: session,
                routeKind: routeKind,
                targetScene: activeSceneName,
                reason: reason);

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var coordinator) || coordinator == null)
            {
                HardFailFastH1.Trigger(typeof(IntroStageLifecycleOrchestrator),
                    $"[FATAL][H1][IntroStage] Missing IIntroStageCoordinator for EnterStage dispatch. source='{source}' sessionSignature='{session.SessionSignature}'.");
            }

            if (coordinator != null)
            {
                _ = coordinator.RunIntroStageAsync(context);
            }
        }

        private static void PublishIntroStageCompleted(IntroStageSession session, string source, bool wasSkipped, string reason)
        {
            string contentName = DescribeSessionContentName(session);
            DebugUtility.Log<IntroStageLifecycleOrchestrator>(
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
                session.SessionSignature,
                null,
                hasIntroStage: false,
                session.HasRunResultStage);
        }
    }
}
