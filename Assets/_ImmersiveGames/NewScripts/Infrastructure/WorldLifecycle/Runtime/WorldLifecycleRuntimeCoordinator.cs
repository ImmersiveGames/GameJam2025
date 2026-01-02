using System;
using UnityEngine;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
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
    public sealed class WorldLifecycleRuntimeCoordinator
    {
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;

        // Fallback defensivo: se alguma transição para Menu vier sem profile,
        // evitamos ruído/erro por falta de WorldLifecycleController no Menu.
        // Importante: NÃO deve mascarar profile incorreto (ex.: gameplay apontando para Menu).
        private const string FrontendSceneName = "MenuScene";

        public WorldLifecycleRuntimeCoordinator()
        {
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

            DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeCoordinator),
                "[WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent.",
                DebugUtility.Colors.Info);
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

            string message = "SceneTransitionScenesReady recebido. Context="+context;
            DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),message
                , DebugUtility.Colors.Info);

            // SKIP canônico: startup + frontend.
            var skipByProfile = profileId.IsStartupOrFrontend;

            // Fallback: só aplica quando profile vier ausente (evita esconder bug de profile incorreto).
            var skipBySceneFallback =
                !skipByProfile &&
                !profileId.IsValid &&
                string.Equals(activeSceneName, FrontendSceneName, StringComparison.Ordinal);

            if (skipByProfile || skipBySceneFallback)
            {
                var why = skipByProfile ? "profile" : "scene-fallback";

                DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Reset SKIPPED (startup/frontend). why='{why}', profile='{profileId}', activeScene='{activeSceneName}'.",
                    DebugUtility.Colors.Info);

                EmitResetCompleted(
                    context,
                    reason: WorldLifecycleResetReason.SkippedStartupOrFrontend(profileId.ToString(), activeSceneName));
                return;
            }

            _ = RunResetAsync(context, activeSceneName, profileId);
        }

        private async Task RunResetAsync(SceneTransitionContext context, string activeSceneName, SceneFlowProfileId profileId)
        {
            try
            {
                var controller = FindControllerInScene(activeSceneName);
                if (controller == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleRuntimeCoordinator),
                        $"[WorldLifecycle] WorldLifecycleController não encontrado na cena '{activeSceneName}'. Reset abortado.");

                    EmitResetCompleted(context, reason: WorldLifecycleResetReason.FailedNoController(activeSceneName));
                    return;
                }

                DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Disparando hard reset após ScenesReady. reason='ScenesReady/{activeSceneName}', profile='{profileId}'",
                    DebugUtility.Colors.Info);

                // IMPORTANTE: no projeto atual, ResetWorldAsync recebe apenas 'reason'.
                await controller.ResetWorldAsync(reason: WorldLifecycleResetReason.ScenesReadyFor(activeSceneName));

                EmitResetCompleted(context, reason: WorldLifecycleResetReason.ScenesReadyFor(activeSceneName));
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Exceção ao executar reset: {ex.Message}");

                EmitResetCompleted(context, reason: WorldLifecycleResetReason.FailedReset(ex.GetType().Name));
            }
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

        private static void EmitResetCompleted(SceneTransitionContext context, string reason)
        {
            // Contrato atual: SceneTransitionSignatureUtil.Compute(context) == context.ToString()
            // (centralizado para permitir evolução futura sem tocar nos callers).
            var signature = SceneTransitionSignatureUtil.Compute(context);

            DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeCoordinator),
                $"[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='{context.TransitionProfileId}', signature='{signature}', reason='{reason}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldLifecycleResetCompletedEvent>.Raise(
                new WorldLifecycleResetCompletedEvent(signature, reason));
        }
    }
}
