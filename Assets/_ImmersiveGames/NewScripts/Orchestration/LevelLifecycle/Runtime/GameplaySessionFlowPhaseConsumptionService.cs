using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionFlowPhaseConsumptionService : IDisposable
    {
        private readonly EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private readonly IGameplaySessionContextService _sessionContextService;
        private readonly IGameplayPhaseRuntimeService _phaseRuntimeService;
        private readonly IGameplayPhasePlayerParticipationService _phasePlayersService;
        private readonly IGameplayPhaseRulesObjectivesService _phaseRulesObjectivesService;
        private readonly IGameplayPhaseInitialStateService _phaseInitialStateService;
        private bool _disposed;

        public GameplaySessionFlowPhaseConsumptionService(
            IGameplaySessionContextService sessionContextService,
            IGameplayPhaseRuntimeService phaseRuntimeService,
            IGameplayPhasePlayerParticipationService phasePlayersService,
            IGameplayPhaseRulesObjectivesService phaseRulesObjectivesService,
            IGameplayPhaseInitialStateService phaseInitialStateService)
        {
            _sessionContextService = sessionContextService ?? throw new ArgumentNullException(nameof(sessionContextService));
            _phaseRuntimeService = phaseRuntimeService ?? throw new ArgumentNullException(nameof(phaseRuntimeService));
            _phasePlayersService = phasePlayersService ?? throw new ArgumentNullException(nameof(phasePlayersService));
            _phaseRulesObjectivesService = phaseRulesObjectivesService ?? throw new ArgumentNullException(nameof(phaseRulesObjectivesService));
            _phaseInitialStateService = phaseInitialStateService ?? throw new ArgumentNullException(nameof(phaseInitialStateService));

            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(OnPhaseDefinitionSelected);
            EventBus<PhaseDefinitionSelectedEvent>.Register(_phaseSelectedBinding);

            DebugUtility.LogVerbose<GameplaySessionFlowPhaseConsumptionService>(
                "[OBS][GameplaySessionFlow][PhaseDefinition] GameplaySessionFlowPhaseConsumptionService registrado como consumidor explicito da phase resolvida.",
                DebugUtility.Colors.Info);
        }

        private void OnPhaseDefinitionSelected(PhaseDefinitionSelectedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (!evt.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowPhaseConsumptionService),
                    "[FATAL][H1][GameplaySessionFlow] PhaseDefinitionSelectedEvent invalido recebido pelo consumidor da phase.");
            }

            DebugUtility.Log<GameplaySessionFlowPhaseConsumptionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionConsumptionStarted phaseId='{evt.PhaseId}' routeId='{evt.MacroRouteId}' v='{evt.SelectionVersion}' reason='{evt.Reason}'.",
                DebugUtility.Colors.Info);

            GameplaySessionContextSnapshot sessionContext = _sessionContextService.UpdateFromPhaseDefinitionSelectedEvent(evt);
            GameplayPhaseRuntimeSnapshot phaseRuntime = _phaseRuntimeService.UpdateFromPhaseDefinitionSelectedEvent(evt);
            GameplayPhasePlayerParticipationSnapshot players = _phasePlayersService.UpdateFromPhaseDefinitionSelectedEvent(evt);
            GameplayPhaseRulesObjectivesSnapshot rulesObjectives = _phaseRulesObjectivesService.UpdateFromPhaseDefinitionSelectedEvent(evt);
            GameplayPhaseInitialStateSnapshot initialState = _phaseInitialStateService.UpdateFromPhaseDefinitionSelectedEvent(evt);

            DebugUtility.Log<GameplaySessionFlowPhaseConsumptionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionConsumptionCompleted phaseSignature='{phaseRuntime.PhaseRuntimeSignature}' sessionSignature='{sessionContext.SessionSignature}' runResultStage='{phaseRuntime.HasRunResultStage}' playersSignature='{players.ParticipationSignature}' rulesSignature='{rulesObjectives.RulesSignature}' initialStateSignature='{initialState.InitialStateSignature}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<PhaseDefinitionSelectedEvent>.Unregister(_phaseSelectedBinding);
        }
    }
}
