using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition
{
    public interface IPhaseDefinitionCatalog
    {
        IReadOnlyCollection<string> PhaseIds { get; }

        bool TryGet(string phaseId, out PhaseDefinitionAsset phaseDefinition);
    }
}
