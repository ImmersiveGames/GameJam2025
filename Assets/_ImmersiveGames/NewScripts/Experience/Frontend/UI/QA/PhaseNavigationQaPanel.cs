using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
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
        private const string GoToSpecificPhaseReason = "QA/PhaseNavigation/GoToSpecificPhase";
        private const string RestartCatalogReason = "QA/PhaseNavigation/RestartCatalog";
        private const float PanelWidth = 960f;
        private const float PanelHeight = 556f;
        private const float PanelMargin = 16f;
        private const float ContentHeight = 188f;
        private const float ButtonHeight = 32f;
        private const float ButtonSpacing = 6f;
        private const float SectionSpacing = 8f;

        [Header("Layout")]
        [SerializeField] private Rect panelRect = new(0f, 0f, PanelWidth, PanelHeight);
        [SerializeField] private string title = "Phase Navigation QA";

        [Inject] private IGameplayPhaseRuntimeService _phaseRuntimeService;
        [Inject] private IPhaseDefinitionSelectionService _phaseSelectionService;
        [Inject] private IPhaseCatalogRuntimeStateService _phaseCatalogRuntimeStateService;
        [Inject] private IPhaseNextPhaseService _phaseNavigationService;
        [Inject] private IPhaseDefinitionCatalog _phaseDefinitionCatalog;
        [Inject] private IGameLoopService _gameLoopService;

        private EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private EventBinding<PhaseContentAppliedEvent> _phaseContentAppliedBinding;
        private EventBinding<IntroStageCompletedEvent> _introStageCompletedBinding;
        private EventBinding<GameRunStartedEvent> _gameRunStartedBinding;
        private bool _dependenciesInjected;
        private bool _registered;
        private bool _isExecutingRequest;
        private bool _hasNextInCatalog;
        private bool _hasPreviousInCatalog;
        private bool _isOperationallyReadyForQa;
        private bool _buttonClickableNext;
        private bool _buttonClickablePrevious;
        private string _operationalStateReason = string.Empty;
        private string _phaseLabel = string.Empty;
        private string _specificPhaseId = string.Empty;
        private Vector2 _scrollPosition = Vector2.zero;
        private GUIStyle _wrappedLabelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;

        private void Awake()
        {
            EnsureDependenciesInjected();

            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(_ => RefreshView("PhaseDefinitionSelectedEvent"));
            _phaseContentAppliedBinding = new EventBinding<PhaseContentAppliedEvent>(_ => RefreshView("PhaseContentAppliedEvent"));
            _introStageCompletedBinding = new EventBinding<IntroStageCompletedEvent>(_ => RefreshView("IntroStageCompletedEvent"));
            _gameRunStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
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
            _isExecutingRequest = false;
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
            GUILayout.Label(BuildCatalogSummaryLine(), _wrappedLabelStyle);
            GUILayout.Space(4f);

            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.Height(ContentHeight));

            GUILayout.Label(_phaseLabel, _wrappedLabelStyle, GUILayout.ExpandHeight(true));

            GUILayout.EndScrollView();

            GUILayout.Space(SectionSpacing);

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && (_buttonClickableNext || _buttonClickablePrevious);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev", _buttonStyle, GUILayout.Height(42f)))
            {
                _ = ExecutePhaseNavigationAsync(PhaseNavigationDirection.Previous, "QA/PhaseNavigation/PreviousPhase");
            }

            if (GUILayout.Button("Next", _buttonStyle, GUILayout.Height(42f)))
            {
                _ = ExecuteCanonicalNextPhaseAsync(NextPhaseReason);
            }

            if (GUILayout.Button("Restart Cat", _buttonStyle, GUILayout.Height(42f)))
            {
                _ = ExecuteRestartCatalogAsync(RestartCatalogReason);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(ButtonSpacing);

            GUILayout.BeginHorizontal();
            _specificPhaseId = GUILayout.TextField(_specificPhaseId ?? string.Empty, GUILayout.ExpandWidth(true));
            bool specificButtonEnabled = previousEnabled &&
                                         _isOperationallyReadyForQa &&
                                         !string.IsNullOrWhiteSpace(_specificPhaseId);
            GUI.enabled = specificButtonEnabled;
            if (GUILayout.Button("Specific", _buttonStyle, GUILayout.Height(42f), GUILayout.Width(88f)))
            {
                _ = ExecuteGoToSpecificPhaseAsync(_specificPhaseId, GoToSpecificPhaseReason);
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();

            GUILayout.Space(ButtonSpacing);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private async Task ExecuteCanonicalNextPhaseAsync(string reason)
        {
            if (_isExecutingRequest)
            {
                DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='NextPhaseCanonical' guard_ignored='true' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!_isOperationallyReadyForQa)
            {
                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='NextPhaseCanonical' outcome='RejectedNotReady' reason='{reason}'.");
                RefreshView("NextPhaseCanonical/RejectedNotReady");
                return;
            }

            if (_phaseNavigationService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNavigationQaPanel),
                    $"[FATAL][H1][QA][PhaseNavigation] IPhaseNextPhaseService indisponivel. reason='{reason}'.");
                return;
            }

            _isExecutingRequest = true;

            try
            {
                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='NextPhaseCanonical' stage='PhaseNavigationRequested' currentPhase='{DescribePhaseId(GetCurrentPhase() != null ? GetCurrentPhase().PhaseId : default)}' reason='{reason}'.",
                    DebugUtility.Colors.Info);

                PhaseNavigationResult result = await _phaseNavigationService.NextPhaseAsync(reason, CancellationToken.None);

                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='NextPhaseCanonical' outcome='{result.Outcome}' reason='{reason}'.",
                    result.IsBlockedAtBoundary ? DebugUtility.Colors.Warning : DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation] action='NextPhaseCanonical' failed reason='{reason}' notes='{ex.GetType().Name}: {ex.Message}'.");

                RefreshView("NextPhaseCanonical/Failed");
            }
            finally
            {
                _isExecutingRequest = false;
                RefreshView("NextPhaseCanonical/Finished");
            }
        }

        private async Task ExecutePhaseNavigationAsync(PhaseNavigationDirection direction, string reason)
        {
            if (_isExecutingRequest)
            {
                DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='{DescribeDirection(direction)}' guard_ignored='true' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            PhaseDefinitionAsset currentPhase = GetCurrentPhase();
            PhaseNavigationRequest request = direction == PhaseNavigationDirection.Previous
                ? PhaseNavigationRequest.Previous(reason)
                : PhaseNavigationRequest.Next(reason);

            if (_phaseNavigationService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNavigationQaPanel),
                    $"[FATAL][H1][QA][PhaseNavigation] IPhaseNextPhaseService indisponivel. reason='{reason}'.");
                return;
            }

            if (!_isOperationallyReadyForQa)
            {
                PhaseNavigationRequest rejectedRequest = direction == PhaseNavigationDirection.Previous
                    ? PhaseNavigationRequest.Previous(reason)
                    : PhaseNavigationRequest.Next(reason);
                PhaseNavigationResult rejectedResult = new PhaseNavigationResult(
                    rejectedRequest,
                    PhaseNavigationOutcome.RejectedNotReady,
                    currentPhase,
                    DescribeCatalog(),
                    PhaseCatalogTraversalMode.Finite,
                    false,
                    default);

                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='{DescribeDirection(direction)}' outcome='{rejectedResult.Outcome}' reason='{rejectedResult.Reason}'.");
                RefreshView($"{DescribeDirection(direction)}/RejectedNotReady");
                return;
            }

            _isExecutingRequest = true;

            try
            {
                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='{DescribeDirection(direction)}' started currentPhase='{DescribePhaseId(currentPhase != null ? currentPhase.PhaseId : default)}' reason='{request.Reason}'.",
                    DebugUtility.Colors.Info);

                PhaseNavigationResult result = direction == PhaseNavigationDirection.Previous
                    ? await _phaseNavigationService.PreviousPhaseAsync(request.Reason, CancellationToken.None)
                    : await _phaseNavigationService.NextPhaseAsync(request.Reason, CancellationToken.None);

                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='{DescribeDirection(direction)}' outcome='{result.Outcome}' reason='{request.Reason}'.",
                    result.IsBlockedAtBoundary ? DebugUtility.Colors.Warning : DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation] action='{DescribeDirection(direction)}' failed reason='{reason}' notes='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                _isExecutingRequest = false;
                RefreshView($"{DescribeDirection(direction)}/Finished");
            }
        }

        private async Task ExecuteGoToSpecificPhaseAsync(string rawInput, string reason)
        {
            if (_isExecutingRequest)
            {
                DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='GoToSpecificPhase' guard_ignored='true' input='{rawInput}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (_phaseNavigationService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNavigationQaPanel),
                    $"[FATAL][H1][QA][PhaseNavigation] IPhaseNextPhaseService indisponivel. reason='{reason}'.");
                return;
            }

            if (!_isOperationallyReadyForQa)
            {
                PhaseNavigationResult rejectedResult = new PhaseNavigationResult(
                    PhaseNavigationRequest.Specific(rawInput, reason),
                    PhaseNavigationOutcome.RejectedNotReady,
                    GetCurrentPhase(),
                    DescribeCatalog(),
                    PhaseCatalogTraversalMode.Finite,
                    false,
                    default);

                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='GoToSpecificPhase' outcome='{rejectedResult.Outcome}' input='{rawInput}' reason='{rejectedResult.Reason}'.");
                RefreshView("GoToSpecificPhase/RejectedNotReady");
                return;
            }

            if (!TryResolveSpecificTarget(rawInput, out string resolvedPhaseId, out string resolutionKind, out string rejectionReason))
            {
                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='GoToSpecificPhase' rejected input='{rawInput}' reason='{rejectionReason}'.");
                RefreshView("GoToSpecificPhase/RejectedInvalidInput");
                return;
            }

            _isExecutingRequest = true;

            try
            {
                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='GoToSpecificPhase' started input='{rawInput}' interpretation='{resolutionKind}' targetPhaseId='{resolvedPhaseId}' reason='{reason}'.",
                    DebugUtility.Colors.Info);

                PhaseNavigationResult result = await _phaseNavigationService.GoToSpecificPhaseAsync(resolvedPhaseId, reason, CancellationToken.None);

                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='GoToSpecificPhase' outcome='{result.Outcome}' input='{rawInput}' interpretation='{resolutionKind}' targetPhaseId='{resolvedPhaseId}' reason='{reason}'.",
                    result.Outcome == PhaseNavigationOutcome.Changed ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation] action='GoToSpecificPhase' failed input='{rawInput}' reason='{reason}' notes='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                _isExecutingRequest = false;
                RefreshView("GoToSpecificPhase/Finished");
            }
        }

        private async Task ExecuteRestartCatalogAsync(string reason)
        {
            if (_isExecutingRequest)
            {
                DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='RestartCatalog' guard_ignored='true' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (_phaseNavigationService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNavigationQaPanel),
                    $"[FATAL][H1][QA][PhaseNavigation] IPhaseNextPhaseService indisponivel. reason='{reason}'.");
                return;
            }

            if (!_isOperationallyReadyForQa)
            {
                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='RestartCatalog' outcome='RejectedNotReady' reason='{reason}'.");
                RefreshView("RestartCatalog/RejectedNotReady");
                return;
            }

            _isExecutingRequest = true;

            try
            {
                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='RestartCatalog' started currentPhase='{DescribePhaseId(GetCurrentPhase() != null ? GetCurrentPhase().PhaseId : default)}' reason='{reason}'.",
                    DebugUtility.Colors.Info);

                PhaseNavigationResult result = await _phaseNavigationService.RestartCatalogAsync(reason, CancellationToken.None);

                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='RestartCatalog' outcome='{result.Outcome}' reason='{reason}'.",
                    result.Outcome == PhaseNavigationOutcome.Changed ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation] action='RestartCatalog' failed reason='{reason}' notes='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                _isExecutingRequest = false;
                RefreshView("RestartCatalog/Finished");
            }
        }

        private bool TryResolveSpecificTarget(string rawInput, out string resolvedPhaseId, out string resolutionKind, out string rejectionReason)
        {
            resolvedPhaseId = string.Empty;
            resolutionKind = string.Empty;
            rejectionReason = string.Empty;

            string normalizedInput = string.IsNullOrWhiteSpace(rawInput) ? string.Empty : rawInput.Trim();
            if (string.IsNullOrWhiteSpace(normalizedInput))
            {
                rejectionReason = "empty_input";
                return false;
            }

            if (int.TryParse(normalizedInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out int catalogIndex))
            {
                int total = _phaseDefinitionCatalog?.PhaseIds?.Count ?? 0;
                if (catalogIndex <= 0)
                {
                    rejectionReason = $"index_out_of_range index={catalogIndex} total={total} range='1..{total}'";
                    DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                        $"[OBS][QA][PhaseNavigation][Parse] input='{normalizedInput}' interpretation='index' outcome='Rejected' reason='index_must_be_positive' total='{total}'.");
                    return false;
                }

                if (_phaseDefinitionCatalog == null || _phaseDefinitionCatalog.PhaseIds == null)
                {
                    rejectionReason = "catalog_unavailable";
                    return false;
                }

                if (catalogIndex > total)
                {
                    rejectionReason = $"index_out_of_range index={catalogIndex} total={total} range='1..{total}'";
                    DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                        $"[OBS][QA][PhaseNavigation][Parse] input='{normalizedInput}' interpretation='index' outcome='Rejected' reason='index_out_of_range' total='{total}' index='{catalogIndex}'.");
                    return false;
                }

                resolvedPhaseId = _phaseDefinitionCatalog.PhaseIds[catalogIndex - 1];
                resolutionKind = $"index_1_based={catalogIndex}";

                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Parse] input='{normalizedInput}' interpretation='index' index='{catalogIndex}' resolvedPhaseId='{resolvedPhaseId}' total='{total}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            resolvedPhaseId = normalizedInput;
            resolutionKind = "phaseId";

            DebugUtility.Log<PhaseNavigationQaPanel>(
                $"[OBS][QA][PhaseNavigation][Parse] input='{normalizedInput}' interpretation='phaseId' resolvedPhaseId='{resolvedPhaseId}'.",
                DebugUtility.Colors.Info);
            return true;
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            _isExecutingRequest = false;
            RefreshView("GameRunStartedEvent");
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
            UpdateInteractionState(
                currentPhase,
                hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty);

            DebugUtility.Log<PhaseNavigationQaPanel>(
                $"[OBS][QA][PhaseNavigation] PhaseQaViewUpdated phaseId='{DescribePhaseId(currentPhase != null ? currentPhase.PhaseId : default)}' contentId='{DescribeContentId(currentPhase, hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty)}' index='{DescribeCatalogIndex(currentPhase != null ? currentPhase.PhaseId : default)}' hasNextInCatalog='{(_hasNextInCatalog ? "true" : "false")}' hasPreviousInCatalog='{(_hasPreviousInCatalog ? "true" : "false")}' isExecuting='{(_isExecutingRequest ? "true" : "false")}' buttonClickableNext='{(_buttonClickableNext ? "true" : "false")}' buttonClickablePrevious='{(_buttonClickablePrevious ? "true" : "false")}' isOperationallyReadyForQa='{(_isOperationallyReadyForQa ? "true" : "false")}' reason='{_operationalStateReason}'.",
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

        private void UpdateInteractionState(PhaseDefinitionAsset selectedPhase, GameplayPhaseRuntimeSnapshot runtimeSnapshot)
        {
            PhaseDefinitionAsset currentPhase = ResolveCurrentPhase(selectedPhase, runtimeSnapshot);

            _hasNextInCatalog = currentPhase != null &&
                                hasActiveCatalog() &&
                                _phaseDefinitionCatalog.TryGetNext(currentPhase.PhaseId.Value, out _);
            _hasPreviousInCatalog = currentPhase != null &&
                                    hasActiveCatalog() &&
                                    _phaseDefinitionCatalog.TryGetPrevious(currentPhase.PhaseId.Value, out _);

            bool isPlaying = _gameLoopService != null &&
                             string.Equals(_gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);

            _isOperationallyReadyForQa = currentPhase != null &&
                                          isPlaying &&
                                          _phaseNavigationService != null;

            _buttonClickableNext = _isOperationallyReadyForQa;
            _buttonClickablePrevious = _isOperationallyReadyForQa;

            _operationalStateReason = currentPhase == null
                ? "no_current_phase"
                : !isPlaying
                        ? "waiting_for_playing"
                        : _phaseNavigationService == null
                            ? "phase_navigation_service_missing"
                            : _isExecutingRequest
                                ? "executing_request"
                                : "ready";

            bool hasActiveCatalog()
            {
                return _phaseDefinitionCatalog != null && _phaseDefinitionCatalog.PhaseIds != null;
            }
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
                    fontSize = 27,
                    wordWrap = true
                };
            }

            if (_wrappedLabelStyle == null)
            {
                _wrappedLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    fontSize = 21
                };
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 21,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true
                };
            }
        }

        private string BuildPhaseLabel(PhaseDefinitionAsset selectedPhase, GameplayPhaseRuntimeSnapshot runtimeSnapshot)
        {
            PhaseDefinitionAsset currentPhase = ResolveCurrentPhase(selectedPhase, runtimeSnapshot);
            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"Id: {DescribePhaseId(currentPhase != null ? currentPhase.PhaseId : default)}");
            builder.AppendLine($"Content: {DescribeContentId(currentPhase, runtimeSnapshot)}");
            builder.AppendLine($"Index: {DescribeCatalogIndex(currentPhase != null ? currentPhase.PhaseId : default)}");
            builder.AppendLine($"Loop: {DescribeLoopCount()}");
            builder.AppendLine($"Next/Prev: {(_hasNextInCatalog ? "Y" : "N")}/{(_hasPreviousInCatalog ? "Y" : "N")}");
            builder.AppendLine($"Ready: {(_isOperationallyReadyForQa ? "Y" : "N")} ({_operationalStateReason})");
            builder.AppendLine("Specific: phaseId | index 1-based");
            builder.AppendLine($"Input: {(string.IsNullOrWhiteSpace(_specificPhaseId) ? "<none>" : _specificPhaseId.Trim())}");
            return builder.ToString().TrimEnd();
        }

        private string BuildCatalogSummaryLine()
        {
            PhaseDefinitionAsset currentPhase = GetCurrentPhase();
            string currentPhaseId = DescribePhaseId(currentPhase != null ? currentPhase.PhaseId : default);
            string currentIndex = DescribeCatalogIndex(currentPhase != null ? currentPhase.PhaseId : default);
            string loopCount = DescribeLoopCount();
            string catalogName = DescribeCatalog();

            return $"Catalog: {catalogName} | Phase: {currentPhaseId} | Index: {currentIndex} | Loop: {loopCount}";
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

        private PhaseDefinitionAsset GetCurrentPhase()
        {
            PhaseDefinitionAsset selectedPhase = null;
            GameplayPhaseRuntimeSnapshot runtimeSnapshot = GameplayPhaseRuntimeSnapshot.Empty;

            bool hasSelection = _phaseSelectionService != null && _phaseSelectionService.TryGetCurrent(out selectedPhase);
            bool hasRuntime = _phaseRuntimeService != null && _phaseRuntimeService.TryGetCurrent(out runtimeSnapshot);

            return ResolveCurrentPhase(
                hasSelection ? selectedPhase : null,
                hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty);
        }

        private static string DescribeDirection(PhaseNavigationDirection direction)
        {
            return direction == PhaseNavigationDirection.Previous ? "Previous" : "Next";
        }

        private string DescribeCatalog()
        {
            if (_phaseDefinitionCatalog == null)
            {
                return "<none>";
            }

            return _phaseDefinitionCatalog is UnityEngine.Object unityObject
                ? unityObject.name
                : _phaseDefinitionCatalog.GetType().Name;
        }

        private string DescribeLoopCount()
        {
            if (_phaseCatalogRuntimeStateService == null)
            {
                return "<none>";
            }

            return _phaseCatalogRuntimeStateService.LoopCount.ToString(CultureInfo.InvariantCulture);
        }

        private string BuildCatalogPhaseMap()
        {
            if (_phaseDefinitionCatalog == null || _phaseDefinitionCatalog.PhaseIds == null || _phaseDefinitionCatalog.PhaseIds.Count == 0)
            {
                return "<none>";
            }

            List<string> entries = new List<string>(_phaseDefinitionCatalog.PhaseIds.Count);
            for (int i = 0; i < _phaseDefinitionCatalog.PhaseIds.Count; i++)
            {
                string phaseId = _phaseDefinitionCatalog.PhaseIds[i];
                entries.Add($"{i + 1}->{phaseId}");
            }

            return string.Join(" | ", entries);
        }
    }
}
