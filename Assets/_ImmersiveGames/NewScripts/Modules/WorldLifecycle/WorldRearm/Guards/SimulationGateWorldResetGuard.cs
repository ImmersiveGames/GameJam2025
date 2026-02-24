using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Guards
{
    /// <summary>
    /// Guard baseado nos tokens do SimulationGate (flow.scene_transition / sim.gameplay).
    /// Apenas observa e loga; não bloqueia por padrão (best-effort).
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
            if (_gateService == null)
            {
                if (policy != null && policy.IsStrict)
                {
                    DebugUtility.LogWarning(typeof(SimulationGateWorldResetGuard),
                        $"[{ResetLogTags.Guarded}][STRICT_VIOLATION] ISimulationGateService ausente. reset seguirá sem gate. request={request}");
                }
                else
                {
                    DebugUtility.LogWarning(typeof(SimulationGateWorldResetGuard),
                        $"[{ResetLogTags.Guarded}][DEGRADED_MODE] ISimulationGateService ausente. reset seguirá sem gate. request={request}");
                }

                return ResetDecision.Proceed();
            }

            bool sceneTransition = _gateService.IsTokenActive(SimulationGateTokens.SceneTransition);
            bool gameplaySimulation = _gateService.IsTokenActive(SimulationGateTokens.GameplaySimulation);

            if (request.Origin == WorldResetOrigin.SceneFlow && !sceneTransition)
            {
                DebugUtility.LogWarning(typeof(SimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}][STRICT_VIOLATION] SceneFlow reset sem token '{SimulationGateTokens.SceneTransition}' ativo. request={request}");
            }
            else if (request.Origin != WorldResetOrigin.SceneFlow && sceneTransition)
            {
                DebugUtility.LogWarning(typeof(SimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}][DEGRADED_MODE] Reset manual durante SceneTransition. request={request}");
            }

            if (gameplaySimulation)
            {
                DebugUtility.LogWarning(typeof(SimulationGateWorldResetGuard),
                    $"[{ResetLogTags.Guarded}][DEGRADED_MODE] Reset solicitado com sim.gameplay ativo. request={request}");
            }

            return ResetDecision.Proceed();
        }
    }
}
