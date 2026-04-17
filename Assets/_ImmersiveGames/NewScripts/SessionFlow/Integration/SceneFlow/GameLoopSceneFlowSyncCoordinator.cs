using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Fade.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    public sealed partial class GameLoopSceneFlowSyncCoordinator : IDisposable
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly IGameLoopService _gameLoop;
        private readonly IFadeService _fadeService;
        private readonly SceneTransitionRequest _startPlan;
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();

        private bool _startInProgress;
        private bool _transitionCompleted;
        private bool _worldResetCompleted;
        private bool _syncIssued;

        private string _expectedContextSignature;

        private readonly EventBinding<BootStartPlanRequestedEvent> _startRequestedBinding;
        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private readonly EventBinding<WorldResetCompletedEvent> _worldResetCompletedBinding;

        private bool _disposed;

        public GameLoopSceneFlowSyncCoordinator(
            ISceneTransitionService sceneFlow,
            IGameLoopService gameLoop,
            IFadeService fadeService,
            SceneTransitionRequest startPlan)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _gameLoop = gameLoop ?? throw new InvalidOperationException("[FATAL][Config][GameLoopSceneFlow] IGameLoopService obrigatorio ausente para o coordinator.");
            _fadeService = fadeService;
            _startPlan = ValidateStartPlanOrFailFast(startPlan);

            if (_startPlan.UseFade && _fadeService == null)
            {
                FailFastConfig("GameLoopSceneFlowSyncCoordinator requires IFadeService when startPlan.UseFade is true.");
            }

            _startRequestedBinding = new EventBinding<BootStartPlanRequestedEvent>(_ => OnStartRequestedCommon());
            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            _worldResetCompletedBinding = new EventBinding<WorldResetCompletedEvent>(OnWorldResetCompleted);

            _subscriptions.Register(_startRequestedBinding);
            _subscriptions.Register(_transitionStartedBinding);
            _subscriptions.Register(_transitionCompletedBinding);
            _subscriptions.Register(_worldResetCompletedBinding);

            DebugUtility.Log(typeof(GameLoopSceneFlowSyncCoordinator),
                $"[OBS][GameLoopSceneFlow][Operational] Coordinator registrado. StartPlan: Load=[{string.Join(", ", _startPlan.ScenesToLoad)}], Unload=[{string.Join(", ", _startPlan.ScenesToUnload)}], Active='{_startPlan.TargetActiveScene}', UseFade={_startPlan.UseFade}, Style='{_startPlan.StyleLabel}'.");
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
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                    "[OBS][GameLoopSceneFlow][Operational] Start REQUEST ignorado (ja em progresso).",
                    DebugUtility.Colors.Info);
                return;
            }

            _startInProgress = true;
            ResetStartState();

            DebugUtility.Log(typeof(GameLoopSceneFlowSyncCoordinator),
                "[OBS][GameLoopSceneFlow][Operational] Start REQUEST recebido. Disparando transicao de cenas...");

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
                HardFailFastH1.Trigger(typeof(GameLoopSceneFlowSyncCoordinator),
                    $"[FATAL][H1][GameLoopSceneFlow] Falha ao executar TransitionAsync(startPlan). ex={ex}",
                    ex);
            }
        }

        private async Task EnsureFadeReadyForStartTransitionAsync()
        {
            if (_fadeService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoopSceneFlow] IFadeService obrigatorio ausente para o startPlan com fade habilitado.");
            }

            await _fadeService.EnsureReadyAsync();
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (!ShouldHandleTransition(evt.context))
            {
                return;
            }

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                $"[OBS][GameLoopSceneFlow][Operational] TransitionStarted recebido. expectedSignature='{_expectedContextSignature ?? "<null>"}'.",
                DebugUtility.Colors.Info);
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!ShouldHandleTransition(evt.context))
            {
                return;
            }

            string ctxSig = SceneTransitionSignature.Compute(evt.context);
            if (!string.IsNullOrEmpty(_expectedContextSignature) &&
                !string.Equals(ctxSig, _expectedContextSignature, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                    $"[OBS][GameLoopSceneFlow][Operational] TransitionCompleted ignorado (signature mismatch). expected='{_expectedContextSignature}', got='{ctxSig}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _transitionCompleted = true;

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                $"[OBS][GameLoopSceneFlow][Operational] TransitionCompleted recebido. expectedSignature='{_expectedContextSignature ?? "<null>"}'.",
                DebugUtility.Colors.Info);

            TryIssueGameLoopSync();
        }

        private void OnWorldResetCompleted(WorldResetCompletedEvent evt)
        {
            if (!_startInProgress)
            {
                return;
            }

            if (!string.IsNullOrEmpty(evt.ContextSignature))
            {
                if (string.IsNullOrEmpty(_expectedContextSignature))
                {
                    _expectedContextSignature = evt.ContextSignature;
                }
                else if (!string.Equals(evt.ContextSignature, _expectedContextSignature, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                        $"[OBS][GameLoopSceneFlow][Operational] WorldResetCompletedEvent ignorado (signature mismatch). expected='{_expectedContextSignature}', got='{evt.ContextSignature}', outcome='{evt.Outcome}', reason='{evt.Reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.",
                        DebugUtility.Colors.Info);
                    return;
                }
            }
            else if (!string.IsNullOrEmpty(_expectedContextSignature))
            {
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                    $"[OBS][GameLoopSceneFlow][Operational] WorldResetCompletedEvent ignorado (sem assinatura, mas expectedSignature='{_expectedContextSignature}'). outcome='{evt.Outcome}', reason='{evt.Reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _worldResetCompleted = true;

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                $"[OBS][GameLoopSceneFlow][Operational] WorldReset concluido (ou skip). outcome='{evt.Outcome}', reason='{evt.Reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.",
                DebugUtility.Colors.Info);

            TryIssueGameLoopSync();
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

        private void TryIssueGameLoopSync()
        {
            if (_syncIssued)
            {
                return;
            }

            if (!_startInProgress || !_transitionCompleted || !_worldResetCompleted)
            {
                return;
            }

            _syncIssued = true;
            var gameLoop = _gameLoop;
            gameLoop.Initialize();

            DebugUtility.LogVerbose<GameLoopSceneFlowSyncCoordinator>(
                $"[OBS][GameLoopSceneFlow][Operational] Sync concluido. routeId='{_startPlan.RouteId}' activeScene='{_startPlan.TargetActiveScene}'. Chamando RequestReset() no GameLoop.",
                DebugUtility.Colors.Info);

            gameLoop.RequestReset();
            _startInProgress = false;
        }

        private static SceneTransitionRequest ValidateStartPlanOrFailFast(SceneTransitionRequest startPlan)
        {
            if (startPlan == null)
            {
                FailFastConfig("GameLoopSceneFlowSyncCoordinator requires a non-null startPlan.");
            }

            if (!startPlan.RouteId.IsValid)
            {
                FailFastConfig($"GameLoopSceneFlowSyncCoordinator requires a valid startPlan RouteId. routeId='{startPlan.RouteId}'.");
            }

            if (string.IsNullOrWhiteSpace(startPlan.TargetActiveScene))
            {
                FailFastConfig($"GameLoopSceneFlowSyncCoordinator requires a non-empty startPlan TargetActiveScene. routeId='{startPlan.RouteId}'.");
            }

            return startPlan;
        }

        private static void FailFastConfig(string message)
        {
            string fatalMessage = $"[FATAL][Config][GameLoopSceneFlow] {message}";
            DebugUtility.LogError(typeof(GameLoopSceneFlowSyncCoordinator), fatalMessage);
            throw new InvalidOperationException(fatalMessage);
        }
    }
}

