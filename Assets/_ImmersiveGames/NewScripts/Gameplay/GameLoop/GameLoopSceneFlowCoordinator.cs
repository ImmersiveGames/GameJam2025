using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    public sealed class GameLoopSceneFlowCoordinator : IDisposable
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly SceneTransitionRequest _startPlan;

        private bool _startInProgress;
        private bool _transitionCompleted;
        private bool _worldResetCompleted;
        private bool _startIssued;

        private string _expectedContextSignature;

        private readonly EventBinding<GameStartRequestedEvent> _startRequestedBinding;

        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private readonly EventBinding<WorldLifecycleResetCompletedEvent> _worldResetCompletedBinding;

        private bool _disposed;

        public GameLoopSceneFlowCoordinator(ISceneTransitionService sceneFlow, SceneTransitionRequest startPlan)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _startPlan = startPlan;

            _startRequestedBinding = new EventBinding<GameStartRequestedEvent>(_ => OnStartRequestedCommon());

            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            _worldResetCompletedBinding = new EventBinding<WorldLifecycleResetCompletedEvent>(OnWorldResetCompleted);

            EventBus<GameStartRequestedEvent>.Register(_startRequestedBinding);

            EventBus<SceneTransitionStartedEvent>.Register(_transitionStartedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);
            EventBus<WorldLifecycleResetCompletedEvent>.Register(_worldResetCompletedBinding);

            if (_startPlan == null)
            {
                DebugUtility.LogWarning(typeof(GameLoopSceneFlowCoordinator),
                    "[GameLoopSceneFlow] Coordinator registrado com startPlan NULL. Start será ignorado até corrigir o GlobalBootstrap.");
                return;
            }

            DebugUtility.Log(typeof(GameLoopSceneFlowCoordinator),
                $"[GameLoopSceneFlow] Coordinator registrado. StartPlan: " +
                $"Load=[{string.Join(", ", _startPlan.ScenesToLoad)}], " +
                $"Unload=[{string.Join(", ", _startPlan.ScenesToUnload)}], " +
                $"Active='{_startPlan.TargetActiveScene}', " +
                $"UseFade={_startPlan.UseFade}, " +
                $"Profile='{_startPlan.TransitionProfileName}'.");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            EventBus<GameStartRequestedEvent>.Unregister(_startRequestedBinding);
            EventBus<SceneTransitionStartedEvent>.Unregister(_transitionStartedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_transitionCompletedBinding);
            EventBus<WorldLifecycleResetCompletedEvent>.Unregister(_worldResetCompletedBinding);
        }

        private void OnStartRequestedCommon()
        {
            if (_startPlan == null)
            {
                DebugUtility.LogError(typeof(GameLoopSceneFlowCoordinator),
                    "[GameLoopSceneFlow] Start REQUEST recebido, mas startPlan é NULL. Abortando.");
                return;
            }

            if (_startInProgress)
            {
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                    "[GameLoopSceneFlow] Start REQUEST ignorado (já em progresso).");
                return;
            }

            _startInProgress = true;
            _transitionCompleted = false;
            _worldResetCompleted = false;
            _startIssued = false;
            _expectedContextSignature = null;

            DebugUtility.Log(typeof(GameLoopSceneFlowCoordinator),
                "[GameLoopSceneFlow] Start REQUEST recebido. Disparando transição de cenas...");

            _ = StartTransitionAsync();
        }

        private async Task StartTransitionAsync()
        {
            try
            {
                await _sceneFlow.TransitionAsync(_startPlan);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameLoopSceneFlowCoordinator),
                    $"[GameLoopSceneFlow] Falha ao executar TransitionAsync(startPlan). ex={ex}");

                _startInProgress = false;
            }
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (!_startInProgress)
            {
                return;
            }

            if (!IsMatchingProfile(evt.Context.TransitionProfileId))
            {
                return;
            }

            EnsureExpectedSignatureFromContext(evt.Context);

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                $"[GameLoopSceneFlow] TransitionStarted recebido. expectedSignature='{_expectedContextSignature ?? "<null>"}'.");
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!_startInProgress)
            {
                return;
            }

            if (!IsMatchingProfile(evt.Context.TransitionProfileId))
            {
                return;
            }

            EnsureExpectedSignatureFromContext(evt.Context);

            string ctxSig = SceneTransitionSignatureUtil.Compute(evt.Context);
            if (!string.IsNullOrEmpty(_expectedContextSignature) &&
                !string.Equals(ctxSig, _expectedContextSignature, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                    $"[GameLoopSceneFlow] TransitionCompleted ignorado (signature mismatch). " +
                    $"expected='{_expectedContextSignature}', got='{ctxSig}'.");
                return;
            }

            _transitionCompleted = true;

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                $"[GameLoopSceneFlow] TransitionCompleted recebido. expectedSignature='{_expectedContextSignature ?? "<null>"}'.");

            TryIssueGameLoopSync();
        }

        private void OnWorldResetCompleted(WorldLifecycleResetCompletedEvent evt)
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
                    DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                        $"[GameLoopSceneFlow] WorldLifecycleResetCompleted ignorado (signature mismatch). " +
                        $"expected='{_expectedContextSignature}', got='{evt.ContextSignature}', reason='{evt.Reason ?? "<null>"}'.");
                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(_expectedContextSignature))
                {
                    DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                        $"[GameLoopSceneFlow] WorldLifecycleResetCompleted ignorado (sem assinatura, mas expectedSignature='{_expectedContextSignature}'). " +
                        $"reason='{evt.Reason ?? "<null>"}'.");
                    return;
                }
            }

            _worldResetCompleted = true;

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                $"[GameLoopSceneFlow] WorldLifecycle reset concluído (ou skip). reason='{evt.Reason ?? "<null>"}'.");

            TryIssueGameLoopSync();
        }

        private bool IsMatchingProfile(SceneFlowProfileId transitionProfileId)
        {
            var expected = _startPlan?.TransitionProfileId ?? default;
            if (!expected.IsValid)
            {
                return true;
            }

            return transitionProfileId.Equals(expected);
        }

        private void EnsureExpectedSignatureFromContext(SceneTransitionContext context)
        {
            if (!string.IsNullOrEmpty(_expectedContextSignature))
            {
                return;
            }

            _expectedContextSignature = SceneTransitionSignatureUtil.Compute(context);
        }

        private void TryIssueGameLoopSync()
        {
            if (_startIssued)
            {
                return;
            }

            if (!_startInProgress || !_transitionCompleted || !_worldResetCompleted)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoop) || gameLoop == null)
            {
                DebugUtility.LogError(typeof(GameLoopSceneFlowCoordinator),
                    "[GameLoopSceneFlow] IGameLoopService indisponível no DI global; não foi possível sincronizar GameLoop.");

                _startInProgress = false;
                return;
            }

            _startIssued = true;

            // Importante: este coordinator é usado para o startPlan de produção (tipicamente 'startup').
            // Regra: Só iniciar "run" em gameplay. Em startup/frontend, apenas deixar o GameLoop em Ready (sem ativar Playing).
            var profileId = _startPlan?.TransitionProfileId ?? default;

            gameLoop.Initialize();

            if (profileId.IsGameplay)
            {
                DebugUtility.LogVerbose<GameLoopSceneFlowCoordinator>(
                    $"[GameLoopSceneFlow] Profile gameplay detectado (profileId='{profileId.Value}'). Chamando RequestStart() no GameLoop.");
                gameLoop.RequestStart();
            }
            else
            {
                var profileLabel = profileId.IsValid ? profileId.Value : "<none>";

                DebugUtility.LogVerbose<GameLoopSceneFlowCoordinator>(
                    $"[GameLoopSceneFlow] Profile não-gameplay (profileId='{profileLabel}'). Chamando RequestReady() no GameLoop.",
                    DebugUtility.Colors.Info);
                gameLoop.RequestReady();
            }

            _startInProgress = false;
        }
    }
}
