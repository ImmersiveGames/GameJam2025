#nullable enable
using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.ContentContract;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.ExecuteSkipPolicy
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
        private readonly IntroStageLifecycleDeferPolicy _deferPolicy = new();
        private readonly IntroStageLifecycleTelemetry _telemetry = new();
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

            if (_deferPolicy.ShouldDeferGameplayIntro(evt))
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

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private void QueuePendingGameplayIntro(IntroStageEntryEvent evt)
        {
            lock (_sync)
            {
                _pendingGameplaySession = evt.Session;
                _pendingGameplaySource = evt.Source;
                _pendingGameplayReason = Normalize(evt.Session.Reason);
                _hasPendingGameplayIntro = true;
            }

            _telemetry.LogDeferred(evt.Source, evt.Session, Normalize(evt.Session.Reason));
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageLifecycleDispatchService : IIntroStageLifecycleDispatchService
    {
        private readonly IIntroStageCoordinator _introStageCoordinator;
        private readonly IntroStageDispatchEligibilityPolicy _eligibilityPolicy;
        private readonly IntroStageLifecycleTelemetry _telemetry = new();
        private readonly IIntroStageCompletionSignalingService _completionSignalingService;

        public IntroStageLifecycleDispatchService(
            IIntroStagePresenterRegistry presenterRegistry,
            IIntroStageCoordinator introStageCoordinator)
        {
            _introStageCoordinator = introStageCoordinator ?? throw new ArgumentNullException(nameof(introStageCoordinator));
            _eligibilityPolicy = new IntroStageDispatchEligibilityPolicy(
                presenterRegistry ?? throw new ArgumentNullException(nameof(presenterRegistry)));
            _completionSignalingService = new IntroStageCompletionSignalingService(_telemetry);
        }

        public void DispatchIntroStage(string source, IntroStageSession session, SceneRouteKind routeKind, string reason)
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            IntroStageDispatchEligibilityDecision decision = _eligibilityPolicy.Evaluate(session, source, reason);

            if (decision.ShouldSkip)
            {
                IntroStageSession noContentSession = CreateNoContentSession(session);
                _telemetry.LogSkipped(decision, source, noContentSession, reason);

                _completionSignalingService.Publish(noContentSession, source, wasSkipped: true, reason: decision.CompletionReason);
                RunIntroStage(noContentSession, routeKind, activeSceneName, reason);
                return;
            }

            _telemetry.LogStartRequested(source, session, reason);

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
    }

    internal sealed class IntroStageLifecycleDeferPolicy
    {
        public bool ShouldDeferGameplayIntro(IntroStageEntryEvent evt)
        {
            return evt.RouteKind == SceneRouteKind.Gameplay
                   && string.Equals(evt.Source, "GameplaySessionFlow", StringComparison.Ordinal);
        }
    }

    internal sealed class IntroStageDispatchEligibilityPolicy
    {
        private readonly IIntroStagePresenterRegistry _presenterRegistry;

        public IntroStageDispatchEligibilityPolicy(IIntroStagePresenterRegistry presenterRegistry)
        {
            _presenterRegistry = presenterRegistry ?? throw new ArgumentNullException(nameof(presenterRegistry));
        }

        public IntroStageDispatchEligibilityDecision Evaluate(IntroStageSession session, string source, string reason)
        {
            if (!session.HasIntroStage)
            {
                return IntroStageDispatchEligibilityDecision.SkipNoContent();
            }

            if (!_presenterRegistry.TryEnsureCurrentPresenter(session, source, out _))
            {
                return IntroStageDispatchEligibilityDecision.SkipPresenterUnavailable();
            }

            return IntroStageDispatchEligibilityDecision.Execute();
        }
    }

    internal readonly struct IntroStageDispatchEligibilityDecision
    {
        private IntroStageDispatchEligibilityDecision(
            bool shouldSkip,
            bool warning,
            string logReason,
            string skipReason,
            string detail,
            string completionReason)
        {
            ShouldSkip = shouldSkip;
            Warning = warning;
            LogReason = logReason;
            SkipReason = skipReason;
            Detail = detail;
            CompletionReason = completionReason;
        }

        public bool ShouldSkip { get; }
        public bool Warning { get; }
        public string LogReason { get; }
        public string SkipReason { get; }
        public string Detail { get; }
        public string CompletionReason { get; }

        public static IntroStageDispatchEligibilityDecision Execute()
            => new(false, false, string.Empty, string.Empty, string.Empty, string.Empty);

        public static IntroStageDispatchEligibilityDecision SkipNoContent()
            => new(true, false, "no_content", "no_content", string.Empty, "no_content");

        public static IntroStageDispatchEligibilityDecision SkipPresenterUnavailable()
            => new(true, true, "presenter_unavailable", "no_content", "scene_local_presenter_not_found", "presenter_unavailable");
    }

    internal interface IIntroStageCompletionSignalingService
    {
        void Publish(IntroStageSession session, string source, bool wasSkipped, string reason);
    }

    internal sealed class IntroStageCompletionSignalingService : IIntroStageCompletionSignalingService
    {
        private readonly IntroStageLifecycleTelemetry _telemetry;

        public IntroStageCompletionSignalingService(IntroStageLifecycleTelemetry telemetry)
        {
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public void Publish(IntroStageSession session, string source, bool wasSkipped, string reason)
        {
            string canonicalSource = IntroStageCompletionSignalPolicy.CanonicalizeSource(source);
            string canonicalReason = IntroStageCompletionSignalPolicy.CanonicalizeReason(reason, wasSkipped);

            _telemetry.LogCompletedPublished(canonicalSource, session, wasSkipped, canonicalReason);
            EventBus<IntroStageCompletedEvent>.Raise(new IntroStageCompletedEvent(session, canonicalSource, wasSkipped, canonicalReason));
        }
    }

    internal static class IntroStageCompletionSignalPolicy
    {
        private const string GameplaySessionFlowSource = "GameplaySessionFlow";
        private const string PhaseDefinitionNavigationSource = "PhaseDefinitionNavigation";

        public static string CanonicalizeSource(string source)
        {
            string normalized = NormalizeToken(source);
            if (string.IsNullOrEmpty(normalized))
            {
                return GameplaySessionFlowSource;
            }

            if (string.Equals(normalized, GameplaySessionFlowSource, StringComparison.OrdinalIgnoreCase))
            {
                return GameplaySessionFlowSource;
            }

            if (string.Equals(normalized, PhaseDefinitionNavigationSource, StringComparison.OrdinalIgnoreCase))
            {
                return GameplaySessionFlowSource;
            }

            return normalized;
        }

        public static string CanonicalizeReason(string reason, bool wasSkipped)
        {
            string normalized = NormalizeToken(reason);
            if (string.IsNullOrEmpty(normalized))
            {
                return wasSkipped ? "no_content" : "<none>";
            }

            if (string.Equals(normalized, "no_content", StringComparison.OrdinalIgnoreCase))
            {
                return "no_content";
            }

            if (string.Equals(normalized, "presenter_unavailable", StringComparison.OrdinalIgnoreCase))
            {
                return "presenter_unavailable";
            }

            if (string.Equals(normalized, "IntroStage/ContinueButton", StringComparison.OrdinalIgnoreCase))
            {
                return "IntroStage/ContinueButton";
            }

            if (string.Equals(normalized, "superseded", StringComparison.OrdinalIgnoreCase))
            {
                return "superseded";
            }

            return normalized;
        }

        private static string NormalizeToken(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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

        public void LogSkipped(IntroStageDispatchEligibilityDecision decision, string source, IntroStageSession session, string reason)
        {
            string message = decision.Warning
                ? $"[WARN][OBS][IntroStage] IntroStageSkipped reason='{decision.LogReason}' skipReason='{decision.SkipReason}' source='{source}' contentName='{DescribeSessionContentName(session)}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' reason='{Normalize(reason)}' sessionSignature='{Normalize(session.SessionSignature)}' detail='{decision.Detail}'."
                : $"[OBS][IntroStage] IntroStageSkipped reason='{decision.LogReason}' source='{source}' contentName='{DescribeSessionContentName(session)}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' reason='{Normalize(reason)}' sessionSignature='{Normalize(session.SessionSignature)}'.";

            if (decision.Warning)
            {
                DebugUtility.LogWarning<IntroStageLifecycleDispatchService>(message);
                return;
            }

            DebugUtility.Log<IntroStageLifecycleDispatchService>(message, DebugUtility.Colors.Info);
        }

        public void LogStartRequested(string source, IntroStageSession session, string reason)
        {
            DebugUtility.Log<IntroStageLifecycleDispatchService>(
                $"[OBS][IntroStage] IntroStageStartRequested source='{source}' contentName='{DescribeSessionContentName(session)}' v='{session.SelectionVersion}' hasIntroStage='{session.HasIntroStage}' reason='{Normalize(reason)}' sessionSignature='{Normalize(session.SessionSignature)}'.",
                DebugUtility.Colors.Info);
        }

        public void LogCompletedPublished(string source, IntroStageSession session, bool wasSkipped, string reason)
        {
            DebugUtility.Log<IntroStageLifecycleDispatchService>(
                $"[OBS][IntroStage] IntroStageCompletedPublished source='{source}' contentName='{DescribeSessionContentName(session)}' v='{session.SelectionVersion}' signature='{session.SessionSignature}' skipped='{wasSkipped.ToString().ToLowerInvariant()}' reason='{Normalize(reason)}'.",
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
