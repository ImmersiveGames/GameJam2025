using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Guards
{
    /// <summary>
    /// Guard baseado nos tokens do SimulationGate (flow.scene_transition / sim.gameplay).
    /// Valida pre-condicoes obrigatorias do trilho macro.
    /// </summary>
    public sealed class SimulationGateWorldResetGuard : IWorldResetGuard
    {
        private readonly ISimulationGateService _gateService;

        public SimulationGateWorldResetGuard(ISimulationGateService gateService)
        {
            _gateService = gateService;
        }

        public ResetDecision Evaluate(WorldResetRequest request, IWorldResetPolicy policy)
        {
            _ = policy;

            if (_gateService == null)
            {
                string detail = "ISimulationGateService ausente para validar gate do reset macro.";
                DebugUtility.LogError(typeof(SimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}] {detail} request={request}");
                return ResetDecision.Skip("Guard_MissingSimulationGateService", detail, publishCompletion: true, isViolation: true);
            }

            bool sceneTransition = _gateService.IsTokenActive(SimulationGateTokens.SceneTransition);
            bool gameplaySimulation = _gateService.IsTokenActive(SimulationGateTokens.GameplaySimulation);

            if (request.Origin == WorldResetOrigin.SceneFlow && !sceneTransition)
            {
                string detail = $"SceneFlow reset sem token '{SimulationGateTokens.SceneTransition}' ativo.";
                DebugUtility.LogWarning(typeof(SimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}][STRICT_VIOLATION] {detail} request={request}");
                return ResetDecision.Skip("Guard_MissingSceneTransitionToken", detail, publishCompletion: true, isViolation: true);
            }

            if (request.Origin != WorldResetOrigin.SceneFlow && sceneTransition)
            {
                string detail = "Reset nao-SceneFlow solicitado durante SceneTransition.";
                DebugUtility.LogWarning(typeof(SimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}][DEGRADED_MODE] {detail} request={request}");
                return ResetDecision.Skip("Guard_ResetDuringSceneTransition", detail, publishCompletion: true, isViolation: true);
            }

            if (gameplaySimulation)
            {
                string detail = "Reset solicitado com sim.gameplay ativo.";
                DebugUtility.LogWarning(typeof(SimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}][DEGRADED_MODE] {detail} request={request}");
                return ResetDecision.Skip("Guard_ResetDuringGameplaySimulation", detail, publishCompletion: true, isViolation: true);
            }

            return ResetDecision.Proceed();
        }
    }
}

