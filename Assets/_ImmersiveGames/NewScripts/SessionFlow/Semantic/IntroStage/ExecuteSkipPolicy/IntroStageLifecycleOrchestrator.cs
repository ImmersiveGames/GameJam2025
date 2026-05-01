#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.ContentContract;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.ExecuteSkipPolicy
{
    public sealed class IntroStageLifecycleOrchestrator : IDisposable
    {
        private readonly IIntroStageLifecycleStateService _stateService;
        private readonly IIntroStageLifecycleDispatchService _dispatchService;
        private readonly EventBinding<SessionTransitionIntroStageActivationEvent> _introStageActivationBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;

        public IntroStageLifecycleOrchestrator(
            IIntroStageLifecycleStateService stateService,
            IIntroStageLifecycleDispatchService dispatchService)
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _dispatchService = dispatchService ?? throw new ArgumentNullException(nameof(dispatchService));
            _introStageActivationBinding = new EventBinding<SessionTransitionIntroStageActivationEvent>(OnIntroStageActivation);
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            EventBus<SessionTransitionIntroStageActivationEvent>.Register(_introStageActivationBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);
        }

        public void Dispose()
        {
            EventBus<SessionTransitionIntroStageActivationEvent>.Unregister(_introStageActivationBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
        }

        private void OnIntroStageActivation(SessionTransitionIntroStageActivationEvent evt)
        {
            if (!evt.HasCanonicalPayload)
            {
                HardFailFastH1.Trigger(typeof(IntroStageLifecycleOrchestrator),
                    "[FATAL][H1][IntroStage] Invalid SessionTransitionIntroStageActivationEvent received.");
            }

            if (!_stateService.TryAcceptIntroStageActivation(evt, out bool shouldDefer))
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
                Normalize(evt.Reason));
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
        bool TryAcceptIntroStageActivation(SessionTransitionIntroStageActivationEvent evt, out bool shouldDefer);
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
        private readonly IntroStageLifecycleDeferPolicy _deferPolicy = new();
        private readonly IntroStageLifecycleTelemetry _telemetry = new();
        private int _lastProcessedPhaseLocalEntrySequence;
        private IntroStageSession _pendingGameplaySession;
        private string _pendingGameplayContextSignature = string.Empty;
        private string _pendingGameplayTargetScene = string.Empty;
        private string _pendingGameplaySource = string.Empty;
        private string _pendingGameplayReason = string.Empty;
        private bool _hasPendingGameplayIntro;

        public bool TryAcceptIntroStageActivation(SessionTransitionIntroStageActivationEvent evt, out bool shouldDefer)
        {
            shouldDefer = false;

            if (!TryAdvanceDedupe(evt.Session.PhaseLocalEntrySequence, evt.Session.SelectionVersion, evt.Source))
            {
                return false;
            }

            if (_deferPolicy.ShouldDeferGameplayIntro(evt.RouteKind))
            {
                QueuePendingGameplayIntro(
                    evt.Session,
                    evt.PhaseLocalEntryReadyEvent.Context.ContextSignature,
                    evt.PhaseLocalEntryReadyEvent.SceneName,
                    evt.Source,
                    evt.Reason);
                shouldDefer = true;
            }

            return true;
        }

        public bool TryReleasePendingGameplayIntro(
            SceneTransitionCompletedEvent evt,
            out IntroStagePendingGameplayIntro pendingGameplayIntro)
        {
            pendingGameplayIntro = default;

            IntroStageSession pendingSession;
            string pendingSource;
            string pendingReason;
            string pendingContextSignature;
            string pendingTargetScene;

            lock (_sync)
            {
                if (!_hasPendingGameplayIntro)
                {
                    _telemetry.LogStaleSceneTransitionCompleted(
                        evt.context,
                        "no_pending_intro",
                        default,
                        string.Empty,
                        string.Empty,
                        string.Empty);
                    return false;
                }

                pendingSession = _pendingGameplaySession;
                pendingSource = _pendingGameplaySource;
                pendingReason = _pendingGameplayReason;
                pendingContextSignature = _pendingGameplayContextSignature;
                pendingTargetScene = _pendingGameplayTargetScene;

                if (!MatchesPendingGameplayIntro(evt.context, pendingSession, pendingContextSignature, pendingTargetScene))
                {
                    _telemetry.LogStaleSceneTransitionCompleted(
                        evt.context,
                        "pending_intro_mismatch",
                        pendingSession,
                        pendingContextSignature,
                        pendingTargetScene,
                        pendingReason);
                    return false;
                }

                _hasPendingGameplayIntro = false;
                _pendingGameplaySession = default;
                _pendingGameplayContextSignature = string.Empty;
                _pendingGameplayTargetScene = string.Empty;
                _pendingGameplaySource = string.Empty;
                _pendingGameplayReason = string.Empty;
            }

            _telemetry.LogReleasedOnSceneTransitionCompleted(
                pendingSource,
                pendingSession,
                pendingReason,
                evt.context.RouteKind,
                SceneTransitionSignature.Compute(evt.context));

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

        private void QueuePendingGameplayIntro(
            IntroStageSession session,
            string contextSignature,
            string targetScene,
            string source,
            string reason)
        {
            lock (_sync)
            {
                _pendingGameplaySession = session;
                _pendingGameplayContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
                _pendingGameplayTargetScene = string.IsNullOrWhiteSpace(targetScene) ? string.Empty : targetScene.Trim();
                _pendingGameplaySource = source;
                _pendingGameplayReason = Normalize(reason);
                _hasPendingGameplayIntro = true;
            }

            _telemetry.LogDeferred(source, session, Normalize(reason));
        }

        private bool MatchesPendingGameplayIntro(
            SceneTransitionContext context,
            IntroStageSession pendingSession,
            string pendingContextSignature,
            string pendingTargetScene)
        {
            if (!pendingSession.PhaseEntryIdentity.IsValid)
            {
                HardFailFastH1.Trigger(typeof(IntroStageLifecycleStateService),
                    "[FATAL][H1][IntroStage] Pending gameplay intro must carry a canonical PhaseEntryIdentity.");
            }

            if (context.RouteKind != pendingSession.PhaseEntryIdentity.RouteKind)
            {
                return false;
            }

            if (context.RouteId != pendingSession.PhaseEntryIdentity.RouteId)
            {
                return false;
            }

            if (!string.Equals(context.TargetActiveScene, pendingTargetScene, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(pendingContextSignature) &&
                !string.Equals(context.ContextSignature, pendingContextSignature, StringComparison.Ordinal))
            {
                return false;
            }

            return string.Equals(pendingSession.SessionSignature, pendingSession.PhaseEntryIdentity.SessionSignature, StringComparison.Ordinal) &&
                   string.Equals(pendingSession.EntrySignature, pendingSession.PhaseEntryIdentity.EntrySignature, StringComparison.Ordinal) &&
                   pendingSession.PhaseLocalEntrySequence == pendingSession.PhaseEntryIdentity.PhaseLocalEntrySequence;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageLifecycleDispatchService : IIntroStageLifecycleDispatchService
    {
        private readonly IIntroStageCoordinator _introStageCoordinator;
        private readonly IIntroStagePresenterRegistry _presenterRegistry;
        private readonly IntroStageLifecycleTelemetry _telemetry = new();

        public IntroStageLifecycleDispatchService(
            IIntroStageCoordinator introStageCoordinator,
            IIntroStagePresenterRegistry presenterRegistry)
        {
            _introStageCoordinator = introStageCoordinator ?? throw new ArgumentNullException(nameof(introStageCoordinator));
            _presenterRegistry = presenterRegistry ?? throw new ArgumentNullException(nameof(presenterRegistry));
        }

        public void DispatchIntroStage(string source, IntroStageSession session, SceneRouteKind routeKind, string reason)
        {
            string activeSceneName = SceneManager.GetActiveScene().name;

            if (!session.HasIntroStage)
            {
                ValidateGameplayCompletionPayload(routeKind, session, PhaseFlowSignalVocabulary.NoContentReason);
                _telemetry.LogSkipped(PhaseFlowSignalVocabulary.NoContentReason, source, session, reason);
                RunIntroStage(session, routeKind, activeSceneName, reason, source);
                return;
            }

            if (!_presenterRegistry.TryEnsureCurrentPresenter(session, source, out _))
            {
                HardFailFastH1.Trigger(typeof(IntroStageLifecycleDispatchService),
                    $"[FATAL][H1][IntroStage] Presenter mandatory but absent for execute path. source='{NormalizeForLog(source)}' reason='{NormalizeForLog(reason)}' signature='{NormalizeForLog(session.SessionSignature)}' phaseEntryIdentity='{session.PhaseEntryIdentity}'.");
            }

            _telemetry.LogStartRequested(source, session, reason);
            RunIntroStage(session, routeKind, activeSceneName, reason, source);
        }

        private void RunIntroStage(
            IntroStageSession session,
            SceneRouteKind routeKind,
            string activeSceneName,
            string reason,
            string source)
        {
            var context = new IntroStageContext(
                session: session,
                routeKind: routeKind,
                targetScene: activeSceneName,
                reason: reason);

            _ = ObserveIntroStageDispatchAsync(context, source);
        }

        private async Task ObserveIntroStageDispatchAsync(IntroStageContext context, string source)
        {
            try
            {
                DebugUtility.Log<IntroStageLifecycleDispatchService>(
                    BuildIntroStageDispatchLogMessage(context, source, "started", null),
                    DebugUtility.Colors.Info);

                await _introStageCoordinator.RunIntroStageAsync(context);

                DebugUtility.Log<IntroStageLifecycleDispatchService>(
                    BuildIntroStageDispatchLogMessage(context, source, "completed", null),
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<IntroStageLifecycleDispatchService>(
                    BuildIntroStageDispatchLogMessage(context, source, "failed", ex));

                HardFailFastH1.Trigger(typeof(IntroStageLifecycleDispatchService),
                    $"[FATAL][H1][IntroStage] IntroStage dispatch failed. operation='IntroStageDispatch' status='failed' reason='{NormalizeForLog(context.Reason)}' targetScene='{NormalizeForLog(context.TargetScene)}' routeKind='{context.RouteKind}' contextSignature='{NormalizeForLog(context.ContextSignature)}' executionSignature='{NormalizeForLog(context.ExecutionSignature)}' phaseLocalEntrySequence='{context.Session.PhaseLocalEntrySequence}' entrySignature='{NormalizeForLog(context.Session.EntrySignature)}' sourcePath='{NormalizeForLog(source)}' exceptionType='{ex.GetType().Name}' exceptionMessage='{NormalizeForLog(ex.Message)}'.",
                    ex);
            }
        }

        private static void ValidateGameplayCompletionPayload(
            SceneRouteKind routeKind,
            IntroStageSession session,
            string reason)
        {
            if (routeKind != SceneRouteKind.Gameplay)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(session.SessionSignature) &&
                !string.IsNullOrWhiteSpace(session.PhaseRuntimeSignature) &&
                !string.IsNullOrWhiteSpace(session.EntrySignature))
            {
                return;
            }

            HardFailFastH1.Trigger(typeof(IntroStageLifecycleDispatchService),
                $"[FATAL][H1][IntroStage] Invalid IntroStageCompleted payload for Gameplay route. reason='{NormalizeForLog(reason)}' hasSessionSignature='{(!string.IsNullOrWhiteSpace(session.SessionSignature)).ToString().ToLowerInvariant()}' hasPhaseRuntimeSignature='{(!string.IsNullOrWhiteSpace(session.PhaseRuntimeSignature)).ToString().ToLowerInvariant()}' hasEntrySignature='{(!string.IsNullOrWhiteSpace(session.EntrySignature)).ToString().ToLowerInvariant()}'.");
        }

        private static string BuildIntroStageDispatchLogMessage(
            IntroStageContext context,
            string source,
            string status,
            Exception? exception)
        {
            string exceptionFields = exception == null
                ? string.Empty
                : $" exceptionType='{exception.GetType().Name}' exceptionMessage='{NormalizeForLog(exception.Message)}'";

            return $"[OBS][IntroStage] intro_stage_dispatch_{status} operation='IntroStageDispatch' status='{status}' reason='{NormalizeForLog(context.Reason)}' targetScene='{NormalizeForLog(context.TargetScene)}' routeKind='{context.RouteKind}' contextSignature='{NormalizeForLog(context.ContextSignature)}' executionSignature='{NormalizeForLog(context.ExecutionSignature)}' phaseLocalEntrySequence='{context.Session.PhaseLocalEntrySequence}' entrySignature='{NormalizeForLog(context.Session.EntrySignature)}' sourcePath='{NormalizeForLog(source)}' hasIntroStage='{context.HasIntroStage.ToString().ToLowerInvariant()}' selectionVersion='{context.Session.SelectionVersion}'{exceptionFields}.";
        }

        private static string NormalizeForLog(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    internal sealed class IntroStageLifecycleDeferPolicy
    {
        public bool ShouldDeferGameplayIntro(SceneRouteKind routeKind)
        {
            return routeKind == SceneRouteKind.Gameplay;
        }
    }

    internal sealed class IntroStageLifecycleTelemetry
    {
        public void LogDeferred(string source, IntroStageSession session, string reason)
        {
            DebugUtility.Log<IntroStageLifecycleStateService>(
                $"[OBS][IntroStage] IntroStageDeferred source='{source}' contentName='{DescribeSessionContentName(session)}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' reason='{Normalize(reason)}' sessionSignature='{Normalize(session.SessionSignature)}' gate='SceneTransitionCompletedEvent'.",
                DebugUtility.Colors.Info);
        }

        public void LogReleasedOnSceneTransitionCompleted(
            string source,
            IntroStageSession session,
            string reason,
            SceneRouteKind routeKind,
            string sceneTransitionSignature)
        {
            DebugUtility.Log<IntroStageLifecycleStateService>(
                $"[OBS][IntroStage] IntroStageReleasedOnSceneTransitionCompleted source='{source}' contentName='{DescribeSessionContentName(session)}' v='{session.SelectionVersion}' reason='{Normalize(reason)}' sessionSignature='{Normalize(session.SessionSignature)}' routeKind='{routeKind}' sceneTransitionSignature='{sceneTransitionSignature}'.",
                DebugUtility.Colors.Info);
        }

        public void LogStaleSceneTransitionCompleted(
            SceneTransitionContext context,
            string reason,
            IntroStageSession expectedSession,
            string expectedContextSignature,
            string expectedTargetScene,
            string expectedReason)
        {
            string expectedIdentity = expectedSession.IsValid
                ? expectedSession.PhaseEntryIdentity.ToString()
                : "<none>";

            string expectedSessionSignature = expectedSession.IsValid
                ? Normalize(expectedSession.SessionSignature)
                : "<none>";

            string expectedEntrySignature = expectedSession.IsValid
                ? Normalize(expectedSession.EntrySignature)
                : "<none>";

            string expectedPhaseLocalEntrySequence = expectedSession.IsValid
                ? expectedSession.PhaseLocalEntrySequence.ToString()
                : "<none>";

            DebugUtility.Log<IntroStageLifecycleStateService>(
                $"[OBS][IntroStage] StaleFactIgnored source='scene_transition_completed' reason='{Normalize(reason)}' expectedPhaseEntryIdentity='{expectedIdentity}' expectedSessionSignature='{expectedSessionSignature}' expectedEntrySignature='{expectedEntrySignature}' expectedPhaseLocalEntrySequence='{expectedPhaseLocalEntrySequence}' expectedContextSignature='{Normalize(expectedContextSignature)}' expectedScene='{Normalize(expectedTargetScene)}' expectedRouteId='{expectedSession.PhaseEntryIdentity.RouteId}' expectedRouteKind='{expectedSession.PhaseEntryIdentity.RouteKind}' receivedContextSignature='{Normalize(context.ContextSignature)}' receivedScene='{Normalize(context.TargetActiveScene)}' receivedRouteId='{context.RouteId}' receivedRouteKind='{context.RouteKind}' receivedReason='{Normalize(context.Reason)}' pendingReason='{Normalize(expectedReason)}'.",
                DebugUtility.Colors.Info);
        }

        public void LogSkipped(string skipReason, string source, IntroStageSession session, string reason)
        {
            DebugUtility.Log<IntroStageLifecycleDispatchService>(
                $"[OBS][IntroStage] IntroStageSkipped reason='{Normalize(skipReason)}' source='{source}' contentName='{DescribeSessionContentName(session)}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' reason='{Normalize(reason)}' sessionSignature='{Normalize(session.SessionSignature)}'.",
                DebugUtility.Colors.Info);
        }

        public void LogStartRequested(string source, IntroStageSession session, string reason)
        {
            DebugUtility.Log<IntroStageLifecycleDispatchService>(
                $"[OBS][IntroStage] IntroStageStartRequested source='{source}' contentName='{DescribeSessionContentName(session)}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' reason='{Normalize(reason)}' sessionSignature='{Normalize(session.SessionSignature)}'.",
                DebugUtility.Colors.Info);
        }

        private static string DescribeSessionContentName(IntroStageSession session)
        {
            if (session.PhaseDefinitionRef != null)
            {
                return session.PhaseDefinitionRef.name;
            }

            return "<none>";
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
