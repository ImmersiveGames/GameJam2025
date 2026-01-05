using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Armazena o PhaseSpawnPlan atual no escopo da cena.
    /// </summary>
    public sealed class PhaseSpawnPlanContext : IPhaseSpawnPlanContext
    {
        public PhaseSpawnPlan CurrentPlan { get; private set; }

        public void SetPlan(PhaseSpawnPlan plan)
        {
            CurrentPlan = plan;

            if (plan == null)
            {
                DebugUtility.LogWarning(typeof(PhaseSpawnPlanContext),
                    "[Phase] PhaseSpawnPlanContext recebeu plan nulo.");
            }
        }
    }
}
