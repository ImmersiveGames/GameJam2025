namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Resolve PhaseDefinition a partir de um catálogo.
    /// </summary>
    public interface IPhaseDefinitionResolver
    {
        PhaseDefinition Resolve(PhaseId phaseId);
    }
}
