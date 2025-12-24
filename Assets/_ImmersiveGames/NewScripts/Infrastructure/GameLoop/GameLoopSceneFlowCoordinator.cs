/*
 * Nota (QA):
 * - O coordinator NÃO deve cachear IGameLoopService; deve resolver no momento do "ready"
 *   para que overrides de QA no DI sejam observados.
 *
 * Responsabilidade:
 * - Ouve um REQUEST de start (QA/UI) e dispara o SceneFlow startPlan.
 * - Captura a assinatura real do contexto a partir do PRIMEIRO evento da transição (Started),
 *   evitando reconstruir/“supor” assinatura via startPlan.
 * - Aguarda TransitionCompleted (gate já abriu) + WorldLifecycleResetCompleted para então pedir start do GameLoop.
 */
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.World;

namespace _ImmersiveGames.NewScripts.GameLoop.SceneFlow
{
    public sealed class GameLoopSceneFlowCoordinator
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly SceneTransitionRequest _startPlan;

        private bool _startRequested;
        private bool _transitionCompleted;
        private bool _worldResetCompleted;
        private bool _startIssued;

        // Assinatura “real” capturada do primeiro evento da transição (Started).
        private string _expectedContextSignature;

        private readonly EventBinding<GameStartRequestedEvent> _startRequestedBinding;
        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private readonly EventBinding<WorldLifecycleResetCompletedEvent> _worldResetCompletedBinding;

        public GameLoopSceneFlowCoordinator(ISceneTransitionService sceneFlow, SceneTransitionRequest startPlan)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _startPlan = startPlan;

            _startRequestedBinding = new EventBinding<GameStartRequestedEvent>(OnStartRequested);
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

        private void OnStartRequested(GameStartRequestedEvent evt)
        {
            if (_startPlan == null)
            {
                DebugUtility.LogError(typeof(GameLoopSceneFlowCoordinator),
                    "[GameLoopSceneFlow] Start REQUEST recebido, mas startPlan é NULL. Abortando.");
                return;
            }

            if (_startRequested)
            {
                DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                    "[GameLoopSceneFlow] Start REQUEST ignorado (já recebido).");
                return;
            }

            _startRequested = true;
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
            }
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (!_startRequested)
                return;

            // Filtra por profile (quando definido) para evitar cross-talk.
            if (!IsMatchingProfile(evt.Context.TransitionProfileName))
                return;

            EnsureExpectedSignatureFromContext(evt.Context);

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                $"[GameLoopSceneFlow] TransitionStarted recebido. expectedSignature='{_expectedContextSignature ?? "<null>"}'.");
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!_startRequested)
                return;

            if (!IsMatchingProfile(evt.Context.TransitionProfileName))
                return;

            // Garante assinatura baseada no evento real (não no plan).
            EnsureExpectedSignatureFromContext(evt.Context);

            // Se já temos expectedSignature e a do evento diverge, ignora.
            var ctxSig = evt.Context.ToString();
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

            TryIssueGameLoopStart();
        }

        private void OnWorldResetCompleted(WorldLifecycleResetCompletedEvent evt)
        {
            if (!_startRequested)
                return;

            // Se o evento de reset veio com assinatura, usamos para filtrar / capturar.
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

            _worldResetCompleted = true;

            DebugUtility.LogVerbose(typeof(GameLoopSceneFlowCoordinator),
                $"[GameLoopSceneFlow] WorldLifecycle reset concluído (ou skip). reason='{evt.Reason ?? "<null>"}'.");

            TryIssueGameLoopStart();
        }

        private bool IsMatchingProfile(string transitionProfileName)
        {
            // Se o startPlan não definiu profile, não filtra.
            var expected = _startPlan?.TransitionProfileName;
            if (string.IsNullOrWhiteSpace(expected))
                return true;

            return string.Equals(transitionProfileName ?? string.Empty, expected, StringComparison.Ordinal);
        }

        private void EnsureExpectedSignatureFromContext(SceneTransitionContext context)
        {
            if (!string.IsNullOrEmpty(_expectedContextSignature))
                return;

            _expectedContextSignature = context.ToString();
        }

        private void TryIssueGameLoopStart()
        {
            if (_startIssued)
                return;

            // Ready “seguro”: gate já abriu (TransitionCompleted) + world reset (ou skip) finalizado.
            if (!_startRequested || !_transitionCompleted || !_worldResetCompleted)
                return;

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoop) || gameLoop == null)
            {
                DebugUtility.LogError(typeof(GameLoopSceneFlowCoordinator),
                    "[GameLoopSceneFlow] IGameLoopService indisponível no DI global; não foi possível RequestStart().");
                return;
            }

            _startIssued = true;

            DebugUtility.Log(typeof(GameLoopSceneFlowCoordinator),
                "[GameLoopSceneFlow] Ready: TransitionCompleted + WorldLifecycleResetCompleted. Chamando GameLoop.RequestStart().",
                DebugUtility.Colors.Success);

            gameLoop.RequestStart();
        }
    }
}
