using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Domain;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Core
{
    /// <summary>
    /// Entry-point de produção para solicitar ResetWorld fora de QA.
    ///
    /// Implementação Unity-native:
    /// - Encaminha para o IWorldResetService canonico no DI.
    /// - Best-effort e defensiva: nunca lan?a para o caller.
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
                string normalizedSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
                string reason = normalizedSource.StartsWith(WorldResetReasons.ProductionTriggerPrefix, StringComparison.Ordinal)
                    ? normalizedSource
                    : $"{WorldResetReasons.ProductionTriggerPrefix}{normalizedSource}";

                // Observabilidade canônica (Contrato): ResetRequested com sourceSignature/reason/profile/target.
                // Como este caminho não passa pelo SceneFlow, usamos uma assinatura manual correlacionável.
                string signature = $"directReset:scene={activeScene};src={normalizedSource}";
                DebugUtility.LogVerbose(typeof(WorldResetRequestService),
                    $"[OBS][WorldLifecycle] ResetRequested signature='{signature}' sourceSignature='{signature}' profile='{WorldResetReasons.ManualProfile}' target='{activeScene}' reason='{reason}' source='{normalizedSource}' scene='{activeScene}'.",
                    DebugUtility.Colors.Info);

                var request = new WorldResetRequest(
                    contextSignature: signature,
                    reason: reason,
                    profileName: WorldResetReasons.ManualProfile,
                    targetScene: activeScene,
                    origin: WorldResetOrigin.Manual,
                    sourceSignature: signature,
                    isGameplayProfile: true);

                if (DependencyManager.HasInstance &&
                    DependencyManager.Provider.TryGetGlobal<IWorldResetService>(out var resetService) &&
                    resetService != null)
                {
                    DebugUtility.LogVerbose<WorldResetRequestService>(
                        $"[WorldLifecycle] RequestResetAsync -> IWorldResetService.TriggerResetAsync. source='{normalizedSource}', scene='{activeScene}', reason='{reason}'.",
                        DebugUtility.Colors.Info);
                    await resetService.TriggerResetAsync(request);
                    return;
                }

                // Observabilidade: se estiver em transição, isso pode ser um sinal de uso indevido.
                if (_gateService != null && _gateService.IsTokenActive(SimulationGateTokens.SceneTransition))
                {
                    DebugUtility.LogWarning<WorldResetRequestService>(
                        $"[{ResetLogTags.Guarded}][DEGRADED_MODE] [WorldLifecycle] RequestResetAsync chamado durante SceneTransition. source='{source ?? "<null>"}', activeScene='{activeScene}'.");
                }

                DebugUtility.LogError<WorldResetRequestService>(
                    $"[{ResetLogTags.Failed}][DEGRADED_MODE] [WorldLifecycle] IWorldResetService ausente. Reset manual ignorado. source='{source ?? "<null>"}', activeScene='{activeScene}'.");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<WorldResetRequestService>(
                    $"[WorldLifecycle] Erro em RequestResetAsync. source='{source ?? "<null>"}', ex='{ex}'.");
            }
        }

    }
}


