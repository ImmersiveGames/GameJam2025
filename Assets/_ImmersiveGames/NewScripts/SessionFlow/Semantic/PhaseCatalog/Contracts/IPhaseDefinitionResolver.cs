using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts
{
    public interface IPhaseDefinitionResolver
    {
        IPhaseDefinitionCatalog Catalog { get; }

        bool TryResolve(string phaseId, out PhaseDefinitionAsset phaseDefinition);

        PhaseDefinitionAsset ResolveOrFail(string phaseId);
    }
}

