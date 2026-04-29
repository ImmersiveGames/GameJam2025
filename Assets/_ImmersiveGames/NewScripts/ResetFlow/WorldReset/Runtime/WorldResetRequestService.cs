using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
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
            _resetService = resetService ?? throw new System.ArgumentNullException(nameof(resetService));
            _gateService = gateService ?? throw new System.ArgumentNullException(nameof(gateService));
        }

        public async Task RequestResetAsync(WorldResetRequest request)
        {
            DebugUtility.LogVerbose(typeof(WorldResetRequestService),
                $"[OBS][WorldReset] ResetRequested correlationKey='{request.CorrelationKey}' signature='{request.ContextSignature}' sourceSignature='{request.SourceSignature}' target='{request.TargetScene}' reason='{request.Reason}' origin='{request.Origin}' shouldExecute={request.ShouldExecute}.",
                DebugUtility.Colors.Info);

            if (_gateService.IsTokenActive(SimulationGateTokens.SceneTransition))
            {
                DebugUtility.LogWarning<WorldResetRequestService>(
                    $"[{ResetLogTags.Guarded}] [WorldReset] RequestResetAsync chamado durante SceneTransition. correlationKey='{request.CorrelationKey}', signature='{request.ContextSignature}', targetScene='{request.TargetScene}'.");
            }

            await _resetService.TriggerResetAsync(request);
        }
    }
}
