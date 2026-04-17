namespace ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime
{
    public interface IPhaseDefinitionSelectionService
    {
        PhaseDefinitionId SelectedPhaseDefinitionId { get; }
        PhaseDefinitionAsset Current { get; }
        bool TryGetCurrent(out PhaseDefinitionAsset phaseDefinition);
        PhaseDefinitionAsset ResolveOrFail();
    }
}

