using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// Entry-point de produção para solicitar ResetWorld fora de QA.
    ///
    /// Implementação Unity-native:
    /// - Localiza WorldLifecycleController na cena ativa e executa ResetWorldAsync(source).
    /// - Best-effort e defensiva: nunca lança para o caller.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetRequestService : IWorldResetRequestService
    {
        private readonly ISimulationGateService _gateService;

        public WorldResetRequestService(ISimulationGateService gateService = null)
        {
            _gateService = gateService;
        }

        public async Task RequestResetAsync(string source)
        {
            try
            {
                string activeScene = SceneManager.GetActiveScene().name ?? string.Empty;

                // Observabilidade: se estiver em transição, isso pode ser um sinal de uso indevido.
                if (_gateService != null && _gateService.IsTokenActive(SimulationGateTokens.SceneTransition))
                {
                    DebugUtility.LogWarning<WorldResetRequestService>(
                        $"[WorldLifecycle] RequestResetAsync chamado durante SceneTransition. source='{source ?? "<null>"}', activeScene='{activeScene}'.");
                }

                var controller = FindControllerForActiveScene(activeScene);
                if (controller == null)
                {
                    DebugUtility.LogWarning<WorldResetRequestService>(
                        $"[WorldLifecycle] Nenhum WorldLifecycleController encontrado para RequestResetAsync. source='{source ?? "<null>"}', activeScene='{activeScene}'.");
                    return;
                }

                DebugUtility.LogVerbose<WorldResetRequestService>(
                    $"[WorldLifecycle] RequestResetAsync → ResetWorldAsync. source='{source ?? "<null>"}', scene='{activeScene}'.",
                    DebugUtility.Colors.Info);

                await controller.ResetWorldAsync(source ?? "WorldReset/Request");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<WorldResetRequestService>(
                    $"[WorldLifecycle] Erro em RequestResetAsync. source='{source ?? "<null>"}', ex='{ex}'.");
            }
        }

        private static WorldLifecycleController FindControllerForActiveScene(string activeSceneName)
        {
            var all = UnityEngine.Object.FindObjectsOfType<WorldLifecycleController>(includeInactive: true);
            if (all == null || all.Length == 0)
            {
                return null;
            }

            string target = (activeSceneName ?? string.Empty).Trim();
            if (target.Length == 0)
            {
                return all[0];
            }

            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null)
                {
                    continue;
                }

                var scene = c.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                if (string.Equals(scene.name, target, StringComparison.Ordinal))
                {
                    return c;
                }
            }

            // Fallback: único controller existente.
            return all.Length == 1 ? all[0] : null;
        }
    }
}
