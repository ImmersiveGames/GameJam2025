using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts
{
    public interface IPhaseDefinitionSelectionService
    {
        PhaseDefinitionId SelectedPhaseDefinitionId { get; }
        PhaseDefinitionAsset Current { get; }
        bool TryGetCurrent(out PhaseDefinitionAsset phaseDefinition);
        PhaseDefinitionAsset ResolveOrFail();
    }
}

