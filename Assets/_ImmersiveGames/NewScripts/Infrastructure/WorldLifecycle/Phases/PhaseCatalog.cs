using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Catálogo de definições de fase (ScriptableObject).
    /// </summary>
    [CreateAssetMenu(
        fileName = "PhaseCatalog",
        menuName = "ImmersiveGames/Phases/Phase Catalog",
        order = 1)]
    public sealed class PhaseCatalog : ScriptableObject
    {
        [SerializeField]
        private List<PhaseDefinition> phases = new();

        public IReadOnlyList<PhaseDefinition> Phases => phases;

        public bool TryGetById(PhaseId phaseId, out PhaseDefinition definition)
        {
            definition = null;

            if (!phaseId.IsValid || phases == null)
            {
                return false;
            }

            for (int i = 0; i < phases.Count; i++)
            {
                var entry = phases[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.Id == phaseId)
                {
                    definition = entry;
                    return true;
                }
            }

            return false;
        }
    }
}
