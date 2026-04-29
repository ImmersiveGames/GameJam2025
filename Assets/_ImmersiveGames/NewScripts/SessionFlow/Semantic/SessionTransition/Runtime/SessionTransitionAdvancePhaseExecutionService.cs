using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public interface ISessionTransitionAdvancePhaseExecutionService
    {
        Task<PhaseNavigationResult> AdvanceAsync(SessionTransitionPlan plan, CancellationToken ct = default);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionTransitionAdvancePhaseExecutionService : ISessionTransitionAdvancePhaseExecutionService
    {
        private readonly IRestartContextService _restartContextService;
        private readonly IPhaseCatalogNavigationService _phaseCatalogNavigationService;
        private readonly GameplayPhaseFlowService _gameplayPhaseFlowService;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;

        public SessionTransitionAdvancePhaseExecutionService(
            IRestartContextService restartContextService,
            IPhaseCatalogNavigationService phaseCatalogNavigationService,
            GameplayPhaseFlowService gameplayPhaseFlowService,
            ISceneCompositionExecutor sceneCompositionExecutor)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _phaseCatalogNavigationService = phaseCatalogNavigationService ?? throw new ArgumentNullException(nameof(phaseCatalogNavigationService));
            _gameplayPhaseFlowService = gameplayPhaseFlowService ?? throw new ArgumentNullException(nameof(gameplayPhaseFlowService));
            _sceneCompositionExecutor = sceneCompositionExecutor ?? throw new ArgumentNullException(nameof(sceneCompositionExecutor));
        }

        public async Task<PhaseNavigationResult> AdvanceAsync(SessionTransitionPlan plan, CancellationToken ct = default)
        {
            ValidatePlanOrFail(plan);

            string normalizedReason = NormalizeReason(plan.Reason);
            ct.ThrowIfCancellationRequested();

            GameplayStartSnapshot currentSnapshot = ResolveCurrentSnapshotOrFail(normalizedReason);
            PhaseCatalogNavigationPlan navigationPlan = _phaseCatalogNavigationService.ResolveNext(normalizedReason);

            DebugUtility.Log<SessionTransitionAdvancePhaseExecutionService>(
                $"[OBS][GameplaySessionFlow][SessionTransition] advance_phase_catalog_resolved operation='AdvancePhase' source='SessionTransitionAdvancePhaseExecutionService' reason='{normalizedReason}' outcome='{navigationPlan.Outcome}' from='{DescribePhase(navigationPlan.CurrentCommitted)}' to='{DescribePhase(navigationPlan.TargetPhaseRef)}' catalog='{NormalizeForLog(navigationPlan.CatalogName)}' traversalMode='{navigationPlan.TraversalMode}' wasWrapped='{navigationPlan.WasWrapped.ToString().ToLowerInvariant()}'.",
                navigationPlan.Outcome == PhaseNavigationOutcome.Changed ? DebugUtility.Colors.Info : DebugUtility.Colors.Warning);

            if (navigationPlan.Outcome != PhaseNavigationOutcome.Changed)
            {
                return new PhaseNavigationResult(
                    navigationPlan.Request,
                    navigationPlan.Outcome,
                    navigationPlan.CurrentCommitted,
                    navigationPlan.CatalogName,
                    navigationPlan.TraversalMode,
                    navigationPlan.WasWrapped,
                    default);
            }

            ValidateNavigationPlanAgainstSnapshotOrFail(navigationPlan, currentSnapshot, normalizedReason);

            _phaseCatalogNavigationService.Commit(navigationPlan);

            PhaseDefinitionSelectedEvent phaseSelectedEvent = _gameplayPhaseFlowService.PublishPhaseDefinitionSelected(
                navigationPlan.TargetPhaseRef,
                currentSnapshot.MacroRouteId,
                currentSnapshot.MacroRouteRef,
                normalizedReason);

            var selectionContext = new PhaseNavigationSelectionContext(
                currentSnapshot,
                navigationPlan.TargetPhaseRef,
                phaseSelectedEvent,
                normalizedReason,
                ResolveActiveSceneNameOrFail(normalizedReason),
                PhaseNavigationDirection.Next,
                forceFullReload: false);

            SceneCompositionRequest applyRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                navigationPlan.TargetPhaseRef,
                normalizedReason,
                phaseSelectedEvent.SelectionSignature,
                forceFullReload: false);

            DebugUtility.Log<SessionTransitionAdvancePhaseExecutionService>(
                $"[OBS][GameplaySessionFlow][SessionTransition] advance_phase_content_apply_started operation='AdvancePhase' source='SessionTransitionAdvancePhaseExecutionService' reason='{normalizedReason}' from='{DescribePhase(navigationPlan.CurrentCommitted)}' to='{DescribePhase(navigationPlan.TargetPhaseRef)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' executionSignature='{phaseSelectedEvent.SelectionSignature}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] scenesToUnload=[{string.Join(",", applyRequest.ScenesToUnload)}].",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(applyRequest, ct);
            if (!compositionResult.Success)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionAdvancePhaseExecutionService),
                    $"[FATAL][H1][SessionTransition] AdvancePhase scene composition failed. from='{DescribePhase(navigationPlan.CurrentCommitted)}' to='{DescribePhase(navigationPlan.TargetPhaseRef)}' reason='{normalizedReason}' correlationId='{applyRequest.CorrelationId}'.");
            }

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                navigationPlan.TargetPhaseRef,
                applyRequest.ScenesToLoad,
                applyRequest.ActiveScene,
                PhaseFlowSignalVocabulary.SessionTransitionAdvancePhaseSource);

            _phaseCatalogNavigationService.ClearPendingTarget(normalizedReason);

            DebugUtility.Log<SessionTransitionAdvancePhaseExecutionService>(
                $"[OBS][GameplaySessionFlow][SessionTransition] advance_phase_content_apply_completed operation='AdvancePhase' source='SessionTransitionAdvancePhaseExecutionService' reason='{normalizedReason}' from='{DescribePhase(navigationPlan.CurrentCommitted)}' to='{DescribePhase(navigationPlan.TargetPhaseRef)}' routeId='{currentSnapshot.MacroRouteId}' routeKind='{currentSnapshot.MacroRouteRef.RouteKind}' executionSignature='{phaseSelectedEvent.SelectionSignature}' activeScene='{NormalizeForLog(applyRequest.ActiveScene)}' outcome='{navigationPlan.Outcome}' wasWrapped='{navigationPlan.WasWrapped.ToString().ToLowerInvariant()}'.",
                DebugUtility.Colors.Success);

            return new PhaseNavigationResult(
                navigationPlan.Request,
                navigationPlan.Outcome,
                navigationPlan.CurrentCommitted,
                navigationPlan.CatalogName,
                navigationPlan.TraversalMode,
                navigationPlan.WasWrapped,
                selectionContext);
        }

        private GameplayStartSnapshot ResolveCurrentSnapshotOrFail(string reason)
        {
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasPhaseDefinitionRef ||
                snapshot.PhaseDefinitionRef == null ||
                !snapshot.PhaseDefinitionRef.PhaseId.IsValid ||
                snapshot.MacroRouteRef == null ||
                !snapshot.MacroRouteId.IsValid ||
                snapshot.MacroRouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionAdvancePhaseExecutionService),
                    $"[FATAL][H1][SessionTransition] AdvancePhase requires a valid current gameplay snapshot. reason='{reason}'.");
            }

            return snapshot;
        }

        private static void ValidatePlanOrFail(SessionTransitionPlan plan)
        {
            if (!plan.IsValid ||
                plan.IntentKind != SessionTransitionIntentKind.AdvancePhase ||
                plan.Execution.Kind != SessionTransitionExecutionKind.NextPhase ||
                !plan.EmitsPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionAdvancePhaseExecutionService),
                    $"[FATAL][H1][SessionTransition] AdvancePhase execution received an invalid SessionTransitionPlan. intent='{plan.IntentKind}' executionKind='{plan.Execution.Kind}' phaseLocalEntryReady='{plan.EmitsPhaseLocalEntryReady}' reason='{NormalizeForLog(plan.Reason)}'.");
            }
        }

        private static void ValidateNavigationPlanAgainstSnapshotOrFail(
            PhaseCatalogNavigationPlan navigationPlan,
            GameplayStartSnapshot snapshot,
            string reason)
        {
            if (!HasSamePhase(navigationPlan.CurrentCommitted, snapshot.PhaseDefinitionRef))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionAdvancePhaseExecutionService),
                    $"[FATAL][H1][SessionTransition] AdvancePhase catalog current phase does not match gameplay snapshot. catalogCurrent='{DescribePhase(navigationPlan.CurrentCommitted)}' snapshotCurrent='{DescribePhase(snapshot.PhaseDefinitionRef)}' target='{DescribePhase(navigationPlan.TargetPhaseRef)}' reason='{reason}'.");
            }

            if (navigationPlan.TargetPhaseRef == null || !navigationPlan.TargetPhaseRef.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionAdvancePhaseExecutionService),
                    $"[FATAL][H1][SessionTransition] AdvancePhase requires a valid target phase. current='{DescribePhase(navigationPlan.CurrentCommitted)}' reason='{reason}'.");
            }
        }

        private static bool HasSamePhase(PhaseDefinitionAsset left, PhaseDefinitionAsset right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || !left.PhaseId.IsValid || !right.PhaseId.IsValid)
            {
                return false;
            }

            return string.Equals(left.PhaseId.Value, right.PhaseId.Value, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveActiveSceneNameOrFail(string reason)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionAdvancePhaseExecutionService),
                    $"[FATAL][H1][SessionTransition] AdvancePhase could not resolve active scene name. reason='{reason}'.");
            }

            return sceneName.Trim();
        }

        private static string DescribePhase(PhaseDefinitionAsset phaseDefinition)
        {
            return phaseDefinition != null && phaseDefinition.PhaseId.IsValid
                ? phaseDefinition.PhaseId.Value
                : "<none>";
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "SessionTransition/AdvancePhase" : reason.Trim();
        }

        private static string NormalizeForLog(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
