using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Experience.Frontend.UI.QA
{
    /// <summary>
    /// QA IMGUI para navegação entre phases na rota de gameplay.
    /// Mantém o painel fora da phase scene local e fala apenas com o trilho canônico de pós-level.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/Experience/Frontend/UI/QA/Phase Navigation QA Panel")]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseNavigationQaPanel : MonoBehaviour
    {
        private const string NextPhaseReason = "QA/PhaseNavigation/NextPhase";
        private const float PanelWidth = 560f;
        private const float PanelHeight = 240f;
        private const float PanelMargin = 16f;
        private const float ContentHeight = 110f;
        private const float LineHeight = 22f;
        private const float ButtonHeight = 34f;
        private const float ButtonSpacing = 6f;
        private const float SectionSpacing = 8f;

        [Header("Layout")]
        [SerializeField] private Rect panelRect = new(0f, 0f, PanelWidth, PanelHeight);
        [SerializeField] private string title = "Phase Navigation QA Mock";

        [Inject] private IPhaseNextPhaseService _phaseNextPhaseService;
        [Inject] private IGameplayPhaseRuntimeService _phaseRuntimeService;
        [Inject] private IPhaseDefinitionSelectionService _phaseSelectionService;
        [Inject] private IPhaseDefinitionCatalog _phaseDefinitionCatalog;
        [Inject] private IGameLoopService _gameLoopService;

        private EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private EventBinding<PhaseContentAppliedEvent> _phaseContentAppliedBinding;
        private EventBinding<IntroStageCompletedEvent> _introStageCompletedBinding;
        private EventBinding<GameRunStartedEvent> _gameRunStartedBinding;
        private bool _dependenciesInjected;
        private bool _registered;
        private bool _actionRequested;
        private bool _hasNextInCatalog;
        private bool _canAdvanceNow;
        private string _canAdvanceReason = string.Empty;
        private string _phaseLabel = string.Empty;
        private Vector2 _scrollPosition = Vector2.zero;
        private GUIStyle _wrappedLabelStyle;
        private GUIStyle _titleStyle;

        private void Awake()
        {
            EnsureDependenciesInjected();

            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(_ => RefreshView("PhaseDefinitionSelectedEvent"));
            _phaseContentAppliedBinding = new EventBinding<PhaseContentAppliedEvent>(_ => RefreshView("PhaseContentAppliedEvent"));
            _introStageCompletedBinding = new EventBinding<IntroStageCompletedEvent>(_ => RefreshView("IntroStageCompletedEvent"));
            _gameRunStartedBinding = new EventBinding<GameRunStartedEvent>(_ => RefreshView("GameRunStartedEvent"));
            RegisterBindings();
            RefreshView("Awake");
        }

        private void OnEnable()
        {
            RegisterBindings();
            RefreshView("OnEnable");
        }

        private void Start()
        {
            EnsureDependenciesInjected();
            RefreshView("Start");
        }

        private void OnDisable()
        {
            UnregisterBindings();
            _actionRequested = false;
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

            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(panelRect);
            GUILayout.BeginVertical(GUILayout.Width(panelRect.width), GUILayout.Height(panelRect.height));
            GUILayout.Label(title, _titleStyle);
            GUILayout.Space(SectionSpacing);

            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.Height(ContentHeight));

            GUILayout.Label(_phaseLabel, _wrappedLabelStyle, GUILayout.ExpandHeight(true));

            GUILayout.EndScrollView();

            GUILayout.Space(SectionSpacing);

            bool previousEnabled = GUI.enabled;
            GUI.enabled = !_actionRequested;

            GUI.enabled = previousEnabled && !_actionRequested && _phaseNextPhaseService != null && _canAdvanceNow;
            if (GUILayout.Button("Next Phase", GUILayout.Height(ButtonHeight)))
            {
                _ = ExecuteNextPhaseAsync(NextPhaseReason);
            }

            GUI.enabled = previousEnabled;
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private async Task ExecuteNextPhaseAsync(string reason)
        {
            if (_actionRequested)
            {
                DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation] Action ignored because another action is already running. action='NextPhase' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            IPhaseNextPhaseService service = ResolveRequiredPhaseNextPhaseService("NextPhase");
            if (service == null)
            {
                return;
            }

            _actionRequested = true;

            try
            {
                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Intent] action='NextPhase' reason='{reason}'.",
                    DebugUtility.Colors.Info);

                await service.NextPhaseAsync(reason, CancellationToken.None);

                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='NextPhase' completed reason='{reason}'.",
                    DebugUtility.Colors.Success);

                RefreshView("NextPhase/Completed");
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation] action='NextPhase' failed reason='{reason}' notes='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                _actionRequested = false;
            }
        }

        private void RefreshView(string reason)
        {
            PhaseDefinitionAsset selectedPhase = null;
            GameplayPhaseRuntimeSnapshot runtimeSnapshot = GameplayPhaseRuntimeSnapshot.Empty;

            bool hasSelection = _phaseSelectionService != null && _phaseSelectionService.TryGetCurrent(out selectedPhase);
            bool hasRuntime = _phaseRuntimeService != null && _phaseRuntimeService.TryGetCurrent(out runtimeSnapshot);

            PhaseDefinitionAsset currentPhase = ResolveCurrentPhase(
                hasSelection ? selectedPhase : null,
                hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty);

            _phaseLabel = BuildPhaseLabel(
                currentPhase,
                hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty);
            UpdateButtons(
                currentPhase,
                hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty);

            DebugUtility.Log<PhaseNavigationQaPanel>(
                $"[OBS][QA][PhaseNavigation] PhaseQaViewUpdated phaseId='{DescribePhaseId(currentPhase != null ? currentPhase.PhaseId : default)}' contentId='{DescribeContentId(currentPhase, hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty)}' index='{DescribeCatalogIndex(currentPhase != null ? currentPhase.PhaseId : default)}' hasNextInCatalog='{(_hasNextInCatalog ? "true" : "false")}' canAdvanceNow='{(_canAdvanceNow ? "true" : "false")}' reason='{_canAdvanceReason}'.",
                DebugUtility.Colors.Info);
        }

        private void RegisterBindings()
        {
            if (_registered)
            {
                return;
            }

            EventBus<PhaseDefinitionSelectedEvent>.Register(_phaseSelectedBinding);
            EventBus<PhaseContentAppliedEvent>.Register(_phaseContentAppliedBinding);
            EventBus<IntroStageCompletedEvent>.Register(_introStageCompletedBinding);
            EventBus<GameRunStartedEvent>.Register(_gameRunStartedBinding);
            _registered = true;

            DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                "[OBS][QA][PhaseNavigation] Phase QA bindings registered.",
                DebugUtility.Colors.Info);
        }

        private void UnregisterBindings()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<PhaseDefinitionSelectedEvent>.Unregister(_phaseSelectedBinding);
            EventBus<PhaseContentAppliedEvent>.Unregister(_phaseContentAppliedBinding);
            EventBus<IntroStageCompletedEvent>.Unregister(_introStageCompletedBinding);
            EventBus<GameRunStartedEvent>.Unregister(_gameRunStartedBinding);
            _registered = false;

            DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                "[OBS][QA][PhaseNavigation] Phase QA bindings unregistered.",
                DebugUtility.Colors.Info);
        }

        private bool ShouldShow()
        {
            EnsureDependenciesInjected();

            return _gameLoopService != null &&
                   string.Equals(_gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);
        }

        private void UpdateButtons(PhaseDefinitionAsset selectedPhase, GameplayPhaseRuntimeSnapshot runtimeSnapshot)
        {
            PhaseDefinitionAsset currentPhase = ResolveCurrentPhase(selectedPhase, runtimeSnapshot);

            _hasNextInCatalog = currentPhase != null &&
                                hasActiveCatalog() &&
                                _phaseDefinitionCatalog.TryGetNext(currentPhase.PhaseId.Value, out _);

            bool isPlaying = _gameLoopService != null &&
                             string.Equals(_gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);

            _canAdvanceNow = _hasNextInCatalog &&
                             isPlaying &&
                             !_actionRequested &&
                             _phaseNextPhaseService != null;

            _canAdvanceReason = !_hasNextInCatalog
                ? "no_next_in_catalog"
                : _actionRequested
                    ? "action_in_progress"
                    : !isPlaying
                        ? "waiting_for_playing"
                        : _phaseNextPhaseService == null
                            ? "phase_next_service_missing"
                            : "ready";

            bool hasActiveCatalog()
            {
                return _phaseDefinitionCatalog != null && _phaseDefinitionCatalog.PhaseIds != null;
            }
        }

        private IPhaseNextPhaseService ResolveRequiredPhaseNextPhaseService(string reason)
        {
            EnsureDependenciesInjected();

            if (_phaseNextPhaseService != null)
            {
                return _phaseNextPhaseService;
            }

            HardFailFastH1.Trigger(typeof(PhaseNavigationQaPanel),
                $"[FATAL][H1][QA][PhaseNavigation] IPhaseNextPhaseService indisponivel. reason='{reason}'.");
            return null;
        }

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected)
            {
                return;
            }

            var provider = DependencyManager.Provider;
            if (provider == null)
            {
                return;
            }

            try
            {
                provider.InjectDependencies(this);
                _dependenciesInjected = true;
            }
            catch
            {
                _dependenciesInjected = false;
            }
        }

        private void EnsurePanelBounds()
        {
            float width = Mathf.Max(PanelWidth, 1f);
            float height = Mathf.Max(PanelHeight, 1f);
            float x = Mathf.Max(Screen.width - width - PanelMargin, PanelMargin);
            float y = PanelMargin;

            if (Screen.width > 0 && Screen.height > 0)
            {
                x = Mathf.Clamp(Screen.width - width - PanelMargin, PanelMargin, Mathf.Max(Screen.width - width - PanelMargin, PanelMargin));
                y = Mathf.Clamp(PanelMargin, PanelMargin, Mathf.Max(Screen.height - height - PanelMargin, PanelMargin));
            }

            panelRect = new Rect(x, y, width, height);
        }

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 15,
                    wordWrap = true
                };
            }

            if (_wrappedLabelStyle == null)
            {
                _wrappedLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    fontSize = 12
                };
            }
        }

        private string BuildPhaseLabel(PhaseDefinitionAsset selectedPhase, GameplayPhaseRuntimeSnapshot runtimeSnapshot)
        {
            PhaseDefinitionAsset currentPhase = ResolveCurrentPhase(selectedPhase, runtimeSnapshot);
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Phase");
            builder.AppendLine($"PhaseId: {DescribePhaseId(currentPhase != null ? currentPhase.PhaseId : default)}");
            builder.AppendLine($"ContentId: {DescribeContentId(currentPhase, runtimeSnapshot)}");
            builder.AppendLine($"Index: {DescribeCatalogIndex(currentPhase != null ? currentPhase.PhaseId : default)}");
            builder.AppendLine($"HasNextInCatalog: {(_hasNextInCatalog ? "true" : "false")}");
            builder.AppendLine($"CanAdvanceNow: {(_canAdvanceNow ? "true" : "false")} ({_canAdvanceReason})");
            return builder.ToString().TrimEnd();
        }

        private static string DescribePhaseId(PhaseDefinitionId phaseId)
        {
            return phaseId.IsValid ? phaseId.Value : "<none>";
        }

        private string DescribeCatalogIndex(PhaseDefinitionId phaseId)
        {
            if (_phaseDefinitionCatalog == null)
            {
                return "0/0";
            }

            IReadOnlyList<string> phaseIds = _phaseDefinitionCatalog.PhaseIds;
            int total = phaseIds?.Count ?? 0;
            if (!phaseId.IsValid || total <= 0)
            {
                return $"0/{total}";
            }

            for (int i = 0; i < phaseIds.Count; i++)
            {
                if (string.Equals(phaseIds[i], phaseId.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{i + 1}/{total}";
                }
            }

            return $"0/{total}";
        }

        private string DescribeContentId(PhaseDefinitionAsset selectedPhase, GameplayPhaseRuntimeSnapshot runtimeSnapshot)
        {
            PhaseDefinitionAsset phase = runtimeSnapshot.IsValid && runtimeSnapshot.PhaseDefinitionRef != null
                ? runtimeSnapshot.PhaseDefinitionRef
                : selectedPhase;

            return phase != null ? phase.BuildCanonicalIntroContentId() : "<none>";
        }

        private static PhaseDefinitionAsset ResolveCurrentPhase(PhaseDefinitionAsset selectedPhase, GameplayPhaseRuntimeSnapshot runtimeSnapshot)
        {
            if (runtimeSnapshot.IsValid && runtimeSnapshot.PhaseDefinitionRef != null)
            {
                return runtimeSnapshot.PhaseDefinitionRef;
            }

            return selectedPhase;
        }
    }
}
