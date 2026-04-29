using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.Core;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.FrontendRuntime.UI.QA
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
        [Inject] private IPhaseCatalogNavigationService _phaseCatalogNavigationService;
        [Inject] private IRunContinuationSelectionRoutingService _runContinuationSelectionRoutingService;
        [Inject] private IPhaseDefinitionCatalog _phaseDefinitionCatalog;
        [Inject] private IGameplayStateGate _gameplayStateGate;
        [Inject] private IGameplayInteractionReadinessService _interactionReadinessService;
        [Inject] private IGameplayParticipationFlowService _participationFlowService;

        private EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private EventBinding<PhaseContentAppliedEvent> _phaseContentAppliedBinding;
        private EventBinding<IntroStageCompletedEvent> _introStageCompletedBinding;
        private EventBinding<GameRunStartedEvent> _gameRunStartedBinding;
        private Action<GameplayInteractionReadinessSnapshot> _interactionReadinessChangedHandler;
        private bool _dependenciesInjected;
        private bool _registered;
        private bool _interactionReadinessSubscribed;
        private bool _isExecutingRequest;
        private bool _panelVisible;
        private bool _catalogCapabilityNext;
        private bool _catalogCapabilityPrevious;
        private bool _executionAllowedNow;
        private string _navigationCapabilitySource = string.Empty;
        private string _traversalMode = string.Empty;
        private string _resolvedNextPhaseId = string.Empty;
        private string _resolvedPreviousPhaseId = string.Empty;
        private string _executionBlockedReason = string.Empty;
        private string _lastCommandResult = "<none>";
        private string _lastCommandRejectedReason = "<none>";
        private string _operationalStateReason = string.Empty;
        private string _phaseLabel = string.Empty;
        private string _participationLabel = string.Empty;
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
            _interactionReadinessChangedHandler = _ => RefreshView("GameplayInteractionReadyChanged");
            RegisterBindings();
            RegisterInteractionReadinessSubscription();
            RefreshView("Awake");
        }

        private void OnEnable()
        {
            RegisterBindings();
            RegisterInteractionReadinessSubscription();
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
            UnregisterInteractionReadinessSubscription();
            _isExecutingRequest = false;
        }

        private void OnDestroy()
        {
            UnregisterBindings();
            UnregisterInteractionReadinessSubscription();
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
            GUILayout.Space(6f);
            GUILayout.Label(_participationLabel, _wrappedLabelStyle, GUILayout.ExpandHeight(false));

            GUILayout.EndScrollView();

            GUILayout.Space(SectionSpacing);

            bool previousEnabled = GUI.enabled;
            string previousLabel = BuildReadOnlyNavigationButtonLabel("Prev", _resolvedPreviousPhaseId, _catalogCapabilityPrevious);
            string nextLabel = BuildNavigationButtonLabel("Next", _catalogCapabilityNext);

            GUILayout.BeginHorizontal();
            GUI.enabled = false;
            GUILayout.Button(previousLabel, _buttonStyle, GUILayout.Height(42f));

            GUI.enabled = previousEnabled && _catalogCapabilityNext && !_isExecutingRequest;
            if (GUILayout.Button(nextLabel, _buttonStyle, GUILayout.Height(42f)))
            {
                _ = ExecuteCanonicalNextPhaseAsync(NextPhaseReason);
            }

            GUI.enabled = false;
            GUILayout.Button("Restart Cat (disabled)", _buttonStyle, GUILayout.Height(42f));

            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();

            GUILayout.Space(ButtonSpacing);

            GUILayout.BeginHorizontal();
            _specificPhaseId = GUILayout.TextField(_specificPhaseId ?? string.Empty, GUILayout.ExpandWidth(true));
            bool specificButtonEnabled = previousEnabled &&
                                         !string.IsNullOrWhiteSpace(_specificPhaseId);
            GUI.enabled = specificButtonEnabled;
            if (GUILayout.Button("Resolve", _buttonStyle, GUILayout.Height(42f), GUILayout.Width(88f)))
            {
                ResolveSpecificPhaseReadOnly(_specificPhaseId);
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();

            GUILayout.Space(ButtonSpacing);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private Task ExecuteCanonicalNextPhaseAsync(string reason)
        {
            if (_isExecutingRequest)
            {
                DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='NextPhaseCanonical' guard_ignored='true' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return Task.CompletedTask;
            }

            if (_runContinuationSelectionRoutingService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNavigationQaPanel),
                    $"[FATAL][H1][QA][PhaseNavigation] IRunContinuationSelectionRoutingService indisponivel para QA NextPhase canonico. reason='{reason}'.");
                return Task.CompletedTask;
            }

            PhaseDefinitionAsset currentPhase = GetCurrentPhase();
            if (currentPhase == null || !currentPhase.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNavigationQaPanel),
                    $"[FATAL][H1][QA][PhaseNavigation] NextPhaseCanonical sem current phase valida. reason='{reason}'.");
                return Task.CompletedTask;
            }

            _isExecutingRequest = true;

            try
            {
                RunContinuationSelection selection = BuildQaAdvancePhaseSelection(currentPhase, reason);

                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation][Execute] action='NextPhaseCanonical' stage='RunContinuationSelectionRoutingRequested' continuation='{selection.SelectedContinuation}' currentPhase='{DescribePhaseId(currentPhase.PhaseId)}' target='{_resolvedNextPhaseId}' reason='{reason}' source='RunContinuationSelectionRoutingService'.",
                    DebugUtility.Colors.Info);

                _runContinuationSelectionRoutingService.RouteSelection(selection);

                _lastCommandResult = "Dispatched";
                _lastCommandRejectedReason = "<none>";

                DebugUtility.Log<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation] QaNavigationCommandAccepted action='NextPhaseCanonical' target='{_resolvedNextPhaseId}' outcome='Dispatched' source='RunContinuationSelectionRoutingService' continuation='AdvancePhase' reason='{reason}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                RecordAndLogCommandFailure("NextPhaseCanonical", _resolvedNextPhaseId, reason, ex);
                RefreshView("NextPhaseCanonical/Failed");
            }
            finally
            {
                _isExecutingRequest = false;
                RefreshView("NextPhaseCanonical/Finished");
            }

            return Task.CompletedTask;
        }

        private RunContinuationSelection BuildQaAdvancePhaseSelection(PhaseDefinitionAsset currentPhase, string reason)
        {
            string normalizedReason = NormalizeReason(reason);
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                HardFailFastH1.Trigger(typeof(PhaseNavigationQaPanel),
                    $"[FATAL][H1][QA][PhaseNavigation] NextPhaseCanonical nao conseguiu resolver active scene para contexto canonico. reason='{normalizedReason}'.");
            }

            string phaseId = currentPhase != null && currentPhase.PhaseId.IsValid
                ? currentPhase.PhaseId.Value
                : "<none>";
            string signature = BuildQaAdvancePhaseSignature(phaseId, normalizedReason);
            string nextState = string.IsNullOrWhiteSpace(_resolvedNextPhaseId) || string.Equals(_resolvedNextPhaseId, "<none>", StringComparison.Ordinal)
                ? "AdvancePhase"
                : _resolvedNextPhaseId.Trim();

            var intent = new RunEndIntent(
                signature,
                sceneName,
                "gameplay",
                Time.frameCount,
                normalizedReason,
                isGameplayScene: true);

            var context = new RunContinuationContext(
                intent,
                RunResult.Victory,
                new[] { RunContinuationKind.AdvancePhase },
                requiresPlayerDecision: false);

            var completion = new RunDecisionCompletion(
                RunDecisionCompletionKind.Macro,
                normalizedReason,
                nextState);

            var selection = new RunContinuationSelection(
                context,
                RunContinuationKind.AdvancePhase,
                completion);

            if (!selection.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNavigationQaPanel),
                    $"[FATAL][H1][QA][PhaseNavigation] RunContinuationSelection QA invalida para AdvancePhase. phaseId='{phaseId}' scene='{sceneName}' target='{nextState}' reason='{normalizedReason}'.");
            }

            return selection;
        }

        private static string BuildQaAdvancePhaseSignature(string phaseId, string reason)
        {
            string normalizedPhaseId = string.IsNullOrWhiteSpace(phaseId) ? "none" : phaseId.Trim();
            string normalizedReason = NormalizeReason(reason);
            return $"qa.advance-phase|phase:{normalizedPhaseId}|frame:{Time.frameCount}|reason:{normalizedReason}";
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "QA/PhaseNavigation/NextPhase" : reason.Trim();
        }
        private void ResolveSpecificPhaseReadOnly(string rawInput)
        {
            if (!TryResolveSpecificTarget(rawInput, out string resolvedPhaseId, out string resolutionKind, out string rejectionReason))
            {
                _lastCommandResult = "ReadOnlyRejectedInvalidInput";
                _lastCommandRejectedReason = rejectionReason;
                DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                    $"[OBS][QA][PhaseNavigation] QaNavigationCommandRejected action='ResolveSpecificPhaseReadOnly' target='{rawInput}' reason='{rejectionReason}' source='PhaseCatalog'.");
                RefreshView("ResolveSpecificPhaseReadOnly/RejectedInvalidInput");
                return;
            }

            _lastCommandResult = "ReadOnlyResolved";
            _lastCommandRejectedReason = "<none>";
            DebugUtility.Log<PhaseNavigationQaPanel>(
                $"[OBS][QA][PhaseNavigation] QaNavigationReadOnlyResolved action='ResolveSpecificPhaseReadOnly' input='{rawInput}' interpretation='{resolutionKind}' targetPhaseId='{resolvedPhaseId}' source='PhaseCatalog' operationalDispatch='false'.",
                DebugUtility.Colors.Info);
            RefreshView("ResolveSpecificPhaseReadOnly/Resolved");
        }

        private void RecordAndLogCommandFailure(string action, string target, string reason, Exception ex)
        {
            _lastCommandResult = "Failed";
            _lastCommandRejectedReason = $"{ex.GetType().Name}: {ex.Message}";
            DebugUtility.LogWarning<PhaseNavigationQaPanel>(
                $"[OBS][QA][PhaseNavigation] QaNavigationCommandRejected action='{action}' target='{target}' outcome='Failed' reason='{_lastCommandRejectedReason}' source='PhaseCatalog' requestReason='{reason}'.");
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
            ParticipationSnapshot participationSnapshot = ParticipationSnapshot.Empty;

            bool hasSelection = _phaseSelectionService != null && _phaseSelectionService.TryGetCurrent(out selectedPhase);
            bool hasRuntime = _phaseRuntimeService != null && _phaseRuntimeService.TryGetCurrent(out runtimeSnapshot);
            bool hasParticipation = _participationFlowService != null && _participationFlowService.TryGetCurrent(out participationSnapshot);

            PhaseDefinitionAsset currentPhase = ResolveCurrentPhase(
                hasSelection ? selectedPhase : null,
                hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty);

            _phaseLabel = BuildPhaseLabel(
                currentPhase,
                hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty,
                hasParticipation ? participationSnapshot : ParticipationSnapshot.Empty);
            _participationLabel = BuildParticipationLabel(hasParticipation ? participationSnapshot : ParticipationSnapshot.Empty);
            UpdateInteractionState(
                currentPhase,
                hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty);

            DebugUtility.Log<PhaseNavigationQaPanel>(
                $"[OBS][QA][PhaseNavigation] PhaseQaViewUpdated phaseId='{DescribePhaseId(currentPhase != null ? currentPhase.PhaseId : default)}' contentId='{DescribeContentId(currentPhase, hasRuntime ? runtimeSnapshot : GameplayPhaseRuntimeSnapshot.Empty)}' index='{DescribeCatalogIndex(currentPhase != null ? currentPhase.PhaseId : default)}' participationSignature='{DescribeParticipationSignature(hasParticipation ? participationSnapshot : ParticipationSnapshot.Empty)}' participationReadiness='{DescribeParticipationReadiness(hasParticipation ? participationSnapshot : ParticipationSnapshot.Empty)}' panelVisible='{(_panelVisible ? "true" : "false")}' navigationCapabilitySource='{_navigationCapabilitySource}' traversalMode='{_traversalMode}' resolvedNext='{_resolvedNextPhaseId}' resolvedPrevious='{_resolvedPreviousPhaseId}' catalogCapabilityNext='{(_catalogCapabilityNext ? "true" : "false")}' catalogCapabilityPrevious='{(_catalogCapabilityPrevious ? "true" : "false")}' buttonClickableNext='{(_catalogCapabilityNext && !_isExecutingRequest ? "true" : "false")}' buttonClickablePrevious='false' executionAllowedNow='{(_executionAllowedNow ? "true" : "false")}' executionBlockedReason='{_executionBlockedReason}' lastCommandResult='{_lastCommandResult}' lastCommandRejectedReason='{_lastCommandRejectedReason}' isExecuting='{(_isExecutingRequest ? "true" : "false")}' isOperationallyReadyForQa='{(_executionAllowedNow ? "true" : "false")}' reason='{_operationalStateReason}'.",
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

        private void RegisterInteractionReadinessSubscription()
        {
            if (_interactionReadinessService == null || _interactionReadinessChangedHandler == null || _interactionReadinessSubscribed)
            {
                return;
            }

            _interactionReadinessService.Changed += _interactionReadinessChangedHandler;
            _interactionReadinessSubscribed = true;
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

        private void UnregisterInteractionReadinessSubscription()
        {
            if (_interactionReadinessService == null || _interactionReadinessChangedHandler == null || !_interactionReadinessSubscribed)
            {
                return;
            }

            _interactionReadinessService.Changed -= _interactionReadinessChangedHandler;
            _interactionReadinessSubscribed = false;
        }

        private bool ShouldShow()
        {
            EnsureDependenciesInjected();
            _panelVisible = ResolvePanelVisible();
            return _panelVisible;
        }

        private bool ResolvePanelVisible()
        {
            return _phaseCatalogNavigationService != null ||
                   _phaseCatalogRuntimeStateService != null ||
                   _phaseSelectionService != null ||
                   _phaseRuntimeService != null ||
                   _phaseDefinitionCatalog != null;
        }

        private void UpdateInteractionState(PhaseDefinitionAsset selectedPhase, GameplayPhaseRuntimeSnapshot runtimeSnapshot)
        {
            PhaseDefinitionAsset currentPhase = ResolveCurrentPhase(selectedPhase, runtimeSnapshot);
            PhaseCatalogNavigationPlan nextPlan = default;
            PhaseCatalogNavigationPlan previousPlan = default;
            bool hasCatalogNavigationService = _phaseCatalogNavigationService != null;
            bool canResolveFromCatalog = hasCatalogNavigationService &&
                                        currentPhase != null &&
                                        currentPhase.PhaseId.IsValid &&
                                        _phaseCatalogNavigationService.CurrentCommitted != null &&
                                        _phaseCatalogNavigationService.CurrentCommitted.PhaseId.IsValid &&
                                        string.Equals(_phaseCatalogNavigationService.CurrentCommitted.PhaseId.Value, currentPhase.PhaseId.Value, StringComparison.OrdinalIgnoreCase);

            if (canResolveFromCatalog)
            {
                nextPlan = _phaseCatalogNavigationService.ResolveNext(NextPhaseReason);
                previousPlan = _phaseCatalogNavigationService.ResolvePrevious(NextPhaseReason);
            }

            _navigationCapabilitySource = hasCatalogNavigationService ? "PhaseCatalog" : "catalog_navigation_service_missing";
            _traversalMode = hasCatalogNavigationService
                ? _phaseCatalogNavigationService.TraversalMode.ToString()
                : "<none>";
            _resolvedNextPhaseId = nextPlan.HasTarget ? DescribePhaseId(nextPlan.TargetPhaseRef.PhaseId) : "<none>";
            _resolvedPreviousPhaseId = previousPlan.HasTarget ? DescribePhaseId(previousPlan.TargetPhaseRef.PhaseId) : "<none>";
            _panelVisible = ResolvePanelVisible();
            _catalogCapabilityNext = canResolveFromCatalog && nextPlan.HasTarget;
            _catalogCapabilityPrevious = canResolveFromCatalog && previousPlan.HasTarget;

            bool isGameplayActive = _gameplayStateGate != null &&
                                    _gameplayStateGate.IsGameActive();

            if (_gameplayStateGate == null)
            {
                DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                    "[DIAGNOSTIC][PhaseNavigation] _gameplayStateGate is null; treating gameplay as inactive.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.LogVerbose<PhaseNavigationQaPanel>(
                    $"[DIAGNOSTIC][PhaseNavigation] gameplayStateGate.IsGameActive()={isGameplayActive}",
                    DebugUtility.Colors.Info);
            }

            _executionAllowedNow = currentPhase != null &&
                                   canResolveFromCatalog &&
                                   isGameplayActive &&
                                   _runContinuationSelectionRoutingService != null &&
                                   !_isExecutingRequest;

            _executionBlockedReason = ResolveExecutionBlockedReason(
                currentPhase,
                canResolveFromCatalog,
                isGameplayActive);

            _operationalStateReason = _executionAllowedNow
                ? "ready"
                : _executionBlockedReason;
        }

        private string ResolveExecutionBlockedReason(
            PhaseDefinitionAsset currentPhase,
            bool canResolveFromCatalog,
            bool isGameplayActive)
        {
            if (currentPhase == null)
            {
                return "no_current_phase";
            }

            if (!canResolveFromCatalog)
            {
                return "catalog_unavailable";
            }

            if (_runContinuationSelectionRoutingService == null)
            {
                return "run_continuation_routing_service_missing";
            }

            if (_isExecutingRequest)
            {
                return "executing";
            }

            if (_gameplayStateGate == null)
            {
                return "gameplay_state_gate_missing";
            }

            if (!isGameplayActive)
            {
                return "waiting_for_operational_ready";
            }

            return "not_ready";
        }

        private string BuildNavigationButtonLabel(string label, bool catalogCapabilityAvailable)
        {
            if (!catalogCapabilityAvailable)
            {
                return $"{label} (no target)";
            }

            return _isExecutingRequest
                ? $"{label} (target / executing)"
                : $"{label} (target)";
        }

        private static string BuildReadOnlyNavigationButtonLabel(string label, string targetPhaseId, bool catalogCapabilityAvailable)
        {
            string target = catalogCapabilityAvailable && !string.IsNullOrWhiteSpace(targetPhaseId)
                ? targetPhaseId
                : "no target";

            return $"{label} read-only ({target})";
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

        private string BuildPhaseLabel(PhaseDefinitionAsset selectedPhase, GameplayPhaseRuntimeSnapshot runtimeSnapshot, ParticipationSnapshot participationSnapshot)
        {
            PhaseDefinitionAsset currentPhase = ResolveCurrentPhase(selectedPhase, runtimeSnapshot);
            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"Id: {DescribePhaseId(currentPhase != null ? currentPhase.PhaseId : default)}");
            builder.AppendLine($"Content: {DescribeContentId(currentPhase, runtimeSnapshot)}");
            builder.AppendLine($"Index: {DescribeCatalogIndex(currentPhase != null ? currentPhase.PhaseId : default)}");
            builder.AppendLine($"Panel Visible: {(_panelVisible ? "Y" : "N")}");
            builder.AppendLine($"Navigation: {_navigationCapabilitySource} | {_traversalMode} | next={_resolvedNextPhaseId} | prev={_resolvedPreviousPhaseId}");
            builder.AppendLine($"Participation: {DescribeParticipationSignature(participationSnapshot)}");
            builder.AppendLine($"Participation Ready: {DescribeParticipationReadiness(participationSnapshot)}");
            builder.AppendLine($"Loop: {DescribeLoopCount()}");
            builder.AppendLine($"Catalog Capability Next/Prev: {(_catalogCapabilityNext ? "Y" : "N")}/{(_catalogCapabilityPrevious ? "Y" : "N")} (Prev read-only)");
            builder.AppendLine($"Execution Allowed Now: {(_executionAllowedNow ? "Y" : "N")}");
            builder.AppendLine($"Execution Blocked: {_executionBlockedReason}");
            builder.AppendLine($"Last Command: {_lastCommandResult} | rejectedReason={_lastCommandRejectedReason}");
            builder.AppendLine($"Ready: {(_executionAllowedNow ? "Y" : "N")} ({_operationalStateReason})");
            builder.AppendLine("Specific: phaseId | index 1-based (read-only resolve)");
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

            return $"Catalog: {catalogName} | Phase: {currentPhaseId} | Index: {currentIndex} | Loop: {loopCount} | Traversal: {_traversalMode}";
        }

        private static string BuildParticipationLabel(ParticipationSnapshot snapshot)
        {
            if (!snapshot.IsValid)
            {
                return "Participation: <empty>";
            }

            return $"Participation: sig={snapshot.Signature.Value} | readiness={snapshot.Readiness.State} | primary={snapshot.PrimaryParticipantId} | local={snapshot.LocalParticipantId} | count={snapshot.ParticipantCount}";
        }

        private static string DescribeParticipationSignature(ParticipationSnapshot snapshot)
        {
            return snapshot.IsValid ? snapshot.Signature.Value : "<empty>";
        }

        private static string DescribeParticipationReadiness(ParticipationSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"{snapshot.Readiness.State}:{snapshot.Readiness.Reason}"
                : "<empty>";
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

            return phase != null ? PhaseDefinitionId.BuildCanonicalIntroContentId(phase.PhaseId) : "<none>";
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
            if (_phaseCatalogNavigationService != null)
            {
                return _phaseCatalogNavigationService.LoopCount.ToString(CultureInfo.InvariantCulture);
            }

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

