using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunOutcome.EndConditions
{
    [DisallowMultipleComponent]
    public sealed class GameplayOutcomeQaPanel : MonoBehaviour
    {
        private const string VictoryReason = "QA/BaselineV3/VictoryButton";
        private const string DefeatReason = "QA/BaselineV3/DefeatButton";
        private const float MinPanelWidth = 460f;
        private const float MinPanelHeight = 260f;

        [Header("Layout")]
        [SerializeField] private Rect panelRect = new(16f, 16f, 460f, 240f);
        [SerializeField] private string title = "Baseline V3 Outcome Mock";

        [Inject] private IGameRunEndRequestService _endRequest;
        [Inject] private IGameLoopService _gameLoopService;
        [Inject] private IGameplaySessionContextService _sessionContextService;
        [Inject] private IGameplayPhaseRuntimeService _phaseRuntimeService;
        [Inject] private IGameplayPhasePlayerParticipationService _phasePlayersService;
        [Inject] private IGameplayPhaseRulesObjectivesService _phaseRulesObjectivesService;
        [Inject] private IGameplayPhaseInitialStateService _phaseInitialStateService;

        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private EventBinding<GameRunEndedEvent> _runEndedBinding;
        private EventBinding<GamePlayRequestedEvent> _gamePlayRequestedBinding;
        private EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private EventBinding<PhaseIntroStageEntryEvent> _phaseIntroStageEntryBinding;
        private EventBinding<LevelIntroCompletedEvent> _levelIntroCompletedBinding;
        private bool _registered;
        private bool _runEnded;

        private void Awake()
        {
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(_ => _runEnded = false);
            _runEndedBinding = new EventBinding<GameRunEndedEvent>(_ => _runEnded = true);
            _gamePlayRequestedBinding = new EventBinding<GamePlayRequestedEvent>(evt => ReportSmoke("GamePlayRequestedEvent", evt.Reason));
            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(evt => ReportSmoke("PhaseDefinitionSelectedEvent", evt.Reason));
            _phaseIntroStageEntryBinding = new EventBinding<PhaseIntroStageEntryEvent>(evt => ReportSmoke("PhaseIntroStageEntryEvent", evt.Session.Reason));
            _levelIntroCompletedBinding = new EventBinding<LevelIntroCompletedEvent>(evt => ReportSmoke("LevelIntroCompletedEvent", evt.Reason));
        }

        private void OnEnable()
        {
            EnsureDependenciesInjected();
            RegisterBindings();
            ReportSmoke("OnEnable", "panel_enabled");
        }

        private void OnDisable()
        {
            UnregisterBindings();
        }

        private void OnDestroy()
        {
            UnregisterBindings();
        }

        private void OnGUI()
        {
            if (!ShouldShow())
            {
                return;
            }

            EnsurePanelBounds();
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.BeginVertical();
            GUILayout.Label(title, GUI.skin.label);
            GUILayout.Space(4f);
            GUILayout.Label(BuildSmokeSummary(), GUI.skin.label);
            GUILayout.Space(8f);

            if (GUILayout.Button("Victory"))
            {
                RequestOutcome(GameRunOutcome.Victory, VictoryReason);
            }

            if (GUILayout.Button("Defeat"))
            {
                RequestOutcome(GameRunOutcome.Defeat, DefeatReason);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private bool ShouldShow()
        {
            EnsureDependenciesInjected();

            if (_endRequest == null || _gameLoopService == null)
            {
                return false;
            }

            if (_runEnded)
            {
                return false;
            }

            return string.Equals(_gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), System.StringComparison.Ordinal);
        }

        private void RequestOutcome(GameRunOutcome outcome, string reason)
        {
            EnsureDependenciesInjected();
            if (_endRequest == null)
            {
                DebugUtility.LogWarning<GameplayOutcomeQaPanel>(
                    $"[QA][BaselineV3] Outcome mock ignored: IGameRunEndRequestService unavailable. outcome='{outcome}'.",
                    this);
                return;
            }

            DebugUtility.Log<GameplayOutcomeQaPanel>(
                $"[QA][BaselineV3] Outcome mock requested. outcome='{outcome}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            _endRequest.RequestRunEnd(outcome, reason);
        }

        private void EnsureDependenciesInjected()
        {
            if (!DependencyManager.HasInstance)
            {
                return;
            }

            if (_endRequest == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _endRequest);
            }

            if (_gameLoopService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _gameLoopService);
            }

            if (_sessionContextService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _sessionContextService);
            }

            if (_phaseRuntimeService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _phaseRuntimeService);
            }

            if (_phasePlayersService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _phasePlayersService);
            }

            if (_phaseRulesObjectivesService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _phaseRulesObjectivesService);
            }

            if (_phaseInitialStateService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _phaseInitialStateService);
            }
        }

        private void EnsurePanelBounds()
        {
            if (panelRect.width < MinPanelWidth)
            {
                panelRect.width = MinPanelWidth;
            }

            if (panelRect.height < MinPanelHeight)
            {
                panelRect.height = MinPanelHeight;
            }
        }

        private void RegisterBindings()
        {
            if (_registered)
            {
                return;
            }

            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            EventBus<GameRunEndedEvent>.Register(_runEndedBinding);
            EventBus<GamePlayRequestedEvent>.Register(_gamePlayRequestedBinding);
            EventBus<PhaseDefinitionSelectedEvent>.Register(_phaseSelectedBinding);
            EventBus<PhaseIntroStageEntryEvent>.Register(_phaseIntroStageEntryBinding);
            EventBus<LevelIntroCompletedEvent>.Register(_levelIntroCompletedBinding);
            _registered = true;
        }

        private void UnregisterBindings()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            EventBus<GameRunEndedEvent>.Unregister(_runEndedBinding);
            EventBus<GamePlayRequestedEvent>.Unregister(_gamePlayRequestedBinding);
            EventBus<PhaseDefinitionSelectedEvent>.Unregister(_phaseSelectedBinding);
            EventBus<PhaseIntroStageEntryEvent>.Unregister(_phaseIntroStageEntryBinding);
            EventBus<LevelIntroCompletedEvent>.Unregister(_levelIntroCompletedBinding);
            _registered = false;
        }

        [ContextMenu("Smoke/GameplaySession/Report")]
        private void ReportGameplaySessionSmoke()
        {
            ReportSmoke("ContextMenu/Report", "manual");
        }

        [ContextMenu("Smoke/GameplaySession/Clear")]
        private void ClearGameplaySessionSmoke()
        {
            GameplaySessionFlowSmokeReporter.ClearCurrentState("GameplayOutcomeQaPanel/ContextMenuClear");
            ReportSmoke("ContextMenu/Clear", "manual");
        }

        private void ReportSmoke(string stage, string reason)
        {
            EnsureDependenciesInjected();
            GameplaySessionFlowSmokeReporter.ReportCurrentState(stage, reason);
        }

        private string BuildSmokeSummary()
        {
            GameplaySessionContextSnapshot session = GameplaySessionContextSnapshot.Empty;
            GameplayPhaseRuntimeSnapshot phase = GameplayPhaseRuntimeSnapshot.Empty;
            GameplayPhasePlayerParticipationSnapshot players = GameplayPhasePlayerParticipationSnapshot.Empty;
            GameplayPhaseRulesObjectivesSnapshot rulesObjectives = GameplayPhaseRulesObjectivesSnapshot.Empty;
            GameplayPhaseInitialStateSnapshot initialState = GameplayPhaseInitialStateSnapshot.Empty;

            bool hasSession = _sessionContextService != null && _sessionContextService.TryGetCurrent(out session);
            bool hasPhase = _phaseRuntimeService != null && _phaseRuntimeService.TryGetCurrent(out phase);
            bool hasPlayers = _phasePlayersService != null && _phasePlayersService.TryGetCurrent(out players);
            bool hasRulesObjectives = _phaseRulesObjectivesService != null && _phaseRulesObjectivesService.TryGetCurrent(out rulesObjectives);
            bool hasInitialState = _phaseInitialStateService != null && _phaseInitialStateService.TryGetCurrent(out initialState);

            string sessionLabel = hasSession
                ? $"Session: OK [phase:{session.PhaseId}] v{session.SelectionVersion}"
                : "Session: empty";

            string phaseLabel = hasPhase
                ? $"Phase: OK [{(phase.PhaseDefinitionRef != null ? phase.PhaseDefinitionRef.name : "<none>")}] v{phase.SessionContext.SelectionVersion}"
                : "Phase: empty";

            string playersLabel = hasPlayers
                ? $"Players: OK [{players.ParticipationMode}] x{players.ParticipatingPlayerCount} primary={players.PrimaryParticipantId}"
                : "Players: empty";

            string rulesObjectivesLabel = hasRulesObjectives
                ? $"Rules/Objectives: OK rules={rulesObjectives.RuleEntryCount} objectives={rulesObjectives.ObjectiveEntryCount} primary={rulesObjectives.PrimaryObjectiveId}"
                : "Rules/Objectives: empty";

            string initialStateLabel = hasInitialState
                ? $"InitialState: OK seed={initialState.SeedSource}"
                : "InitialState: empty";

            string linkLabel = BuildLinkSummary(hasSession, hasPhase, hasPlayers, hasRulesObjectives, hasInitialState, session, phase, players, rulesObjectives, initialState);

            return $"{sessionLabel} | {phaseLabel} | {playersLabel} | {rulesObjectivesLabel} | {initialStateLabel} | {linkLabel}";
        }

        private static string BuildLinkSummary(
            bool hasSession,
            bool hasPhase,
            bool hasPlayers,
            bool hasRulesObjectives,
            bool hasInitialState,
            GameplaySessionContextSnapshot session,
            GameplayPhaseRuntimeSnapshot phase,
            GameplayPhasePlayerParticipationSnapshot players,
            GameplayPhaseRulesObjectivesSnapshot rulesObjectives,
            GameplayPhaseInitialStateSnapshot initialState)
        {
            string sessionPhase = hasSession && hasPhase
                ? string.Equals(session.SessionSignature, phase.SessionContext.SessionSignature, System.StringComparison.Ordinal)
                    ? "S-P: linked"
                    : "S-P: mismatch"
                : (hasSession ? "S-P: phase empty" : (hasPhase ? "S-P: session empty" : "S-P: empty"));

            string phasePlayers = hasPhase && hasPlayers
                ? string.Equals(phase.PhaseRuntimeSignature, players.PhaseRuntime.PhaseRuntimeSignature, System.StringComparison.Ordinal)
                    ? "P-Players: linked"
                    : "P-Players: mismatch"
                : (hasPhase ? "P-Players: players empty" : (hasPlayers ? "P-Players: phase empty" : "P-Players: empty"));

            string phaseRules = hasPhase && hasRulesObjectives
                ? string.Equals(phase.PhaseRuntimeSignature, rulesObjectives.PhaseRuntime.PhaseRuntimeSignature, System.StringComparison.Ordinal)
                    ? "P-Rules: linked"
                    : "P-Rules: mismatch"
                : (hasPhase ? "P-Rules: rules empty" : (hasRulesObjectives ? "P-Rules: phase empty" : "P-Rules: empty"));

            string rulesInitial = hasRulesObjectives && hasInitialState
                ? string.Equals(rulesObjectives.RulesSignature, initialState.RulesObjectives.RulesSignature, System.StringComparison.Ordinal)
                    ? "Rules-Initial: linked"
                    : "Rules-Initial: mismatch"
                : (hasRulesObjectives ? "Rules-Initial: initial empty" : (hasInitialState ? "Rules-Initial: rules empty" : "Rules-Initial: empty"));

            return $"{sessionPhase} | {phasePlayers} | {phaseRules} | {rulesInitial}";
        }
    }
}
