using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime
{
    /// <summary>
    /// Entry-point de producao para solicitar ResetWorld fora de QA.
    /// Encaminha diretamente ao owner canonico do lifecycle macro.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldResetRequestService : IWorldResetRequestService
    {
        private readonly ISimulationGateService _gateService;
        private readonly IWorldResetService _resetService;

        public WorldResetRequestService(
            IWorldResetService resetService,
            ISimulationGateService gateService)
        {
            _resetService = resetService ?? throw new ArgumentNullException(nameof(resetService));
            _gateService = gateService ?? throw new ArgumentNullException(nameof(gateService));
        }

        public async Task RequestResetAsync(string source)
        {
            string activeScene = SceneManager.GetActiveScene().name ?? string.Empty;
            string normalizedSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
            string reason = normalizedSource.StartsWith(WorldResetReasons.ProductionTriggerPrefix, StringComparison.Ordinal)
                ? normalizedSource
                : $"{WorldResetReasons.ProductionTriggerPrefix}{normalizedSource}";

            string signature = $"directReset:scene={activeScene};src={normalizedSource}";
            DebugUtility.LogVerbose(typeof(WorldResetRequestService),
                $"[OBS][WorldReset] ResetRequested signature='{signature}' sourceSignature='{signature}' target='{activeScene}' reason='{reason}' source='{normalizedSource}' scene='{activeScene}'.",
                DebugUtility.Colors.Info);

            if (_gateService.IsTokenActive(SimulationGateTokens.SceneTransition))
            {
                DebugUtility.LogWarning<WorldResetRequestService>(
                    $"[{ResetLogTags.Guarded}] [WorldReset] RequestResetAsync chamado durante SceneTransition. source='{source ?? "<null>"}', activeScene='{activeScene}'.");
            }

            var request = new WorldResetRequest(
                kind: ResetKind.Macro,
                contextSignature: signature,
                reason: reason,
                targetScene: activeScene,
                origin: WorldResetOrigin.Manual,
                sourceSignature: signature);

            DebugUtility.LogVerbose<WorldResetRequestService>(
                $"[OBS][WorldReset] RequestResetAsync -> IWorldResetService.TriggerResetAsync. source='{normalizedSource}', scene='{activeScene}', reason='{reason}'.",
                DebugUtility.Colors.Info);

            await _resetService.TriggerResetAsync(request);
        }
    }
}

