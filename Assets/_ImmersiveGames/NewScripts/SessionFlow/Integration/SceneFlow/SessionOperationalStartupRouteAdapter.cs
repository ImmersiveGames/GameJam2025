using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Fade.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    /// <summary>
    /// Adapter temporario de StartupRoute para observar o fluxo Boot/Route/SceneFlow.
    /// Este adapter apenas observa o sync operacional e encerra a sincronizacao quando a rota completa.
    /// </summary>
    public sealed partial class SessionOperationalStartupRouteAdapter : IDisposable
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly IFadeService _fadeService;
        private readonly ISessionOperationalStartupRouteDecisionService _syncDecisionService;
        private readonly SceneTransitionRequest _startPlan;
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();

        private bool _startInProgress;
        private bool _transitionCompleted;
        private bool _worldResetCompleted;
        private bool _syncIssued;

        private string _expectedContextSignature;
        private string _lastReceivedContextSignature;

        private readonly EventBinding<BootStartPlanRequestedEvent> _startRequestedBinding;
        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private readonly EventBinding<WorldResetCompletedEvent> _worldResetCompletedBinding;

        private bool _disposed;

        public SessionOperationalStartupRouteAdapter(
            ISceneTransitionService sceneFlow,
            IFadeService fadeService,
            ISessionOperationalStartupRouteDecisionService syncDecisionService,
            SceneTransitionRequest startPlan)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _fadeService = fadeService;
            _syncDecisionService = syncDecisionService ?? throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] ISessionOperationalStartupRouteDecisionService obrigatorio ausente para o adapter.");
            _startPlan = ValidateStartPlanOrFailFast(startPlan);

            if (_startPlan.UseFade && _fadeService == null)
            {
                FailFastConfig("SessionOperationalStartupRouteAdapter requires IFadeService when startPlan.UseFade is true.");
            }

            _startRequestedBinding = new EventBinding<BootStartPlanRequestedEvent>(_ => OnStartRequestedCommon());
            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            _worldResetCompletedBinding = new EventBinding<WorldResetCompletedEvent>(OnWorldResetCompleted);

            _subscriptions.Register(_startRequestedBinding);
            _subscriptions.Register(_transitionStartedBinding);
            _subscriptions.Register(_transitionCompletedBinding);
            _subscriptions.Register(_worldResetCompletedBinding);

            DebugUtility.Log(typeof(SessionOperationalStartupRouteAdapter),
                $"[OBS][SessionOperationalPipeline][StartupRoute] startupRouteRequested registered source='runtime_composition' routeId='{_startPlan.RouteId}' targetActiveScene='{_startPlan.TargetActiveScene}' useFade={_startPlan.UseFade} style='{_startPlan.StyleLabel}' load=[{string.Join(", ", _startPlan.ScenesToLoad)}] unload=[{string.Join(", ", _startPlan.ScenesToUnload)}].");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _subscriptions.Dispose();
        }

        private void OnStartRequestedCommon()
        {
            if (_startInProgress)
            {
                DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteAdapter),
                    "[OBS][SessionOperationalPipeline][StartupRoute] startupRouteRequested ignored reason='already_in_progress'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _startInProgress = true;
            ResetStartState();

            DebugUtility.Log(typeof(SessionOperationalStartupRouteAdapter),
                $"[OBS][SessionOperationalPipeline][StartupRoute] startupRouteRequested routeId='{_startPlan.RouteId}' targetActiveScene='{_startPlan.TargetActiveScene}' source='runtime_composition'.");

            _ = StartTransitionAsync();
        }

        private async Task StartTransitionAsync()
        {
            try
            {
                if (_startPlan.UseFade)
                {
                    await EnsureFadeReadyForStartTransitionAsync();
                }

                await _sceneFlow.TransitionAsync(_startPlan);
            }
            catch (Exception ex)
            {
                _startInProgress = false;
                HardFailFastH1.Trigger(typeof(SessionOperationalStartupRouteAdapter),
                    $"[FATAL][H1][SessionOperationalPipeline] Falha ao executar TransitionAsync(startPlan). ex={ex}",
                    ex);
            }
        }

        private async Task EnsureFadeReadyForStartTransitionAsync()
        {
            if (_fadeService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] IFadeService obrigatorio ausente para o startPlan com fade habilitado.");
            }

            await _fadeService.EnsureReadyAsync();
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (!ShouldHandleTransition(evt.context))
            {
                LogRejectedForeignOrStale();
                return;
            }

            DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteAdapter),
                $"[OBS][SessionOperationalPipeline][StartupRoute] sceneTransitionStartedObserved routeId='{evt.context.RouteId}' targetActiveScene='{evt.context.TargetActiveScene}' reason='{evt.context.Reason}' contextSignature='{evt.context.ContextSignature}' expectedSignature='{_expectedContextSignature ?? "<null>"}'.",
                DebugUtility.Colors.Info);
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!ShouldHandleTransition(evt.context))
            {
                LogRejectedForeignOrStale();
                return;
            }

            string ctxSig = SceneTransitionSignature.Compute(evt.context);
            _lastReceivedContextSignature = ctxSig;

            if (!_syncDecisionService.IsTransitionSignatureAccepted(_expectedContextSignature, ctxSig))
            {
                DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteAdapter),
                    $"[OBS][SessionOperationalPipeline][StartupRoute] rejected reason='stale_or_foreign_event' event='SceneTransitionCompletedEvent' expectedSignature='{_expectedContextSignature ?? "<null>"}' receivedSignature='{ctxSig}' routeId='{evt.context.RouteId}' targetActiveScene='{evt.context.TargetActiveScene}' reason='{evt.context.Reason}' contextSignature='{evt.context.ContextSignature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _transitionCompleted = true;

            DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteAdapter),
                $"[OBS][SessionOperationalPipeline][StartupRoute] sceneTransitionCompletedObserved routeId='{evt.context.RouteId}' targetActiveScene='{evt.context.TargetActiveScene}' reason='{evt.context.Reason}' contextSignature='{evt.context.ContextSignature}' expectedSignature='{_expectedContextSignature ?? "<null>"}'.",
                DebugUtility.Colors.Info);

            TryIssueStartupRouteSync();
        }

        private void OnWorldResetCompleted(WorldResetCompletedEvent evt)
        {
            if (!_startInProgress)
            {
                LogRejectedForeignOrStale();
                return;
            }

            string receivedSignature = evt.ContextSignature ?? string.Empty;
            _lastReceivedContextSignature = receivedSignature;

            WorldResetSyncSignatureDecision signatureDecision =
                _syncDecisionService.DecideWorldResetSignature(_expectedContextSignature, receivedSignature);

            if (signatureDecision.Kind == WorldResetSyncSignatureDecisionKind.RejectedMismatch)
            {
                DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteAdapter),
                    $"[OBS][SessionOperationalPipeline][StartupRoute] rejected reason='stale_or_foreign_event' event='WorldResetCompletedEvent' expectedSignature='{_expectedContextSignature ?? "<null>"}' receivedSignature='{evt.ContextSignature ?? string.Empty}' outcome='{evt.Outcome}' reason='{evt.Reason ?? "<null>"}' detail='{evt.Detail ?? "<null>"}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (signatureDecision.Kind == WorldResetSyncSignatureDecisionKind.RejectedMissingWithExpected)
            {
                DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteAdapter),
                    $"[OBS][SessionOperationalPipeline][StartupRoute] rejected reason='stale_or_foreign_event' event='WorldResetCompletedEvent' expectedSignature='{_expectedContextSignature ?? "<null>"}' receivedSignature='' outcome='{evt.Outcome}' reason='{evt.Reason ?? "<null>"}' detail='{evt.Detail ?? "<null>"}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (signatureDecision.Kind == WorldResetSyncSignatureDecisionKind.AdoptExpectedFromReceived)
            {
                _expectedContextSignature = signatureDecision.ResolvedExpectedSignature;
            }

            _worldResetCompleted = true;

            DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteAdapter),
                $"[OBS][SessionOperationalPipeline][StartupRoute] worldResetCompletedObserved routeId='{evt.MacroRouteId}' targetScene='{evt.TargetScene}' contextSignature='{evt.ContextSignature}' sourceSignature='{evt.SourceSignature}' outcome='{evt.Outcome}' reason='{evt.Reason}' detail='{evt.Detail}'.",
                DebugUtility.Colors.Info);

            TryIssueStartupRouteSync();
        }

        private bool IsMatchingStartPlan(SceneTransitionContext context)
        {
            if (_startPlan == null)
            {
                return false;
            }

            if (_startPlan.RouteId.IsValid && context.RouteId.IsValid && context.RouteId != _startPlan.RouteId)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_startPlan.TargetActiveScene) &&
                !string.IsNullOrWhiteSpace(context.TargetActiveScene) &&
                !string.Equals(context.TargetActiveScene, _startPlan.TargetActiveScene, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        private void EnsureExpectedSignatureFromContext(SceneTransitionContext context)
        {
            if (!string.IsNullOrEmpty(_expectedContextSignature))
            {
                return;
            }

            _expectedContextSignature = SceneTransitionSignature.Compute(context);
        }

        private void ResetStartState()
        {
            _transitionCompleted = false;
            _worldResetCompleted = false;
            _syncIssued = false;
            _expectedContextSignature = null;
            _lastReceivedContextSignature = string.Empty;
        }

        private bool ShouldHandleTransition(SceneTransitionContext context)
        {
            if (!_startInProgress)
            {
                return false;
            }

            if (!IsMatchingStartPlan(context))
            {
                return false;
            }

            EnsureExpectedSignatureFromContext(context);
            return true;
        }

        private void TryIssueStartupRouteSync()
        {
            if (!_syncDecisionService.CanCompleteSync(
                    _startInProgress,
                    _transitionCompleted,
                    _worldResetCompleted,
                    _syncIssued,
                    _expectedContextSignature,
                    _lastReceivedContextSignature))
            {
                return;
            }

            _syncIssued = true;
            DebugUtility.LogVerbose<SessionOperationalStartupRouteAdapter>(
                $"[OBS][SessionOperationalPipeline][StartupRoute] startupRouteSyncCompleted routeId='{_startPlan.RouteId}' targetActiveScene='{_startPlan.TargetActiveScene}'.",
                DebugUtility.Colors.Info);
            _startInProgress = false;
        }

        private static SceneTransitionRequest ValidateStartPlanOrFailFast(SceneTransitionRequest startPlan)
        {
            if (startPlan == null)
            {
                FailFastConfig("SessionOperationalStartupRouteAdapter requires a non-null startPlan.");
            }

            if (!startPlan.RouteId.IsValid)
            {
                FailFastConfig($"SessionOperationalStartupRouteAdapter requires a valid startPlan RouteId. routeId='{startPlan.RouteId}'.");
            }

            if (string.IsNullOrWhiteSpace(startPlan.TargetActiveScene))
            {
                FailFastConfig($"SessionOperationalStartupRouteAdapter requires a non-empty startPlan TargetActiveScene. routeId='{startPlan.RouteId}'.");
            }

            return startPlan;
        }

        private static void FailFastConfig(string message)
        {
            string fatalMessage = $"[FATAL][Config][SessionOperationalPipeline] {message}";
            DebugUtility.LogError(typeof(SessionOperationalStartupRouteAdapter), fatalMessage);
            throw new InvalidOperationException(fatalMessage);
        }

        private void LogRejectedForeignOrStale()
        {
            DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteAdapter),
                "[OBS][SessionOperationalPipeline][StartupRoute] rejected reason='stale_or_foreign_event'.",
                DebugUtility.Colors.Info);
        }
    }

    public interface ISessionOperationalStartupRouteDecisionService
    {
        bool IsTransitionSignatureAccepted(string expectedSignature, string receivedSignature);
        WorldResetSyncSignatureDecision DecideWorldResetSignature(string expectedSignature, string receivedSignature);
        bool CanCompleteSync(
            bool startInProgress,
            bool transitionCompleted,
            bool worldResetCompleted,
            bool syncIssued,
            string expectedSignature,
            string receivedSignature);
    }

    public enum WorldResetSyncSignatureDecisionKind
    {
        Accepted = 0,
        AdoptExpectedFromReceived = 1,
        RejectedMismatch = 2,
        RejectedMissingWithExpected = 3,
    }

    public readonly struct WorldResetSyncSignatureDecision
    {
        public WorldResetSyncSignatureDecision(WorldResetSyncSignatureDecisionKind kind, string resolvedExpectedSignature)
        {
            Kind = kind;
            ResolvedExpectedSignature = resolvedExpectedSignature ?? string.Empty;
        }

        public WorldResetSyncSignatureDecisionKind Kind { get; }
        public string ResolvedExpectedSignature { get; }
    }

    public sealed class SessionOperationalStartupRouteDecisionService : ISessionOperationalStartupRouteDecisionService
    {
        public bool IsTransitionSignatureAccepted(string expectedSignature, string receivedSignature)
        {
            if (string.IsNullOrEmpty(expectedSignature))
            {
                return true;
            }

            return string.Equals(receivedSignature, expectedSignature, StringComparison.Ordinal);
        }

        public WorldResetSyncSignatureDecision DecideWorldResetSignature(string expectedSignature, string receivedSignature)
        {
            bool hasExpectedSignature = !string.IsNullOrEmpty(expectedSignature);
            bool hasReceivedSignature = !string.IsNullOrEmpty(receivedSignature);

            if (hasReceivedSignature)
            {
                if (!hasExpectedSignature)
                {
                    return new WorldResetSyncSignatureDecision(
                        WorldResetSyncSignatureDecisionKind.AdoptExpectedFromReceived,
                        receivedSignature);
                }

                if (!string.Equals(receivedSignature, expectedSignature, StringComparison.Ordinal))
                {
                    return new WorldResetSyncSignatureDecision(
                        WorldResetSyncSignatureDecisionKind.RejectedMismatch,
                        expectedSignature);
                }
            }
            else if (hasExpectedSignature)
            {
                return new WorldResetSyncSignatureDecision(
                    WorldResetSyncSignatureDecisionKind.RejectedMissingWithExpected,
                    expectedSignature);
            }

            return new WorldResetSyncSignatureDecision(
                WorldResetSyncSignatureDecisionKind.Accepted,
                expectedSignature ?? string.Empty);
        }

        public bool CanCompleteSync(
            bool startInProgress,
            bool transitionCompleted,
            bool worldResetCompleted,
            bool syncIssued,
            string expectedSignature,
            string receivedSignature)
        {
            return !syncIssued &&
                   startInProgress &&
                   transitionCompleted &&
                   worldResetCompleted;
        }
    }
}

