using System;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public sealed class PhaseDefinitionSelectionService : IPhaseDefinitionSelectionService
    {
        private readonly PhaseDefinitionId _selectedPhaseDefinitionId;
        private readonly PhaseDefinitionAsset _current;

        public PhaseDefinitionSelectionService(IPhaseDefinitionCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _current = catalog.ResolveInitialOrFail();
            if (_current == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Catalog initial phase could not be resolved.");
            }

            _selectedPhaseDefinitionId = _current.PhaseId;

            if (!_selectedPhaseDefinitionId.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Selected phase reference is invalid.");
            }

            DebugUtility.LogVerbose(typeof(PhaseDefinitionSelectionService),
                $"[OBS][PhaseDefinition] Selected phase resolved from catalog index_0 source='phase_catalog_index_0' phaseId='{_selectedPhaseDefinitionId}' asset='{_current.name}'.",
                DebugUtility.Colors.Info);
        }

        public PhaseDefinitionId SelectedPhaseDefinitionId => _selectedPhaseDefinitionId;
        public PhaseDefinitionAsset Current => _current;

        public bool TryGetCurrent(out PhaseDefinitionAsset phaseDefinition)
        {
            phaseDefinition = _current;
            return _current != null;
        }

        public PhaseDefinitionAsset ResolveOrFail()
        {
            if (_current == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Selected phase was not resolved.");
            }

            return _current;
        }
    }
}
