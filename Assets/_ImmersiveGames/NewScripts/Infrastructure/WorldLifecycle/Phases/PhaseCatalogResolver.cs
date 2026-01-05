using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Resolve PhaseDefinition via PhaseCatalog em Resources.
    /// </summary>
    public sealed class PhaseCatalogResolver : IPhaseDefinitionResolver
    {
        public const string DefaultCatalogPath = "Phases/PhaseCatalog";

        private readonly Dictionary<string, PhaseDefinition> _cache = new();
        private PhaseCatalog _catalog;

        public PhaseDefinition Resolve(PhaseId phaseId)
        {
            if (!phaseId.IsValid)
            {
                DebugUtility.LogWarning(typeof(PhaseCatalogResolver),
                    "[Phase] Resolve ignored (invalid phaseId).");
                return null;
            }

            if (_cache.TryGetValue(phaseId.Value, out var cached) && cached != null)
            {
                return cached;
            }

            var catalog = EnsureCatalogLoaded();
            if (catalog == null)
            {
                DebugUtility.LogWarning(typeof(PhaseCatalogResolver),
                    $"[Phase] PhaseCatalog não disponível. phaseId='{phaseId.Value}'.");
                return null;
            }

            if (!catalog.TryGetById(phaseId, out var definition) || definition == null)
            {
                DebugUtility.LogWarning(typeof(PhaseCatalogResolver),
                    $"[Phase] PhaseDefinition não encontrada. phaseId='{phaseId.Value}'.");
                return null;
            }

            _cache[phaseId.Value] = definition;
            return definition;
        }

        private PhaseCatalog EnsureCatalogLoaded()
        {
            if (_catalog != null)
            {
                return _catalog;
            }

            var catalog = Resources.Load<PhaseCatalog>(DefaultCatalogPath);
            if (catalog == null)
            {
                DebugUtility.LogWarning(typeof(PhaseCatalogResolver),
                    $"[Phase] PhaseCatalog não encontrado em Resources/{DefaultCatalogPath}.");
                return null;
            }

            _catalog = catalog;

            DebugUtility.LogVerbose(typeof(PhaseCatalogResolver),
                $"[Phase] PhaseCatalog carregado: name='{catalog.name}', path='{DefaultCatalogPath}'.");

            return _catalog;
        }
    }
}
