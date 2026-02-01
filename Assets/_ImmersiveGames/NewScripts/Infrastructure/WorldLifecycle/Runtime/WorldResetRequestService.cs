using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
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

        private const string ProductionTriggerPrefix = "ProductionTrigger/";
        private const string ManualProfile = "manual";

        public WorldResetRequestService(ISimulationGateService gateService = null)
        {
            _gateService = gateService;
        }

        public async Task RequestResetAsync(string source)
        {
            try
            {
                string activeScene = SceneManager.GetActiveScene().name ?? string.Empty;
                string normalizedSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
                string reason = normalizedSource.StartsWith(ProductionTriggerPrefix, StringComparison.Ordinal)
                    ? normalizedSource
                    : $"{ProductionTriggerPrefix}{normalizedSource}";

                // Observabilidade canônica (Contrato): ResetRequested com sourceSignature/reason/profile/target.
                // Como este caminho não passa pelo SceneFlow, usamos uma assinatura manual correlacionável.
                string signature = $"directReset:scene={activeScene};src={normalizedSource}";
                DebugUtility.LogVerbose(typeof(WorldResetRequestService),
                    $"[OBS][WorldLifecycle] ResetRequested signature='{signature}' sourceSignature='{signature}' profile='{ManualProfile}' target='{activeScene}' reason='{reason}' source='{normalizedSource}' scene='{activeScene}'.",
                    DebugUtility.Colors.Info);

                // Observabilidade: se estiver em transição, isso pode ser um sinal de uso indevido.
                if (_gateService != null && _gateService.IsTokenActive(SimulationGateTokens.SceneTransition))
                {
                    DebugUtility.LogWarning<WorldResetRequestService>(
                        $"[WorldLifecycle] RequestResetAsync chamado durante SceneTransition. source='{source ?? "<null>"}', activeScene='{activeScene}'.");
                }

                var controller = WorldLifecycleControllerLocator.FindSingleForSceneOrFallback(activeScene);
                if (controller == null)
                {
                    DebugUtility.LogWarning<WorldResetRequestService>(
                        $"[WorldLifecycle] Nenhum WorldLifecycleController encontrado para RequestResetAsync. source='{source ?? "<null>"}', activeScene='{activeScene}'.");
                    return;
                }

                DebugUtility.LogVerbose<WorldResetRequestService>(
                    $"[WorldLifecycle] RequestResetAsync → ResetWorldAsync. source='{normalizedSource}', scene='{activeScene}', reason='{reason}'.",
                    DebugUtility.Colors.Info);

                await controller.ResetWorldAsync(reason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<WorldResetRequestService>(
                    $"[WorldLifecycle] Erro em RequestResetAsync. source='{source ?? "<null>"}', ex='{ex}'.");
            }
        }

    }
}
