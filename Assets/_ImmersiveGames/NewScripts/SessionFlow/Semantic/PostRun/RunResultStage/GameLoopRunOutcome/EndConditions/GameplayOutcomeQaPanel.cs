using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Diagnostics;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.RunResultStage.GameLoopRunOutcome.EndConditions
{
    [DisallowMultipleComponent]
    public sealed class GameplayOutcomeQaPanel : MonoBehaviour
    {
        private const string VictoryReason = "QA/BaselineV3/VictoryButton";
        private const string DefeatReason = "QA/BaselineV3/DefeatButton";
        private const float MinPanelWidth = 760f;
        private const float MinPanelHeight = 360f;

        [Header("Layout")]
        [SerializeField] private Rect panelRect = new(16f, 16f, 760f, 360f);
        [SerializeField] private string title = "Run Outcome QA";

        [Inject] private IGameRunEndRequestService _endRequest;
        [Inject] private IGameLoopService _gameLoopService;
        [Inject] private IGameplaySessionContextService _sessionContextService;
        [Inject] private IGameplayPhaseRuntimeService _phaseRuntimeService;
        [Inject] private IGameplayParticipationFlowService _participationFlowService;

        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private EventBinding<GameRunEndedEvent> _runEndedBinding;
        private EventBinding<GamePlayRequestedEvent> _gamePlayRequestedBinding;
        private EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private EventBinding<IntroStageEntryEvent> _introStageEntryBinding;
        private EventBinding<IntroStageCompletedEvent> _introStageCompletedBinding;
        private bool _registered;
        private bool _runEnded;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;

        private void Awake()
        {
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(_ => _runEnded = false);
            _runEndedBinding = new EventBinding<GameRunEndedEvent>(_ => _runEnded = true);
            _gamePlayRequestedBinding = new EventBinding<GamePlayRequestedEvent>(evt => ReportSmoke("GamePlayRequestedEvent", evt.Reason));
            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(evt => ReportSmoke("PhaseDefinitionSelectedEvent", evt.Reason));
            _introStageEntryBinding = new EventBinding<IntroStageEntryEvent>(evt => ReportSmoke("IntroStageEntryEvent", evt.Session.Reason));
            _introStageCompletedBinding = new EventBinding<IntroStageCompletedEvent>(evt => ReportSmoke("IntroStageCompletedEvent", evt.Reason));
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
            EnsureStyles();
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.BeginVertical();
            GUILayout.Label(title, _titleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(BuildSmokeSummary(), _bodyStyle);
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Victory", _buttonStyle, GUILayout.Height(42f)))
            {
                RequestOutcome(GameRunOutcome.Victory, VictoryReason);
            }

            if (GUILayout.Button("Defeat", _buttonStyle, GUILayout.Height(42f)))
            {
                RequestOutcome(GameRunOutcome.Defeat, DefeatReason);
            }
            GUILayout.EndHorizontal();

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

            if (_participationFlowService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _participationFlowService);
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

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 27,
                    wordWrap = true
                };
            }

            if (_bodyStyle == null)
            {
                _bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 21,
                    wordWrap = true
                };
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 21,
                    wordWrap = true
                };
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
            EventBus<IntroStageEntryEvent>.Register(_introStageEntryBinding);
            EventBus<IntroStageCompletedEvent>.Register(_introStageCompletedBinding);
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
            EventBus<IntroStageEntryEvent>.Unregister(_introStageEntryBinding);
            EventBus<IntroStageCompletedEvent>.Unregister(_introStageCompletedBinding);
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
            ParticipationSnapshot participation = ParticipationSnapshot.Empty;

            bool hasSession = _sessionContextService != null && _sessionContextService.TryGetCurrent(out session);
            bool hasPhase = _phaseRuntimeService != null && _phaseRuntimeService.TryGetCurrent(out phase);
            bool hasParticipation = _participationFlowService != null && _participationFlowService.TryGetCurrent(out participation);

            string sessionLabel = hasSession
                ? $"Session: {session.PhaseId} v{session.SelectionVersion}"
                : "Session: empty";

            string phaseLabel = hasPhase
                ? $"Phase: {(phase.PhaseDefinitionRef != null ? phase.PhaseDefinitionRef.name : "<none>")} v{phase.SessionContext.SelectionVersion}"
                : "Phase: empty";

            string playersLabel = hasParticipation
                ? $"Players: x{participation.ParticipantCount} primary={participation.PrimaryParticipantId} readiness={participation.Readiness.State}"
                : "Players: empty";

            string linkLabel = BuildLinkSummary(hasSession, hasPhase, hasParticipation, session, phase, participation);

            return $"{sessionLabel} | {phaseLabel}\n{playersLabel}\n{linkLabel}";
        }

        private static string BuildLinkSummary(
            bool hasSession,
            bool hasPhase,
            bool hasParticipation,
            GameplaySessionContextSnapshot session,
            GameplayPhaseRuntimeSnapshot phase,
            ParticipationSnapshot participation)
        {
            string sessionPhase = hasSession && hasPhase
                ? string.Equals(session.SessionSignature, phase.SessionContext.SessionSignature, System.StringComparison.Ordinal)
                    ? "S-P: linked"
                    : "S-P: mismatch"
                : "S-P: empty";

            string phasePlayers = hasPhase && hasParticipation
                ? string.Equals(phase.PhaseRuntimeSignature, participation.PhaseSignature, System.StringComparison.Ordinal)
                    ? "P-Players: linked"
                    : "P-Players: mismatch"
                : "P-Players: empty";

            return $"{sessionPhase} | {phasePlayers}";
        }
    }
}

