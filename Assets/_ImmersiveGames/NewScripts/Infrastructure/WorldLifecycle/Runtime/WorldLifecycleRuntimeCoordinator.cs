using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Phases;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// Runtime driver do WorldLifecycle:
    /// - Observa <see cref="SceneTransitionScenesReadyEvent"/>
    /// - Dispara <see cref="WorldLifecycleController.ResetWorldAsync"/> no controller da cena ativa (quando aplicável)
    /// - Emite <see cref="WorldLifecycleResetCompletedEvent"/> para liberar o completion gate do SceneFlow
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleRuntimeCoordinator : IWorldResetRequestService
    {
        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private readonly HashSet<string> _inflightSignatures = new();
        private readonly object _signatureLock = new();
        private readonly object _transitionLock = new();

        private string _activeTransitionSignature = string.Empty;
        private SceneFlowProfileId _activeTransitionProfileId = default;
        private string _activeTransitionTargetScene = string.Empty;

        // Fallback defensivo: se alguma transição para Menu vier sem profile,
        // evitamos ruído/erro por falta de WorldLifecycleController no Menu.
        // Importante: NÃO deve mascarar profile incorreto (ex.: gameplay apontando para Menu).
        private const string FrontendSceneName = "MenuScene";

        public WorldLifecycleRuntimeCoordinator()
        {
            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnSceneTransitionStarted);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionStartedEvent>.Register(_transitionStartedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

            DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeCoordinator),
                "[WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent.",
                DebugUtility.Colors.Info);
        }

        private void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {
            var context = evt.Context;
            if (context.Equals(default(SceneTransitionContext)))
            {
                return;
            }

            lock (_transitionLock)
            {
                _activeTransitionSignature = GetContextSignature(context);
                _activeTransitionProfileId = context.TransitionProfileId;
                _activeTransitionTargetScene = context.TargetActiveScene ?? string.Empty;
            }
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            lock (_transitionLock)
            {
                _activeTransitionSignature = string.Empty;
                _activeTransitionProfileId = default;
                _activeTransitionTargetScene = string.Empty;
            }
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (evt.Context.Equals(default(SceneTransitionContext)))
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeCoordinator),
                    "[WorldLifecycle] SceneTransitionScenesReady recebido com Context default. Ignorando.");
                return;
            }

            var context = evt.Context;
            var activeSceneName = context.TargetActiveScene;
            var profileId = context.TransitionProfileId;
            var signature = GetContextSignature(context);

            if (!TryBeginResetForSignature(signature))
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Reset IGNORED (duplicate). signature='{signature}', profile='{profileId}'.");
                return;
            }

            DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),
                $"[WorldLifecycle] SceneTransitionScenesReady recebido. Context={context}");

            // SKIP canônico: startup + frontend.
            var skipByProfile = profileId.IsStartupOrFrontend;

            // Fallback: só aplica quando profile vier ausente (evita esconder bug de profile incorreto).
            var skipBySceneFallback =
                !skipByProfile &&
                !profileId.IsValid &&
                string.Equals(activeSceneName, FrontendSceneName, StringComparison.Ordinal);

            var resetReason = skipByProfile || skipBySceneFallback
                ? WorldLifecycleResetReason.SkippedStartupOrFrontend(profileId, activeSceneName)
                : WorldLifecycleResetReason.ScenesReadyFor(activeSceneName);

            DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),
                $"{WorldLifecyclePhaseObservabilitySignatures.ResetRequested} sourceSignature='{signature}' reason='{resetReason}' profile='{profileId}' target='{activeSceneName}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),
                $"[WorldLifecycle] Reset REQUESTED. reason='{resetReason}', signature='{signature}', profile='{profileId}'.",
                DebugUtility.Colors.Info);

            if (skipByProfile || skipBySceneFallback)
            {
                var why = skipByProfile ? "profile" : "scene-fallback";
                var skipReason = resetReason;

                DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Reset SKIPPED (startup/frontend). why='{why}', profile='{profileId}', activeScene='{activeSceneName}', reason='{skipReason}'.",
                    DebugUtility.Colors.Info);

                EmitResetCompleted(context, reason: skipReason, signature: signature);
                return;
            }

            _ = RunResetAsync(context, activeSceneName, profileId, resetReason, signature);
        }

        private async Task RunResetAsync(
            SceneTransitionContext context,
            string activeSceneName,
            SceneFlowProfileId profileId,
            string resetReason,
            string signature)
        {
            try
            {
                var controller = FindControllerInScene(activeSceneName);
                if (controller == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleRuntimeCoordinator),
                        $"[WorldLifecycle] WorldLifecycleController não encontrado na cena '{activeSceneName}'. Reset abortado.");

                    EmitResetCompleted(context,
                        reason: WorldLifecycleResetReason.FailedNoController(activeSceneName),
                        signature: signature);
                    return;
                }

                DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Disparando hard reset após ScenesReady. reason='{resetReason}', profile='{profileId}'",
                    DebugUtility.Colors.Info);

                // IMPORTANTE: no projeto atual, ResetWorldAsync recebe apenas 'reason'.
                await controller.ResetWorldAsync(reason: resetReason);

                // Commit seguro (pending -> current) SOMENTE após reset ter concluído com sucesso.
                TryCommitPendingPhase(signature, resetReason);

                EmitResetCompleted(context, reason: resetReason, signature: signature);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Exceção ao executar reset: {ex.Message}");

                EmitResetCompleted(context,
                    reason: WorldLifecycleResetReason.FailedReset(ex.GetType().Name),
                    signature: signature);
            }
        }

        public Task RequestResetAsync(string source)
        {
            var activeSceneName = SceneManager.GetActiveScene().name;
            var reason = WorldLifecycleResetReason.ProductionTrigger(source);
            var safeSource = string.IsNullOrWhiteSpace(source) ? "<unspecified>" : source.Trim();

            DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),
                $"[WorldLifecycle] Reset REQUESTED. reason='{reason}', source='{safeSource}', scene='{activeSceneName}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),
                $"{WorldLifecyclePhaseObservabilitySignatures.ResetRequested} sourceSignature='{safeSource}' reason='{reason}' profile='<none>' target='{activeSceneName}'.",
                DebugUtility.Colors.Info);

            if (IsSceneTransitionGateActive())
            {
                var transitionInfo = GetActiveTransitionInfo();
                var signatureInfo = string.IsNullOrEmpty(transitionInfo.Signature)
                    ? "<none>"
                    : transitionInfo.Signature;
                var profileInfo = transitionInfo.ProfileId.IsValid
                    ? transitionInfo.ProfileId.ToString()
                    : "<none>";
                var targetSceneInfo = string.IsNullOrEmpty(transitionInfo.TargetScene)
                    ? "<unknown>"
                    : transitionInfo.TargetScene;

                DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Reset IGNORED (scene-transition). reason='{reason}', scene='{activeSceneName}', detail='SceneTransition gate ativo', signature='{signatureInfo}', profile='{profileInfo}', targetScene='{targetSceneInfo}'.");
                return Task.CompletedTask;
            }

            return RunDirectResetAsync(activeSceneName, reason, safeSource);
        }

        private async Task RunDirectResetAsync(string activeSceneName, string reason, string safeSource)
        {
            try
            {
                var controller = FindControllerInScene(activeSceneName);
                if (controller == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleRuntimeCoordinator),
                        $"[WorldLifecycle] WorldLifecycleController não encontrado na cena '{activeSceneName}'. Reset abortado.");
                    return;
                }

                await controller.ResetWorldAsync(reason: reason);

                // Commit seguro também para reset manual/in-place (após reset bem-sucedido).
                TryCommitPendingPhase(safeSource, reason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Exceção ao executar reset manual: {ex.Message}");
            }
        }

        private static bool IsSceneTransitionGateActive()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService) || gateService == null)
            {
                return false;
            }

            return gateService.IsTokenActive(SimulationGateTokens.SceneTransition);
        }

        private static WorldLifecycleController FindControllerInScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return null;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var go = roots[i];
                if (go == null) continue;

                var controller = go.GetComponentInChildren<WorldLifecycleController>(includeInactive: true);
                if (controller != null)
                    return controller;
            }

            return null;
        }

        private void EmitResetCompleted(SceneTransitionContext context, string reason, string signature)
        {
            try
            {
                // Contrato atual: a correlação usa SceneTransitionContext.ContextSignature,
                // centralizado via SceneTransitionSignatureUtil.Compute(context).
                var safeSignature = string.IsNullOrEmpty(signature)
                    ? SceneTransitionSignatureUtil.Compute(context)
                    : signature;

                DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='{context.TransitionProfileId}', signature='{safeSignature}', reason='{reason}'.",
                    DebugUtility.Colors.Info);

                EventBus<WorldLifecycleResetCompletedEvent>.Raise(
                    new WorldLifecycleResetCompletedEvent(safeSignature, reason));
            }
            finally
            {
                CompleteResetForSignature(signature);
            }
        }

        private static string GetContextSignature(SceneTransitionContext context)
        {
            if (!string.IsNullOrEmpty(context.ContextSignature))
            {
                return context.ContextSignature;
            }

            return SceneTransitionSignatureUtil.Compute(context);
        }

        private bool TryBeginResetForSignature(string signature)
        {
            if (string.IsNullOrEmpty(signature))
            {
                return true;
            }

            lock (_signatureLock)
            {
                if (_inflightSignatures.Contains(signature))
                {
                    return false;
                }

                _inflightSignatures.Add(signature);
                return true;
            }
        }

        private void CompleteResetForSignature(string signature)
        {
            if (string.IsNullOrEmpty(signature))
            {
                return;
            }

            lock (_signatureLock)
            {
                _inflightSignatures.Remove(signature);
            }
        }

        private (string Signature, SceneFlowProfileId ProfileId, string TargetScene) GetActiveTransitionInfo()
        {
            lock (_transitionLock)
            {
                return (_activeTransitionSignature, _activeTransitionProfileId, _activeTransitionTargetScene);
            }
        }

        private static void TryCommitPendingPhase(string sourceSignature, string resetReason)
        {
            try
            {
                if (!DependencyManager.Provider.TryGetGlobal<IPhaseContextService>(out var phaseContext) || phaseContext == null)
                {
                    return;
                }

                if (!phaseContext.HasPending)
                {
                    return;
                }

                var commitReason = $"WorldLifecycle/ResetCompleted sig='{sourceSignature}' reason='{resetReason}'";
                phaseContext.TryCommitPending(commitReason, out _);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle][Phase] Falha ao commit pending phase. ex='{ex.GetType().Name}', msg='{ex.Message}'.");
            }
        }
    }
}
