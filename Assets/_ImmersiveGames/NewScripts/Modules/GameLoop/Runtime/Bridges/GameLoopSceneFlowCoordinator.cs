using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges
{
    /// <summary>
    /// Sincroniza a conclusão do SceneFlow (SceneTransitionCompleted) com o WorldLifecycle reset (WorldLifecycleResetCompleted)
    /// e coloca o GameLoop no estado correto para o perfil de transição.
    ///
    /// Regra Strict/Release (ADR-0013): o SceneFlow NÃO deve forçar RequestStart() em gameplay;
    /// o start efetivo é responsabilidade do pipeline de início de nível (ex.: LevelStartPipeline/IntroStageCoordinator)
    /// que só libera o início após IntroStageController completar.
    /// </summary>
    public sealed class GameLoopSceneFlowCoordinator : IDisposable
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly SceneTransitionRequest _startPlan;

        private bool _startInProgress;
        private bool _transitionCompleted;
        private bool _worldResetCompleted;
        private bool _syncIssued;

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
                    "[GameLoopSceneFlow] Coordinator registrado com startPlan NULL. Start será ignorado até corrigir o GlobalCompositionRoot.");
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
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                    "[OBS][Boot] Aborting start request handling due to fatal latch.",
                    DebugUtility.Colors.Info);
                return;
            }

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
            ResetStartState();

            DebugUtility.Log(typeof(GameLoopSceneFlowCoordinator),
                "[GameLoopSceneFlow] Start REQUEST recebido. Disparando transição de cenas...");

            _ = StartTransitionAsync();
        }

        private async Task StartTransitionAsync()
        {
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                    "[OBS][Boot] Aborting transition start due to fatal latch.",
                    DebugUtility.Colors.Info);
                _startInProgress = false;
                return;
            }

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
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                return;
            }

            if (!ShouldHandleTransition(evt.Context))
            {
                return;
            }


            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                $"[GameLoopSceneFlow] TransitionStarted recebido. expectedSignature='{_expectedContextSignature ?? "<null>"}'.");
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                return;
            }

            if (!ShouldHandleTransition(evt.Context))
            {
                return;
            }


            string ctxSig = SceneTransitionSignature.Compute(evt.Context);
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
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                return;
            }

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

            if (!IsMatchingProfile(context.TransitionProfileId))
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
                DebugUtility.LogError(typeof(GameLoopSceneFlowCoordinator),
                    "[GameLoopSceneFlow] IGameLoopService indisponível no DI global; não foi possível sincronizar GameLoop.");

                _startInProgress = false;
                return;
            }

            _syncIssued = true;

            // Importante: este coordinator é usado para o startPlan de produção.
            // Regra Strict/Release:
            // - Em frontend/startup: apenas manter o GameLoop em Ready.
            // - Em gameplay: também manter em Ready (NÃO RequestStart aqui). O início efetivo acontece após IntroStageController completar.
            //   (ex.: LevelStartPipeline/IntroStageCoordinator chama RequestStart no momento correto)
            var profileId = _startPlan?.TransitionProfileId ?? default;

            gameLoop.Initialize();

            string profileLabel = profileId.IsValid ? profileId.Value : "<none>";

            DebugUtility.LogVerbose<GameLoopSceneFlowCoordinator>(
                $"[GameLoopSceneFlow] Sync concluído. profileId='{profileLabel}'. Chamando RequestReady() no GameLoop (start via pipeline/IntroStageController).",
                DebugUtility.Colors.Info);

            gameLoop.RequestReady();

            _startInProgress = false;
        }
    }
}



