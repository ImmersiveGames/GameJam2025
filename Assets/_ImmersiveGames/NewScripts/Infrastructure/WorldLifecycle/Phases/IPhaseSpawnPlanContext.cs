namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Serviço de escopo de cena para expor o PhaseSpawnPlan durante o reset.
    /// </summary>
    public interface IPhaseSpawnPlanContext
    {
        PhaseSpawnPlan CurrentPlan { get; }

        void SetPlan(PhaseSpawnPlan plan);
    }
}
