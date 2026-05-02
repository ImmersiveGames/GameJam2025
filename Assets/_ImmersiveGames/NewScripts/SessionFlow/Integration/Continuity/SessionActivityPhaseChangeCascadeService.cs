using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Host.PostRun.Presentation;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionActivityPhaseChangeCascadeService : ISessionActivityPhaseChangeCascadeService, IDisposable
    {
        private readonly IRestartContextService _restartContextService;
        private readonly ISessionIntegrationNavigationHandoffService _navigationHandoffService;
        private readonly IPhaseResetExecutor _phaseResetExecutor;
        private readonly IPhaseCatalogNavigationService _phaseCatalogNavigationService;
        private readonly GameplayPhaseFlowService _phaseFlowService;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;
        private readonly EventBinding<RunResultStageCompletedEvent> _runResultStageCompletedBinding;
        private readonly object _ordinalNavigationSync = new();
        private CurrentSessionActivityPhaseChange _currentSessionActivityPhaseChange;
        private bool _disposed;

        public SessionActivityPhaseChangeCascadeService(
            ISessionIntegrationNavigationHandoffService navigationHandoffService,
            IRestartContextService restartContextService,
            IPhaseResetExecutor phaseResetExecutor,
            IPhaseCatalogNavigationService phaseCatalogNavigationService,
            GameplayPhaseFlowService phaseFlowService,
            ISceneCompositionExecutor sceneCompositionExecutor)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _navigationHandoffService = navigationHandoffService ?? throw new ArgumentNullException(nameof(navigationHandoffService));
            _phaseResetExecutor = phaseResetExecutor ?? throw new ArgumentNullException(nameof(phaseResetExecutor));
            _phaseCatalogNavigationService = phaseCatalogNavigationService ?? throw new ArgumentNullException(nameof(phaseCatalogNavigationService));
            _phaseFlowService = phaseFlowService ?? throw new ArgumentNullException(nameof(phaseFlowService));
            _sceneCompositionExecutor = sceneCompositionExecutor ?? throw new ArgumentNullException(nameof(sceneCompositionExecutor));
            _runResultStageCompletedBinding = new EventBinding<RunResultStageCompletedEvent>(OnRunResultStageCompleted);
            EventBus<RunResultStageCompletedEvent>.Register(_runResultStageCompletedBinding);
        }

        public Task<PhaseResetExecutionResult> RestartFromFirstPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            return RestartFromFirstPhaseInternalAsync(reason, ct);
        }

        private async Task<PhaseResetExecutionResult> RestartFromFirstPhaseInternalAsync(string reason, CancellationToken ct)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/RestartFromFirstPhase" : reason.Trim();
            GameplayStartSnapshot currentSnapshot = GameplayStartSnapshot.Empty;
            PhaseDefinitionSelectedEvent phaseSelectedEvent = default;
            PhaseDefinitionAsset targetPhaseRef = null;
            string catalogSignature = "<none>";

            try
            {
                ct.ThrowIfCancellationRequested();

                currentSnapshot = ResolveCurrentGameplayStartSnapshotOrFail(normalizedReason, "RestartFromFirstPhase");

                DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                    $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_requested operation='RestartFromFirstPhase' source='SessionTransitionExecutionPort' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' semantic='FirstPhaseRunRestart' legacy='false' target='RunContinuationOperational' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' scene='{AsText(currentSnapshot.PhaseDefinitionRef != null ? currentSnapshot.PhaseDefinitionRef.name : string.Empty)}'.",
                    DebugUtility.Colors.Info);

                PhaseCatalogNavigationPlan catalogPlan = _phaseCatalogNavigationService.RestartCatalog(normalizedReason);
                if (!catalogPlan.IsValid || !catalogPlan.IsChanged || catalogPlan.TargetPhaseRef == null || !catalogPlan.TargetPhaseRef.PhaseId.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                        $"[FATAL][H1][GameplaySessionFlow] RestartFromFirstPhase requires a valid changed catalog plan. outcome='{catalogPlan.Outcome}' reason='{normalizedReason}'.");
                }

                targetPhaseRef = catalogPlan.TargetPhaseRef;
                catalogSignature = DescribeCatalogName(_phaseCatalogNavigationService.Catalog);

                DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                    $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_catalog_resolved operation='RestartFromFirstPhase' source='{nameof(IPhaseCatalogNavigationService)}' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' targetPhase='{DescribePhase(targetPhaseRef)}' phaseIndex='<none>' catalogSignature='{AsText(catalogSignature)}' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' scene='{AsText(targetPhaseRef != null ? targetPhaseRef.name : string.Empty)}'.",
                    DebugUtility.Colors.Info);

                phaseSelectedEvent = _phaseFlowService.PublishPhaseDefinitionSelected(
                    targetPhaseRef,
                    currentSnapshot.MacroRouteId,
                    currentSnapshot.MacroRouteRef,
                    normalizedReason);

                SceneCompositionRequest applyRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                    targetPhaseRef,
                    normalizedReason,
                    phaseSelectedEvent.SelectionSignature,
                    forceFullReload: true);

                DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                    $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_handoff_started operation='RestartFromFirstPhase' source='SessionActivityPhaseChangeCascadeService' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' targetPhase='{DescribePhase(targetPhaseRef)}' phaseIndex='<none>' catalogSignature='{AsText(catalogSignature)}' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' executionSignature='{AsText(phaseSelectedEvent.SelectionSignature)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' scene='{AsText(applyRequest.ActiveScene)}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] scenesToUnload=[{string.Join(",", applyRequest.ScenesToUnload)}].",
                    DebugUtility.Colors.Info);

                SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(applyRequest, ct);
                if (!compositionResult.Success)
                {
                    HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                        $"[FATAL][H1][GameplaySessionFlow] RestartFromFirstPhase scene composition failed. targetPhase='{DescribePhase(targetPhaseRef)}' reason='{normalizedReason}' correlationId='{applyRequest.CorrelationId}'.");
                }

                PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                    targetPhaseRef,
                    applyRequest.ScenesToLoad,
                    applyRequest.ActiveScene,
                    PhaseFlowSignalVocabulary.PhaseDefinitionNavigationSource);

                PhaseResetExecutionResult resetResult = await ResetCurrentPhaseInternalAsync(normalizedReason, ct);
                if (resetResult.Succeeded)
                {
                    _phaseCatalogNavigationService.Commit(catalogPlan);
                }

                if (resetResult.Succeeded && resetResult.AllowsPhaseLocalEntryReady)
                {
                    DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                        $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_handoff_completed operation='RestartFromFirstPhase' source='SessionActivityPhaseChangeCascadeService' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' targetPhase='{DescribePhase(targetPhaseRef)}' phaseIndex='<none>' catalogSignature='{AsText(catalogSignature)}' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' executionSignature='{AsText(phaseSelectedEvent.SelectionSignature)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' scene='{AsText(applyRequest.ActiveScene)}' result='{resetResult}'.",
                        DebugUtility.Colors.Success);
                }

                return resetResult;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<SessionActivityPhaseChangeCascadeService>(
                    $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_handoff_failed operation='RestartFromFirstPhase' source='SessionActivityPhaseChangeCascadeService' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' targetPhase='{DescribePhase(targetPhaseRef)}' phaseIndex='<none>' catalogSignature='{AsText(catalogSignature)}' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' executionSignature='{AsText(phaseSelectedEvent.SelectionSignature)}' routeId='{(currentSnapshot.IsValid ? currentSnapshot.MacroRouteId.ToString() : "<none>")}' routeKind='{(currentSnapshot.IsValid ? currentSnapshot.MacroRouteRef.RouteKind.ToString() : "<none>")}' scene='{AsText(targetPhaseRef != null ? targetPhaseRef.name : string.Empty)}' exceptionType='{ex.GetType().Name}' exceptionMessage='{AsText(ex.Message)}'.");

                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] RestartFromFirstPhase failed. reason='{normalizedReason}' targetPhase='{DescribePhase(targetPhaseRef)}'.",
                    ex);
                throw;
            }
        }

        public async Task<PhaseResetExecutionResult> ResetCurrentPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/ResetCurrentPhase" : reason.Trim();

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='ResetCurrentPhase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffDispatch action='ResetCurrentPhase' reason='{normalizedReason}' target='PhaseResetOperational'.",
                DebugUtility.Colors.Info);
            PhaseResetExecutionResult result = await ResetCurrentPhaseInternalAsync(normalizedReason, ct);

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffCompleted action='ResetCurrentPhase' reason='{normalizedReason}' target='PhaseResetOperational' result='{result}'.",
                result.Succeeded ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            return result;
        }

        public async Task ExitToMenuAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/ExitToMenu" : reason.Trim();

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='ExitToMenu' reason='{normalizedReason}' handoff='Navigation'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await DispatchExitToMenuHandoffAsync(normalizedReason, ct);

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffCompleted action='ExitToMenu' reason='{normalizedReason}' handoff='Navigation'.",
                DebugUtility.Colors.Success);
        }

        public SessionActivityPhaseChangeCascadeResolution BeginPhaseChangeCascade(
            string operation,
            PhaseOrdinalNavigationKind navigationKind,
            PhaseNavigationDirection direction,
            PhaseDefinitionAsset targetPhaseRef,
            PhaseCatalogNavigationPlan navigationPlan,
            string reason = null,
            string source = null,
            SessionTransitionPlan plan = default,
            CancellationToken ct = default)
        {
            string normalizedOperation = string.IsNullOrWhiteSpace(operation) ? "PhaseOrdinalNavigation" : operation.Trim();
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? normalizedOperation : reason.Trim();
            string normalizedSource = string.IsNullOrWhiteSpace(source) ? nameof(SessionActivityPhaseChangeCascadeService) : source.Trim();

            ct.ThrowIfCancellationRequested();

            GameplayStartSnapshot currentSnapshot = ResolveCurrentGameplayStartSnapshotOrFail(normalizedReason, normalizedOperation);

            if (!_phaseFlowService.TryGetCurrent(out GameplayPhaseRuntimeSnapshot currentPhaseRuntime) || !currentPhaseRuntime.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] {normalizedOperation} requires a valid current phase runtime. reason='{normalizedReason}'.");
            }

            if (!currentPhaseRuntime.HasPhaseDefinitionRef ||
                currentPhaseRuntime.PhaseDefinitionRef == null ||
                !currentPhaseRuntime.PhaseDefinitionRef.PhaseId.IsValid ||
                currentSnapshot.PhaseDefinitionRef == null ||
                !currentSnapshot.PhaseDefinitionRef.PhaseId.IsValid ||
                !string.Equals(currentPhaseRuntime.PhaseDefinitionRef.PhaseId.Value, currentSnapshot.PhaseDefinitionRef.PhaseId.Value, StringComparison.Ordinal) ||
                !string.Equals(currentPhaseRuntime.SessionContext.SessionSignature, currentSnapshot.PhaseSignature, StringComparison.Ordinal) ||
                !string.Equals(currentPhaseRuntime.PhaseEntryIdentity.SessionSignature, currentSnapshot.PhaseSignature, StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] {normalizedOperation} requires the active gameplay phase runtime to match the current gameplay snapshot. snapshotPhase='{AsText(currentSnapshot.PhaseDefinitionRef != null ? currentSnapshot.PhaseDefinitionRef.name : string.Empty)}' runtimePhase='{AsText(currentPhaseRuntime.PhaseDefinitionRef != null ? currentPhaseRuntime.PhaseDefinitionRef.name : string.Empty)}' reason='{normalizedReason}'.");
            }

            if (!currentPhaseRuntime.PhaseEntryIdentity.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] {normalizedOperation} requires a valid current phase entry identity. reason='{normalizedReason}' phaseRuntimeSignature='{currentPhaseRuntime.PhaseRuntimeSignature}'.");
            }

            if (targetPhaseRef == null || !targetPhaseRef.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] {normalizedOperation} requires a valid target phase. reason='{normalizedReason}' currentPhase='{DescribePhase(currentSnapshot.PhaseDefinitionRef)}'.");
            }

            bool hasResultPresentationContract =
                currentPhaseRuntime.PhaseDefinitionRef != null &&
                currentPhaseRuntime.PhaseDefinitionRef.HasPhaseResultPresentation;

            SessionActivityPhaseChangeCascadeStageDisposition resultPresentationDisposition = hasResultPresentationContract
                ? SessionActivityPhaseChangeCascadeStageDisposition.Execute
                : SessionActivityPhaseChangeCascadeStageDisposition.SkipNoContent;

            SessionActivityPhaseChangeCascadeResolution resolution = new(
                normalizedOperation,
                navigationKind,
                direction,
                currentSnapshot,
                currentPhaseRuntime,
                targetPhaseRef,
                normalizedReason,
                normalizedSource,
                hasResultPresentationContract,
                SessionActivityPhaseChangeCascadeStageDisposition.SkipNoContent,
                resultPresentationDisposition,
                SessionActivityPhaseChangeCascadeStageDisposition.SkipNoContent);

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_deactivation_intent_resolved operation='{resolution.Operation}' navigationKind='{resolution.NavigationKind}' direction='{resolution.Direction}' deactivationDisposition='{resolution.DeactivationDisposition}' sessionSignature='{resolution.SessionSignature}' phaseRuntimeSignature='{resolution.CurrentPhaseRuntimeSignature}' routeId='{resolution.RouteId}' routeKind='{resolution.RouteKind}' phaseEntryId='{resolution.CurrentPhaseEntryIdentity.PhaseEntryId}' entrySignature='{resolution.EntrySignature}' phaseLocalEntrySequence='{resolution.PhaseLocalEntrySequence}' currentPhase='{resolution.CurrentPhaseId}' targetPhase='{resolution.TargetPhaseId}' reason='{resolution.Reason}' source='{resolution.Source}'.",
                DebugUtility.Colors.Info);

            EventBus<SessionActivityPhaseChangeCascadeDeactivationIntentResolvedEvent>.Raise(new SessionActivityPhaseChangeCascadeDeactivationIntentResolvedEvent(resolution));

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_deactivation_stage_resolved operation='{resolution.Operation}' stage='Deactivation' disposition='{resolution.DeactivationDisposition}' pipelineHandoff='{resolution.PipelineHandoffTarget}' sessionSignature='{resolution.SessionSignature}' phaseRuntimeSignature='{resolution.CurrentPhaseRuntimeSignature}' routeId='{resolution.RouteId}' routeKind='{resolution.RouteKind}' phaseEntryId='{resolution.CurrentPhaseEntryIdentity.PhaseEntryId}' targetPhase='{resolution.TargetPhaseId}' reason='{resolution.Reason}' source='{resolution.Source}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_result_presentation_resolved operation='{resolution.Operation}' stage='ResultPresentation' contract='{resolution.HasResultPresentationContract}' disposition='{resolution.ResultPresentationDisposition}' pipelineHandoff='{resolution.PipelineHandoffTarget}' sessionSignature='{resolution.SessionSignature}' phaseRuntimeSignature='{resolution.CurrentPhaseRuntimeSignature}' routeId='{resolution.RouteId}' routeKind='{resolution.RouteKind}' phaseEntryId='{resolution.CurrentPhaseEntryIdentity.PhaseEntryId}' targetPhase='{resolution.TargetPhaseId}' reason='{resolution.Reason}' source='{resolution.Source}'.",
                DebugUtility.Colors.Info);

            EventBus<SessionActivityPhaseChangeCascadeResultPresentationResolvedEvent>.Raise(new SessionActivityPhaseChangeCascadeResultPresentationResolvedEvent(resolution));

            if (resolution.ResultPresentationDisposition == SessionActivityPhaseChangeCascadeStageDisposition.SkipNoContent)
            {
                DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                    $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_result_presentation_skipped operation='{resolution.Operation}' stage='ResultPresentation' contract='{resolution.HasResultPresentationContract}' disposition='SkipNoContent' pipelineHandoff='{resolution.PipelineHandoffTarget}' sessionSignature='{resolution.SessionSignature}' phaseRuntimeSignature='{resolution.CurrentPhaseRuntimeSignature}' routeId='{resolution.RouteId}' routeKind='{resolution.RouteKind}' phaseEntryId='{resolution.CurrentPhaseEntryIdentity.PhaseEntryId}' targetPhase='{resolution.TargetPhaseId}' reason='{resolution.Reason}' source='{resolution.Source}' detail='no_result_presentation_contract'.",
                    DebugUtility.Colors.Info);

                EventBus<SessionActivityPhaseChangeCascadeResultPresentationSkippedNoContentEvent>.Raise(
                    new SessionActivityPhaseChangeCascadeResultPresentationSkippedNoContentEvent(resolution, "no_result_presentation_contract"));

                DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                    $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_continuity_decision_resolved operation='{resolution.Operation}' stage='ContinuityDecision' disposition='{resolution.ContinuityDisposition}' pipelineHandoff='{resolution.PipelineHandoffTarget}' sessionSignature='{resolution.SessionSignature}' phaseRuntimeSignature='{resolution.CurrentPhaseRuntimeSignature}' routeId='{resolution.RouteId}' routeKind='{resolution.RouteKind}' phaseEntryId='{resolution.CurrentPhaseEntryIdentity.PhaseEntryId}' targetPhase='{resolution.TargetPhaseId}' reason='{resolution.Reason}' source='{resolution.Source}'.",
                    DebugUtility.Colors.Info);

                EventBus<SessionActivityPhaseChangeCascadeContinuityDecisionSkippedNoContentEvent>.Raise(
                    new SessionActivityPhaseChangeCascadeContinuityDecisionSkippedNoContentEvent(resolution, "target_already_determined"));

                PublishPhaseChangePipelineHandoff(resolution, navigationPlan, plan, normalizedSource, ct);
            }

            return resolution;
        }

        public void OpenPhaseChangeCascadeResultPresentation(
            SessionActivityPhaseChangeCascadeResolution resolution,
            PhaseCatalogNavigationPlan navigationPlan,
            SessionTransitionPlan plan,
            string source = null)
        {
            if (!resolution.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] Phase change cascade requires valid resolution. operation='{resolution.Operation}' reason='{resolution.Reason}'.");
            }

            if (resolution.ResultPresentationDisposition != SessionActivityPhaseChangeCascadeStageDisposition.Execute)
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] Phase change cascade result presentation can only be opened when the stage executes. operation='{resolution.Operation}' disposition='{resolution.ResultPresentationDisposition}' reason='{resolution.Reason}'.");
            }

            RunResultStage stage = BuildPhaseChangeResultStage(resolution);
            var control = new PhaseChangeResultPresentationControl(stage);
            IRunResultStagePresenterHost presenterHost = ResolvePhaseResultPresentationHostOrFail(resolution);

            if (!presenterHost.TryEnsureCurrentPresenter(stage, control, nameof(SessionActivityPhaseChangeCascadeService), out IRunResultStagePresenter presenter) ||
                presenter == null)
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseResultPresentation declarada pela phase atual, mas o presenter local nao foi resolvido. operation='{resolution.Operation}' targetPhase='{resolution.TargetPhaseId}' reason='{resolution.Reason}'.");
            }

            lock (_ordinalNavigationSync)
            {
                _currentSessionActivityPhaseChange = new CurrentSessionActivityPhaseChange(
                    resolution,
                    navigationPlan,
                    plan,
                    presenterHost,
                    source);
            }

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_result_presentation_entered operation='{resolution.Operation}' stage='ResultPresentation' contract='{resolution.HasResultPresentationContract}' disposition='Execute' pipelineHandoff='{resolution.PipelineHandoffTarget}' sessionSignature='{resolution.SessionSignature}' phaseRuntimeSignature='{resolution.CurrentPhaseRuntimeSignature}' routeId='{resolution.RouteId}' routeKind='{resolution.RouteKind}' phaseEntryId='{resolution.CurrentPhaseEntryIdentity.PhaseEntryId}' entrySignature='{resolution.EntrySignature}' targetPhase='{resolution.TargetPhaseId}' reason='{resolution.Reason}' source='{AsText(source)}' presenter='{presenter.GetType().Name}'.",
                DebugUtility.Colors.Info);

            EventBus<SessionActivityPhaseChangeCascadeResultPresentationEnteredEvent>.Raise(
                new SessionActivityPhaseChangeCascadeResultPresentationEnteredEvent(resolution, stage));

            EventBus<RunResultStageEnteredEvent>.Raise(new RunResultStageEnteredEvent(stage));
        }

        public void PublishPhaseChangePipelineHandoff(
            SessionActivityPhaseChangeCascadeResolution resolution,
            PhaseCatalogNavigationPlan navigationPlan,
            SessionTransitionPlan plan,
            string source = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!resolution.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] Ordinal navigation handoff requer resolucao valida. operation='{resolution.Operation}' reason='{resolution.Reason}'.");
            }

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_pipeline_handoff_requested operation='{resolution.Operation}' handoffTarget='{resolution.PipelineHandoffTarget}' targetPhase='{resolution.TargetPhaseId}' targetPhaseName='{resolution.TargetPhaseName}' sessionSignature='{resolution.SessionSignature}' phaseRuntimeSignature='{resolution.CurrentPhaseRuntimeSignature}' routeId='{resolution.RouteId}' routeKind='{resolution.RouteKind}' phaseEntryId='{resolution.CurrentPhaseEntryIdentity.PhaseEntryId}' entrySignature='{resolution.EntrySignature}' phaseLocalEntrySequence='{resolution.PhaseLocalEntrySequence}' reason='{resolution.Reason}' source='{AsText(source)}'.",
                DebugUtility.Colors.Success);

            EventBus<SessionActivityPhaseChangeCascadePipelineHandoffReadyEvent>.Raise(
                new SessionActivityPhaseChangeCascadePipelineHandoffReadyEvent(resolution, plan, source));

            EventBus<SessionActivityPhaseChangeCascadePipelineHandoffRequestedEvent>.Raise(
                new SessionActivityPhaseChangeCascadePipelineHandoffRequestedEvent(resolution, navigationPlan, plan, source));

            EventBus<SessionActivityPhaseChangeCascadeResolvedEvent>.Raise(new SessionActivityPhaseChangeCascadeResolvedEvent(resolution));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<RunResultStageCompletedEvent>.Unregister(_runResultStageCompletedBinding);
        }

        private void OnRunResultStageCompleted(RunResultStageCompletedEvent evt)
        {
            if (_disposed || string.IsNullOrWhiteSpace(evt.Stage.Signature))
            {
                return;
            }

            CurrentSessionActivityPhaseChange activeCascade;
            lock (_ordinalNavigationSync)
            {
                activeCascade = _currentSessionActivityPhaseChange;
                if (activeCascade == null || !activeCascade.IsValid)
                {
                    return;
                }

                if (!activeCascade.Matches(evt))
                {
                    DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                        $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_result_presentation_completed_ignored reason='stale_or_foreign' stageSignature='{AsText(evt.Stage.Signature)}' activeCascadeSignature='{AsText(activeCascade.Resolution.CurrentPhaseRuntimeSignature)}' operation='{activeCascade.Resolution.Operation}' targetPhase='{activeCascade.Resolution.TargetPhaseId}'.",
                        DebugUtility.Colors.Warning);
                    return;
                }

                _currentSessionActivityPhaseChange = default;
            }

            CompletePhaseChangeCascade(activeCascade, evt);
        }

        private void CompletePhaseChangeCascade(CurrentSessionActivityPhaseChange activeCascade, RunResultStageCompletedEvent evt)
        {
            try
            {
                DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                    $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_result_presentation_completed operation='{activeCascade.Resolution.Operation}' stage='ResultPresentation' contract='{activeCascade.Resolution.HasResultPresentationContract}' disposition='Execute' pipelineHandoff='{activeCascade.Resolution.PipelineHandoffTarget}' sessionSignature='{activeCascade.Resolution.SessionSignature}' phaseRuntimeSignature='{activeCascade.Resolution.CurrentPhaseRuntimeSignature}' routeId='{activeCascade.Resolution.RouteId}' routeKind='{activeCascade.Resolution.RouteKind}' phaseEntryId='{activeCascade.Resolution.CurrentPhaseEntryIdentity.PhaseEntryId}' targetPhase='{activeCascade.Resolution.TargetPhaseId}' reason='{evt.Completion.Reason}' kind='{evt.Completion.Kind}' source='{activeCascade.Source}'.",
                    DebugUtility.Colors.Success);

                activeCascade.PresenterHost?.TryDetachCurrentPresenter("ordinal_navigation_result_presentation_completed");

                EventBus<SessionActivityPhaseChangeCascadeResultPresentationCompletedEvent>.Raise(
                    new SessionActivityPhaseChangeCascadeResultPresentationCompletedEvent(activeCascade.Resolution, evt.Stage, evt.Completion));

                DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                    $"[OBS][GameplaySessionFlow][Continuity] ordinal_navigation_continuity_decision_resolved operation='{activeCascade.Resolution.Operation}' stage='ContinuityDecision' disposition='{activeCascade.Resolution.ContinuityDisposition}' pipelineHandoff='{activeCascade.Resolution.PipelineHandoffTarget}' sessionSignature='{activeCascade.Resolution.SessionSignature}' phaseRuntimeSignature='{activeCascade.Resolution.CurrentPhaseRuntimeSignature}' routeId='{activeCascade.Resolution.RouteId}' routeKind='{activeCascade.Resolution.RouteKind}' phaseEntryId='{activeCascade.Resolution.CurrentPhaseEntryIdentity.PhaseEntryId}' targetPhase='{activeCascade.Resolution.TargetPhaseId}' reason='{activeCascade.Resolution.Reason}' source='{activeCascade.Source}'.",
                    DebugUtility.Colors.Info);

                EventBus<SessionActivityPhaseChangeCascadeContinuityDecisionSkippedNoContentEvent>.Raise(
                    new SessionActivityPhaseChangeCascadeContinuityDecisionSkippedNoContentEvent(activeCascade.Resolution, "target_already_determined"));

                PublishPhaseChangePipelineHandoff(activeCascade.Resolution, activeCascade.NavigationPlan, activeCascade.Plan, activeCascade.Source, CancellationToken.None);
            }
            catch (Exception ex)
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] Phase change cascade failed after result presentation completion. operation='{activeCascade.Resolution.Operation}' targetPhase='{activeCascade.Resolution.TargetPhaseId}' reason='{activeCascade.Resolution.Reason}' exceptionType='{ex.GetType().Name}' exceptionMessage='{AsText(ex.Message)}'.",
                    ex);
            }
        }

        private static RunResultStage BuildPhaseChangeResultStage(SessionActivityPhaseChangeCascadeResolution resolution)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            RunEndIntent intent = new(
                signature: resolution.CurrentPhaseRuntimeSignature,
                sceneName: sceneName,
                profile: nameof(SessionActivityPhaseChangeCascadeService),
                frame: Time.frameCount,
                reason: resolution.Reason,
                isGameplayScene: resolution.RouteKind == SceneRouteKind.Gameplay);

            RunContinuationContext continuationContext = new(
                intent,
                RunResult.PhaseChange,
                new[] { RunContinuationKind.AdvancePhase },
                requiresPlayerDecision: false);

            return new RunResultStage(continuationContext);
        }

        private IRunResultStagePresenterHost ResolvePhaseResultPresentationHostOrFail(SessionActivityPhaseChangeCascadeResolution resolution)
        {
            if (!TryResolvePhaseResultPresentationPresenterHost(out IRunResultStagePresenterHost presenterHost) ||
                presenterHost == null)
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseResultPresentation declarada pela phase atual, mas o presenter host local nao esta disponivel. operation='{resolution.Operation}' targetPhase='{resolution.TargetPhaseId}' reason='{resolution.Reason}'.");
            }

            return presenterHost;
        }

        private sealed class PhaseChangeResultPresentationControl : IRunResultStageControl
        {
            private readonly RunResultStage _stage;
            private bool _isActive = true;
            private bool _hasCompleted;

            public PhaseChangeResultPresentationControl(RunResultStage stage)
            {
                _stage = stage;
            }

            public bool IsActive => _isActive;
            public bool HasCompleted => _hasCompleted;
            public RunResultStage CurrentStage => _stage;

            public bool TryComplete(string reason = null)
            {
                if (!_isActive || _hasCompleted)
                {
                    return false;
                }

                _isActive = false;
                _hasCompleted = true;

                RunResultStageCompletion completion = new(RunResultStageCompletionKind.Continue, reason);
                EventBus<RunResultStageCompletedEvent>.Raise(new RunResultStageCompletedEvent(_stage, completion));
                return true;
            }
        }

        private sealed class CurrentSessionActivityPhaseChange
        {
            public CurrentSessionActivityPhaseChange(
                SessionActivityPhaseChangeCascadeResolution resolution,
                PhaseCatalogNavigationPlan navigationPlan,
                SessionTransitionPlan plan,
                IRunResultStagePresenterHost presenterHost,
                string source)
            {
                Resolution = resolution;
                NavigationPlan = navigationPlan;
                Plan = plan;
                PresenterHost = presenterHost;
                Source = Normalize(source);
            }

            public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
            public PhaseCatalogNavigationPlan NavigationPlan { get; }
            public SessionTransitionPlan Plan { get; }
            public IRunResultStagePresenterHost PresenterHost { get; }
            public string Source { get; }

            public bool IsValid => Resolution.IsValid;

            public bool Matches(RunResultStageCompletedEvent evt)
            {
                if (string.IsNullOrWhiteSpace(evt.Stage.Signature))
                {
                    return false;
                }

                return string.Equals(evt.Stage.Signature, Resolution.CurrentPhaseRuntimeSignature, StringComparison.Ordinal) ||
                       string.Equals(evt.Stage.Signature, Resolution.CurrentPhaseEntryIdentity.EntrySignature, StringComparison.Ordinal);
            }
        }

        private static bool TryResolvePhaseResultPresentationPresenterHost(out IRunResultStagePresenterHost presenterHost)
        {
            presenterHost = null;

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return false;
            }

            return DependencyManager.Provider.TryGetGlobal<IRunResultStagePresenterHost>(out presenterHost) &&
                   presenterHost != null;
        }

        private async Task DispatchExitToMenuHandoffAsync(string reason, CancellationToken ct)
        {
            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Handoff] ExitToMenuDispatch action='ExitToMenu' reason='{reason}' target='Navigation'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _navigationHandoffService.RequestExitToMenuAsync(reason, nameof(SessionActivityPhaseChangeCascadeService), ct);
        }

        private async Task<PhaseResetExecutionResult> ResetCurrentPhaseInternalAsync(string reason, CancellationToken ct)
        {
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasPhaseDefinitionRef ||
                snapshot.PhaseDefinitionRef == null ||
                snapshot.MacroRouteRef == null ||
                !snapshot.MacroRouteId.IsValid ||
                string.IsNullOrWhiteSpace(snapshot.PhaseSignature))
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] ResetCurrentPhaseAsync requires a valid current gameplay snapshot. reason='{reason}'.");
            }

            DebugUtility.Log<SessionActivityPhaseChangeCascadeService>(
                $"[OBS][GameplaySessionFlow][Continuity] ResetCurrentPhaseRequested rail='phase' phaseRef='{snapshot.PhaseDefinitionRef.name}' routeId='{snapshot.MacroRouteId}' v='{snapshot.SelectionVersion}' reason='{reason}' phaseSignature='{snapshot.PhaseSignature}'.",
                DebugUtility.Colors.Info);

            PhaseResetContext resetContext = new PhaseResetContext(
                snapshot.PhaseDefinitionRef,
                snapshot.MacroRouteId,
                new PhaseContextSignature(snapshot.PhaseSignature),
                snapshot.PhaseSignature);

            return await _phaseResetExecutor.ResetPhaseAsync(resetContext, reason, ct);
        }

        private GameplayStartSnapshot ResolveCurrentGameplayStartSnapshotOrFail(string reason, string operation)
        {
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasPhaseDefinitionRef ||
                snapshot.PhaseDefinitionRef == null ||
                snapshot.MacroRouteRef == null ||
                !snapshot.MacroRouteId.IsValid ||
                string.IsNullOrWhiteSpace(snapshot.PhaseSignature))
            {
                HardFailFastH1.Trigger(typeof(SessionActivityPhaseChangeCascadeService),
                    $"[FATAL][H1][GameplaySessionFlow] {operation} requires a valid current gameplay snapshot. reason='{reason}'.");
            }

            return snapshot;
        }

        private static string DescribePhase(PhaseDefinitionAsset phaseDefinitionRef)
        {
            return phaseDefinitionRef != null && phaseDefinitionRef.PhaseId.IsValid
                ? phaseDefinitionRef.PhaseId.Value
                : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        private static string DescribeCatalogName(IPhaseDefinitionCatalog catalog)
        {
            if (catalog is UnityEngine.Object unityObject)
            {
                return unityObject.name;
            }

            return catalog != null ? catalog.GetType().Name : "<none>";
        }

    }
}
