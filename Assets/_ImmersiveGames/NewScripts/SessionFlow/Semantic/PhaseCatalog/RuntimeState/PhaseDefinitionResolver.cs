using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.RuntimeState
{
    public sealed class PhaseDefinitionResolver : IPhaseDefinitionResolver
    {
        public PhaseDefinitionResolver(IPhaseDefinitionCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public IPhaseDefinitionCatalog Catalog { get; }

        public bool TryResolve(string phaseId, out PhaseDefinitionAsset phaseDefinition)
        {
            return Catalog.TryGet(phaseId, out phaseDefinition);
        }

        public PhaseDefinitionAsset ResolveOrFail(string phaseId)
        {
            if (string.IsNullOrWhiteSpace(phaseId))
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Invalid phaseId: empty value.");
            }

            if (TryResolve(phaseId, out PhaseDefinitionAsset phaseDefinition) && phaseDefinition != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionResolver),
                    $"[OBS][PhaseDefinition] Resolved phaseId='{PhaseDefinitionId.Normalize(phaseId)}' asset='{phaseDefinition.name}'.",
                    DebugUtility.Colors.Info);
                return phaseDefinition;
            }

            throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing phaseDefinition for phaseId='{PhaseDefinitionId.Normalize(phaseId)}'.");
        }
    }
}

