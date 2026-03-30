using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bridges
{
    public sealed partial class GameLoopSceneFlowSyncCoordinator : IDisposable
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly SceneTransitionRequest _startPlan;
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();

        private bool _startInProgress;
        private bool _transitionCompleted;
        private bool _worldResetCompleted;
        private bool _syncIssued;

        private string _expectedContextSignature;

        private readonly EventBinding<GameStartRequestedEvent> _startRequestedBinding;
        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private readonly EventBinding<WorldResetCompletedEvent> _worldResetCompletedBinding;

        private bool _disposed;

        public GameLoopSceneFlowSyncCoordinator(ISceneTransitionService sceneFlow, SceneTransitionRequest startPlan)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _startPlan = ValidateStartPlanOrFailFast(startPlan);

            _startRequestedBinding = new EventBinding<GameStartRequestedEvent>(_ => OnStartRequestedCommon());
            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            _worldResetCompletedBinding = new EventBinding<WorldResetCompletedEvent>(OnWorldResetCompleted);

            _subscriptions.Register(_startRequestedBinding);
            _subscriptions.Register(_transitionStartedBinding);
            _subscriptions.Register(_transitionCompletedBinding);
            _subscriptions.Register(_worldResetCompletedBinding);

            DebugUtility.Log(typeof(GameLoopSceneFlowSyncCoordinator),
                $"[GameLoopSceneFlow] Coordinator registrado. StartPlan: Load=[{string.Join(", ", _startPlan.ScenesToLoad)}], Unload=[{string.Join(", ", _startPlan.ScenesToUnload)}], Active='{_startPlan.TargetActiveScene}', UseFade={_startPlan.UseFade}, Style='{_startPlan.StyleLabel}'.");
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
                    "[GameLoopSceneFlow] Start REQUEST ignorado (ja em progresso).",
                    DebugUtility.Colors.Info);
                return;
            }

            _startInProgress = true;
            ResetStartState();

            DebugUtility.Log(typeof(GameLoopSceneFlowSyncCoordinator),
                "[GameLoopSceneFlow] Start REQUEST recebido. Disparando transicao de cenas...");

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
                DebugUtility.LogError(typeof(GameLoopSceneFlowSyncCoordinator),
                    $"[GameLoopSceneFlow] Falha ao executar TransitionAsync(startPlan). ex={ex}");

                _startInProgress = false;
            }
        }

        private static async Task EnsureFadeReadyForStartTransitionAsync()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IFadeService>(out var fadeService) || fadeService == null)
            {
                HandleFadeStartFailure("IFadeService is not available in global DI before start transition.");
                return;
            }

            try
            {
                await fadeService.EnsureReadyAsync();
            }
            catch (Exception ex)
            {
                HandleFadeStartFailure($"EnsureReadyAsync failed before start transition. ex='{ex.GetType().Name}: {ex.Message}'");
            }
        }

        private static void HandleFadeStartFailure(string detail)
        {
            if (ShouldDegradeFadeInRuntime())
            {
                DebugUtility.LogError(typeof(GameLoopSceneFlowSyncCoordinator),
                    $"[DEGRADED][Fade] {detail} Continuing without fade.");
                return;
            }

            string message = $"[FATAL][Fade] {detail}";
            DebugUtility.LogError(typeof(GameLoopSceneFlowSyncCoordinator), message);

            DevStopPlayModeInEditor();
            if (!Application.isEditor)
            {
                Application.Quit();
            }

            throw new InvalidOperationException(message);
        }

        static partial void DevStopPlayModeInEditor();

        private static bool ShouldDegradeFadeInRuntime()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (!ShouldHandleTransition(evt.context))
            {
                return;
            }

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                $"[GameLoopSceneFlow] TransitionStarted recebido. expectedSignature='{_expectedContextSignature ?? "<null>"}'.",
                DebugUtility.Colors.Info);
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!ShouldHandleTransition(evt.context))
            {
                TrySyncGameLoopFromTransitionCompleted(evt.context);
                return;
            }

            string ctxSig = SceneTransitionSignature.Compute(evt.context);
            if (!string.IsNullOrEmpty(_expectedContextSignature) &&
                !string.Equals(ctxSig, _expectedContextSignature, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                    $"[GameLoopSceneFlow] TransitionCompleted ignorado (signature mismatch). expected='{_expectedContextSignature}', got='{ctxSig}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _transitionCompleted = true;

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                $"[GameLoopSceneFlow] TransitionCompleted recebido. expectedSignature='{_expectedContextSignature ?? "<null>"}'.",
                DebugUtility.Colors.Info);

            TryIssueGameLoopSync();
        }

        private void TrySyncGameLoopFromTransitionCompleted(SceneTransitionContext context)
        {
            if (_startInProgress)
            {
                return;
            }

            if (context.RouteKind != SceneRouteKind.Gameplay &&
                context.RouteKind != SceneRouteKind.Frontend)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
            {
                DebugUtility.LogWarning(typeof(GameLoopSceneFlowSyncCoordinator),
                    "[GameLoopSceneFlow] IGameLoopService indisponivel; runtime sync de SceneFlow/Completed ignorado.");
                return;
            }

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                $"[GameLoopSceneFlow] Runtime sync delegada ao GameLoop. routeKind='{context.RouteKind}'.",
                DebugUtility.Colors.Info);

            gameLoopService.RequestSceneFlowCompletionSync(context.RouteKind);
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
                        $"[GameLoopSceneFlow] WorldResetCompletedEvent ignorado (signature mismatch). expected='{_expectedContextSignature}', got='{evt.ContextSignature}', outcome='{evt.Outcome}', reason='{evt.Reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.",
                        DebugUtility.Colors.Info);
                    return;
                }
            }
            else if (!string.IsNullOrEmpty(_expectedContextSignature))
            {
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                    $"[GameLoopSceneFlow] WorldResetCompletedEvent ignorado (sem assinatura, mas expectedSignature='{_expectedContextSignature}'). outcome='{evt.Outcome}', reason='{evt.Reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _worldResetCompleted = true;

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowSyncCoordinator),
                $"[GameLoopSceneFlow] WorldReset concluido (ou skip). outcome='{evt.Outcome}', reason='{evt.Reason ?? "<null>"}', detail='{evt.Detail ?? "<null>"}'.",
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

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoop) || gameLoop == null)
            {
                DebugUtility.LogError(typeof(GameLoopSceneFlowSyncCoordinator),
                    "[GameLoopSceneFlow] IGameLoopService indisponivel no DI global; nao foi possivel sincronizar GameLoop.");

                _startInProgress = false;
                return;
            }

            _syncIssued = true;
            gameLoop.Initialize();

            DebugUtility.LogVerbose<GameLoopSceneFlowSyncCoordinator>(
                $"[GameLoopSceneFlow] Sync concluido. routeId='{_startPlan.RouteId}' activeScene='{_startPlan.TargetActiveScene}'. Chamando RequestReady() no GameLoop.",
                DebugUtility.Colors.Info);

            gameLoop.RequestReady();
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
