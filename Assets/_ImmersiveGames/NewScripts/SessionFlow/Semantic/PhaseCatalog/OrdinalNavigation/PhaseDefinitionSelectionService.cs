using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation
{
    public sealed class PhaseDefinitionSelectionService : IPhaseDefinitionSelectionService
    {
        private readonly IPhaseCatalogRuntimeStateService _runtimeStateService;

        public PhaseDefinitionSelectionService(IPhaseCatalogRuntimeStateService runtimeStateService)
        {
            _runtimeStateService = runtimeStateService ?? throw new ArgumentNullException(nameof(runtimeStateService));

            if (_runtimeStateService.CurrentCommitted == null || !_runtimeStateService.CurrentCommitted.PhaseId.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Runtime catalog state requires a valid currentCommitted phase.");
            }

            DebugUtility.LogVerbose(typeof(PhaseDefinitionSelectionService),
                $"[OBS][PhaseDefinition] Selected phase resolved from runtime catalog state phaseId='{Current.PhaseId}' asset='{Current.name}' source='current_committed'.",
                DebugUtility.Colors.Info);
        }

        public PhaseDefinitionId SelectedPhaseDefinitionId => Current != null ? Current.PhaseId : PhaseDefinitionId.None;
        public PhaseDefinitionAsset Current => _runtimeStateService.CurrentCommitted;

        public bool TryGetCurrent(out PhaseDefinitionAsset phaseDefinition)
        {
            phaseDefinition = Current;
            return phaseDefinition != null;
        }

        public PhaseDefinitionAsset ResolveOrFail()
        {
            if (Current == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Selected phase was not resolved.");
            }

            return Current;
        }
    }
}

