using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionFlowContinuityService : IGameplaySessionFlowContinuityService
    {
        private readonly IRestartContextService _restartContextService;
        private readonly ISessionIntegrationNavigationHandoffService _navigationHandoffService;
        private readonly IPhaseResetExecutor _phaseResetExecutor;

        public GameplaySessionFlowContinuityService(
            ISessionIntegrationNavigationHandoffService navigationHandoffService,
            IRestartContextService restartContextService,
            IPhaseResetExecutor phaseResetExecutor)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _navigationHandoffService = navigationHandoffService ?? throw new ArgumentNullException(nameof(navigationHandoffService));
            _phaseResetExecutor = phaseResetExecutor ?? throw new ArgumentNullException(nameof(phaseResetExecutor));
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

                DebugUtility.Log<GameplaySessionFlowContinuityService>(
                    $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_requested operation='RestartFromFirstPhase' source='SessionTransitionExecutionPort' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' semantic='FirstPhaseRunRestart' legacy='false' target='RunContinuationOperational' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' scene='{AsText(currentSnapshot.PhaseDefinitionRef != null ? currentSnapshot.PhaseDefinitionRef.name : string.Empty)}'.",
                    DebugUtility.Colors.Info);

                IPhaseCatalogNavigationService catalogNavigationService = ResolveRequiredGlobal<IPhaseCatalogNavigationService>(nameof(IPhaseCatalogNavigationService), normalizedReason);
                PhaseCatalogNavigationPlan catalogPlan = catalogNavigationService.RestartCatalog(normalizedReason);
                if (!catalogPlan.IsValid || !catalogPlan.IsChanged || catalogPlan.TargetPhaseRef == null || !catalogPlan.TargetPhaseRef.PhaseId.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                        $"[FATAL][H1][GameplaySessionFlow] RestartFromFirstPhase requires a valid changed catalog plan. outcome='{catalogPlan.Outcome}' reason='{normalizedReason}'.");
                }

                targetPhaseRef = catalogPlan.TargetPhaseRef;
                catalogNavigationService.Commit(catalogPlan);
                catalogSignature = DescribeCatalogName(catalogNavigationService.Catalog);

                DebugUtility.Log<GameplaySessionFlowContinuityService>(
                    $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_catalog_resolved operation='RestartFromFirstPhase' source='{nameof(IPhaseCatalogNavigationService)}' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' targetPhase='{DescribePhase(targetPhaseRef)}' phaseIndex='<none>' catalogSignature='{AsText(catalogSignature)}' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' scene='{AsText(targetPhaseRef != null ? targetPhaseRef.name : string.Empty)}'.",
                    DebugUtility.Colors.Info);

                GameplayPhaseFlowService phaseFlowService = ResolveRequiredGlobal<GameplayPhaseFlowService>(nameof(GameplayPhaseFlowService), normalizedReason);
                phaseSelectedEvent = phaseFlowService.PublishPhaseDefinitionSelected(
                    targetPhaseRef,
                    currentSnapshot.MacroRouteId,
                    currentSnapshot.MacroRouteRef,
                    normalizedReason);

                ISceneCompositionExecutor sceneCompositionExecutor = ResolveRequiredGlobal<ISceneCompositionExecutor>(nameof(ISceneCompositionExecutor), normalizedReason);
                SceneCompositionRequest applyRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                    targetPhaseRef,
                    normalizedReason,
                    phaseSelectedEvent.SelectionSignature,
                    forceFullReload: true);

                DebugUtility.Log<GameplaySessionFlowContinuityService>(
                    $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_handoff_started operation='RestartFromFirstPhase' source='GameplaySessionFlowContinuityService' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' targetPhase='{DescribePhase(targetPhaseRef)}' phaseIndex='<none>' catalogSignature='{AsText(catalogSignature)}' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' executionSignature='{AsText(phaseSelectedEvent.SelectionSignature)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' scene='{AsText(applyRequest.ActiveScene)}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] scenesToUnload=[{string.Join(",", applyRequest.ScenesToUnload)}].",
                    DebugUtility.Colors.Info);

                SceneCompositionResult compositionResult = await sceneCompositionExecutor.ApplyAsync(applyRequest, ct);
                if (!compositionResult.Success)
                {
                    HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                        $"[FATAL][H1][GameplaySessionFlow] RestartFromFirstPhase scene composition failed. targetPhase='{DescribePhase(targetPhaseRef)}' reason='{normalizedReason}' correlationId='{applyRequest.CorrelationId}'.");
                }

                PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                    targetPhaseRef,
                    applyRequest.ScenesToLoad,
                    applyRequest.ActiveScene,
                    PhaseFlowSignalVocabulary.PhaseDefinitionNavigationSource);

                PhaseResetExecutionResult resetResult = await ResetCurrentPhaseInternalAsync(normalizedReason, ct);
                if (resetResult.Succeeded && resetResult.AllowsPhaseLocalEntryReady)
                {
                    DebugUtility.Log<GameplaySessionFlowContinuityService>(
                        $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_handoff_completed operation='RestartFromFirstPhase' source='GameplaySessionFlowContinuityService' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' targetPhase='{DescribePhase(targetPhaseRef)}' phaseIndex='<none>' catalogSignature='{AsText(catalogSignature)}' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' executionSignature='{AsText(phaseSelectedEvent.SelectionSignature)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' scene='{AsText(applyRequest.ActiveScene)}' result='{resetResult}'.",
                        DebugUtility.Colors.Success);
                }

                return resetResult;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameplaySessionFlowContinuityService>(
                    $"[OBS][GameplaySessionFlow][Continuity] restart_from_first_phase_handoff_failed operation='RestartFromFirstPhase' source='GameplaySessionFlowContinuityService' reason='{normalizedReason}' selectedContinuation='RestartFromFirstPhase' targetPhase='{DescribePhase(targetPhaseRef)}' phaseIndex='<none>' catalogSignature='{AsText(catalogSignature)}' contextSignature='{AsText(currentSnapshot.PhaseSignature)}' executionSignature='{AsText(phaseSelectedEvent.SelectionSignature)}' routeId='{(currentSnapshot.IsValid ? currentSnapshot.MacroRouteId.ToString() : "<none>")}' routeKind='{(currentSnapshot.IsValid ? currentSnapshot.MacroRouteRef.RouteKind.ToString() : "<none>")}' scene='{AsText(targetPhaseRef != null ? targetPhaseRef.name : string.Empty)}' exceptionType='{ex.GetType().Name}' exceptionMessage='{AsText(ex.Message)}'.");

                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] RestartFromFirstPhase failed. reason='{normalizedReason}' targetPhase='{DescribePhase(targetPhaseRef)}'.",
                    ex);
                throw;
            }
        }

        public async Task<PhaseResetExecutionResult> ResetCurrentPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/ResetCurrentPhase" : reason.Trim();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='ResetCurrentPhase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffDispatch action='ResetCurrentPhase' reason='{normalizedReason}' target='PhaseResetOperational'.",
                DebugUtility.Colors.Info);
            PhaseResetExecutionResult result = await ResetCurrentPhaseInternalAsync(normalizedReason, ct);

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffCompleted action='ResetCurrentPhase' reason='{normalizedReason}' target='PhaseResetOperational' result='{result}'.",
                result.Succeeded ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            return result;
        }

        public async Task ExitToMenuAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/ExitToMenu" : reason.Trim();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='ExitToMenu' reason='{normalizedReason}' handoff='Navigation'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await DispatchExitToMenuHandoffAsync(normalizedReason, ct);

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffCompleted action='ExitToMenu' reason='{normalizedReason}' handoff='Navigation'.",
                DebugUtility.Colors.Success);
        }

        private async Task DispatchExitToMenuHandoffAsync(string reason, CancellationToken ct)
        {
            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Handoff] ExitToMenuDispatch action='ExitToMenu' reason='{reason}' target='Navigation'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _navigationHandoffService.RequestExitToMenuAsync(reason, nameof(GameplaySessionFlowContinuityService), ct);
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
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] ResetCurrentPhaseAsync requires a valid current gameplay snapshot. reason='{reason}'.");
            }

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
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
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] {operation} requires a valid current gameplay snapshot. reason='{reason}'.");
            }

            return snapshot;
        }

        private static T ResolveRequiredGlobal<T>(string label, string reason)
            where T : class
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] DependencyManager.Provider is null while resolving '{label}'. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] Missing required global service '{label}'. reason='{reason}'.");
            }

            return service;
        }

        private static string DescribePhase(PhaseDefinitionAsset phaseDefinitionRef)
        {
            return phaseDefinitionRef != null && phaseDefinitionRef.PhaseId.IsValid
                ? phaseDefinitionRef.PhaseId.Value
                : "<none>";
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
