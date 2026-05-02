using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.LegacySimulationGate;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Guards
{
    /// <summary>
    /// Guard baseado nos tokens do LegacySimulationGate (flow.scene_transition / sim.gameplay).
    /// Valida pre-condicoes obrigatorias do trilho macro.
    /// </summary>
    public sealed class LegacySimulationGateWorldResetGuard : IWorldResetGuard
    {
        private readonly ILegacySimulationGateService _gateService;

        public LegacySimulationGateWorldResetGuard(ILegacySimulationGateService gateService)
        {
            _gateService = gateService;
        }

        public ResetDecision Evaluate(WorldResetRequest request, IWorldResetPolicy policy)
        {
            _ = policy;

            if (_gateService == null)
            {
                string detail = "ILegacySimulationGateService ausente para validar gate do reset macro.";
                DebugUtility.LogError(typeof(LegacySimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}] {detail} request={request}");
                return ResetDecision.Skip("Guard_MissingLegacySimulationGateService", detail, publishCompletion: true, isViolation: true);
            }

            bool sceneTransition = _gateService.IsTokenActive(LegacySimulationGateTokens.SceneTransition);
            bool gameplaySimulation = _gateService.IsTokenActive(LegacySimulationGateTokens.GameplaySimulation);

            if (request.Origin == WorldResetOrigin.SceneFlow && !sceneTransition)
            {
                string detail = $"SceneFlow reset sem token '{LegacySimulationGateTokens.SceneTransition}' ativo.";
                DebugUtility.LogWarning(typeof(LegacySimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}][STRICT_VIOLATION] {detail} request={request}");
                return ResetDecision.Skip("Guard_MissingSceneTransitionToken", detail, publishCompletion: true, isViolation: true);
            }

            if (request.Origin != WorldResetOrigin.SceneFlow && sceneTransition)
            {
                string detail = "Reset nao-SceneFlow solicitado durante SceneTransition.";
                DebugUtility.LogWarning(typeof(LegacySimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}][DEGRADED_MODE] {detail} request={request}");
                return ResetDecision.Skip("Guard_ResetDuringSceneTransition", detail, publishCompletion: true, isViolation: true);
            }

            if (gameplaySimulation)
            {
                string detail = "Reset solicitado com sim.gameplay ativo.";
                DebugUtility.LogWarning(typeof(LegacySimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}][DEGRADED_MODE] {detail} request={request}");
                return ResetDecision.Skip("Guard_ResetDuringGameplaySimulation", detail, publishCompletion: true, isViolation: true);
            }

            return ResetDecision.Proceed();
        }
    }
}

