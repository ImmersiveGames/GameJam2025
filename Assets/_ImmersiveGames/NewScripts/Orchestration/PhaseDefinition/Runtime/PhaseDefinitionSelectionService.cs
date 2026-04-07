using System;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public sealed class PhaseDefinitionSelectionService : IPhaseDefinitionSelectionService
    {
        private readonly IPhaseDefinitionResolver _resolver;
        private readonly PhaseDefinitionId _selectedPhaseDefinitionId;
        private readonly PhaseDefinitionAsset _current;

        public PhaseDefinitionSelectionService(
            IPhaseDefinitionResolver resolver,
            PhaseDefinitionId selectedPhaseDefinitionId)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _selectedPhaseDefinitionId = selectedPhaseDefinitionId;

            if (!_selectedPhaseDefinitionId.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Selected phase id is required.");
            }

            _current = _resolver.ResolveOrFail(_selectedPhaseDefinitionId.Value);

            DebugUtility.LogVerbose(typeof(PhaseDefinitionSelectionService),
                $"[OBS][PhaseDefinition] Selected phase resolved id='{_selectedPhaseDefinitionId}' asset='{_current.name}'.",
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
