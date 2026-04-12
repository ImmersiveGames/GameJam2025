using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition
{
    public interface IPhaseDefinitionCatalog
    {
        IReadOnlyList<string> PhaseIds { get; }

        bool TryGet(string phaseId, out PhaseDefinitionAsset phaseDefinition);

        PhaseDefinitionAsset ResolveInitialOrFail();

        PhaseDefinitionAsset ResolveNextOrFail(string phaseId);

        bool TryGetNext(string phaseId, out PhaseDefinitionAsset nextPhaseDefinition);

        PhaseDefinitionAsset ResolvePreviousOrFail(string phaseId);

        bool TryGetPrevious(string phaseId, out PhaseDefinitionAsset previousPhaseDefinition);
    }
}
