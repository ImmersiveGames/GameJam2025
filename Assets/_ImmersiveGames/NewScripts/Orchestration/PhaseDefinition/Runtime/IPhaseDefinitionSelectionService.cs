namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public interface IPhaseDefinitionSelectionService
    {
        PhaseDefinitionId SelectedPhaseDefinitionId { get; }
        PhaseDefinitionAsset Current { get; }
        bool TryGetCurrent(out PhaseDefinitionAsset phaseDefinition);
        PhaseDefinitionAsset ResolveOrFail();
    }
}
