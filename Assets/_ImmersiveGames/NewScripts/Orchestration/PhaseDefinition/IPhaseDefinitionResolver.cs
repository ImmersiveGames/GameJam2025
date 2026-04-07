namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition
{
    public interface IPhaseDefinitionResolver
    {
        IPhaseDefinitionCatalog Catalog { get; }

        bool TryResolve(string phaseId, out PhaseDefinitionAsset phaseDefinition);

        PhaseDefinitionAsset ResolveOrFail(string phaseId);
    }
}
