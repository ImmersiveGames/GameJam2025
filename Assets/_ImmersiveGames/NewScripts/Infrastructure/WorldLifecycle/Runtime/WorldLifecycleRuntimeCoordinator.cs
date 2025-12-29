using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// Runtime driver do WorldLifecycle:
    /// - Observa SceneTransitionScenesReadyEvent
    /// - Dispara ResetWorldAsync no controller da cena ativa (quando aplicável)
    /// - Emite WorldLifecycleResetCompletedEvent para liberar o completion gate do SceneFlow
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleRuntimeCoordinator
    {
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;

        // Profiles que NÃO disparam reset (ex.: boot e frontend/menu).
        private static readonly HashSet<string> SkipProfiles = new(StringComparer.OrdinalIgnoreCase)
        {
            "startup",
            "frontend"
        };

        // Fallback: enquanto nem toda transição para Menu carrega o profile "frontend",
        // mantemos o check por nome de cena para evitar erro/ruído (controller ausente no Menu).
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
            var profile = context.TransitionProfileName;

            DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),
                $"[WorldLifecycle] SceneTransitionScenesReady recebido. Context={context}");

            // SKIP em startup/frontend (e fallback por cena Menu).
            var skipByProfile = !string.IsNullOrEmpty(profile) && SkipProfiles.Contains(profile);
            var skipByScene = string.Equals(activeSceneName, FrontendSceneName, StringComparison.Ordinal);

            if (skipByProfile || skipByScene)
            {
                DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Reset SKIPPED (startup/frontend). profile='{profile ?? "<null>"}', activeScene='{activeSceneName}'.",
                    DebugUtility.Colors.Info);

                EmitResetCompleted(context, reason: "Skipped_StartupOrFrontend");
                return;
            }

            _ = RunResetAsync(context, activeSceneName, profile);
        }

        private async Task RunResetAsync(SceneTransitionContext context, string activeSceneName, string profile)
        {
            try
            {
                var controller = FindControllerInScene(activeSceneName);
                if (controller == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleRuntimeCoordinator),
                        $"[WorldLifecycle] WorldLifecycleController não encontrado na cena '{activeSceneName}'. Reset abortado.");

                    EmitResetCompleted(context, reason: "Failed_NoController");
                    return;
                }

                DebugUtility.Log(typeof(WorldLifecycleRuntimeCoordinator),
                    $"[WorldLifecycle] Disparando hard reset após ScenesReady. reason='ScenesReady/{activeSceneName}', profile='{profile ?? "<null>"}'",
                    DebugUtility.Colors.Info);

                // IMPORTANTE: no seu projeto atual, ResetWorldAsync recebe apenas 'reason'.
                await controller.ResetWorldAsync(reason: $"ScenesReady/{activeSceneName}");

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
            // Deve casar com o gate: WorldLifecycleResetCompletionGate usa SceneTransitionSignatureUtil.Compute(context).
            var signature = SceneTransitionSignatureUtil.Compute(context);

            DebugUtility.LogVerbose(typeof(WorldLifecycleRuntimeCoordinator),
                $"[WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='{context.TransitionProfileName}', signature='{signature}', reason='{reason}'.",
                DebugUtility.Colors.Info);

            EventBus<WorldLifecycleResetCompletedEvent>.Raise(
                new WorldLifecycleResetCompletedEvent(signature, reason));
        }
    }
}
