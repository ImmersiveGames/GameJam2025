using System;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
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
                $"[OBS][PhaseDefinition] Selected phase resolved from runtime catalog state phaseId='{_runtimeStateService.CurrentCommitted.PhaseId}' asset='{_runtimeStateService.CurrentCommitted.name}'.",
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
