using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts
{
    public enum PhaseCatalogTraversalMode
    {
        Finite = 0,
        Looping = 1
    }

    public interface IPhaseDefinitionCatalog
    {
        IReadOnlyList<string> PhaseIds { get; }

        PhaseCatalogTraversalMode TraversalMode { get; }

        bool TryGet(string phaseId, out PhaseDefinitionAsset phaseDefinition);

        PhaseDefinitionAsset ResolveInitialOrFail();

        PhaseDefinitionAsset ResolveNextOrFail(string phaseId);

        bool TryGetNext(string phaseId, out PhaseDefinitionAsset nextPhaseDefinition);

        PhaseDefinitionAsset ResolvePreviousOrFail(string phaseId);

        bool TryGetPrevious(string phaseId, out PhaseDefinitionAsset previousPhaseDefinition);

        PhaseDefinitionAsset ResolveSpecificPhaseOrFail(string phaseId);
    }
}

