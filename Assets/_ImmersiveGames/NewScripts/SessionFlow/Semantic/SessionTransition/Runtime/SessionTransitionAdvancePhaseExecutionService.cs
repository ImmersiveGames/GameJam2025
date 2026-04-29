using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public interface ISessionTransitionAdvancePhaseExecutionService
    {
        Task<PhaseNavigationResult> AdvanceAsync(SessionTransitionPlan plan, CancellationToken ct = default);
    }

    /// <summary>
    /// Executa AdvancePhase sem passar pelo PhaseNextPhaseService legado.
    /// O orquestrador de SessionTransition continua sendo o dono da publicação de PhaseLocalEntryReady.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionTransitionAdvancePhaseExecutionService : ISessionTransitionAdvancePhaseExecutionService
    {
        private const string Source = PhaseFlowSignalVocabulary.SessionTransitionAdvancePhaseSource;

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
            if (!plan.IsValid || plan.Execution.Kind != SessionTransitionExecutionKind.NextPhase)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionAdvancePhaseExecutionService),
                    $"[FATAL][H1][SessionTransition] AdvancePhase execution recebeu plano invalido. intent='{plan.IntentKind}' executionKind='{plan.Execution.Kind}' reason='{Normalize(plan.Reason)}'.");
            }

            string reason = Normalize(plan.Reason);
            GameplayStartSnapshot currentSnapshot = ResolveCurrentSnapshotOrFail(reason);
            PhaseCatalogNavigationPlan navigationPlan = _phaseCatalogNavigationService.ResolveNext(reason);
            if (navigationPlan.Outcome != PhaseNavigationOutcome.Changed)
            {
                return CreateBlockedResult(navigationPlan);
            }

            ValidateCurrentSnapshotMatchesCatalogOrFail(currentSnapshot, navigationPlan, reason);
            _phaseCatalogNavigationService.Commit(navigationPlan);

            PhaseNavigationSelectionContext selectionContext = await ApplyTargetPhaseAsync(
                navigationPlan,
                currentSnapshot,
                reason,
                Source,
                ct);

            _phaseCatalogNavigationService.ClearPendingTarget(reason);

            DebugUtility.Log<SessionTransitionAdvancePhaseExecutionService>(
                $"[OBS][GameplaySessionFlow][SessionTransition] advance_phase_content_apply_completed source='{Source}' fromPhase='{DescribePhase(navigationPlan.CurrentCommitted)}' toPhase='{DescribePhase(navigationPlan.TargetPhaseRef)}' reason='{reason}'.",
                DebugUtility.Colors.Success);

            return new PhaseNavigationResult(
                navigationPlan.Request,
                PhaseNavigationOutcome.Changed,
                navigationPlan.CurrentCommitted,
                navigationPlan.CatalogName,
                navigationPlan.TraversalMode,
                navigationPlan.WasWrapped,
                selectionContext);
        }

        private async Task<PhaseNavigationSelectionContext> ApplyTargetPhaseAsync(
            PhaseCatalogNavigationPlan navigationPlan,
            GameplayStartSnapshot currentSnapshot,
            string reason,
            string source,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            PhaseDefinitionAsset targetPhaseRef = navigationPlan.TargetPhaseRef;
            PhaseDefinitionSelectedEvent phaseSelectedEvent = _gameplayPhaseFlowService.PublishPhaseDefinitionSelected(
                targetPhaseRef,
                currentSnapshot.MacroRouteId,
                currentSnapshot.MacroRouteRef,
                reason);

            SceneCompositionRequest applyRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                targetPhaseRef,
                reason,
                phaseSelectedEvent.SelectionSignature,
                forceFullReload: false);

            DebugUtility.Log<SessionTransitionAdvancePhaseExecutionService>(
                $"[OBS][GameplaySessionFlow][SessionTransition] advance_phase_content_apply_started source='{source}' fromPhase='{DescribePhase(navigationPlan.CurrentCommitted)}' toPhase='{DescribePhase(targetPhaseRef)}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] reason='{reason}'.",
                DebugUtility.Colors.Info);

            await _sceneCompositionExecutor.ApplyAsync(applyRequest);

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                targetPhaseRef,
                applyRequest.ScenesToLoad,
                applyRequest.ActiveScene,
                source);

            return new PhaseNavigationSelectionContext(
                currentSnapshot,
                targetPhaseRef,
                phaseSelectedEvent,
                reason,
                ResolveTargetSceneName(applyRequest),
                navigationPlan.Direction,
                forceFullReload: false);
        }

        private GameplayStartSnapshot ResolveCurrentSnapshotOrFail(string reason)
        {
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) || !snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionAdvancePhaseExecutionService),
                    $"[FATAL][H1][SessionTransition] AdvancePhase requer GameplayStartSnapshot valido. reason='{reason}'.");
            }

            return snapshot;
        }

        private static void ValidateCurrentSnapshotMatchesCatalogOrFail(
            GameplayStartSnapshot currentSnapshot,
            PhaseCatalogNavigationPlan navigationPlan,
            string reason)
        {
            if (currentSnapshot.PhaseDefinitionRef == null ||
                navigationPlan.CurrentCommitted == null ||
                !string.Equals(currentSnapshot.PhaseDefinitionRef.PhaseId.Value, navigationPlan.CurrentCommitted.PhaseId.Value, StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionAdvancePhaseExecutionService),
                    $"[FATAL][H1][SessionTransition] AdvancePhase current snapshot/catalog mismatch. snapshotPhase='{DescribePhase(currentSnapshot.PhaseDefinitionRef)}' catalogCurrent='{DescribePhase(navigationPlan.CurrentCommitted)}' target='{DescribePhase(navigationPlan.TargetPhaseRef)}' reason='{reason}'.");
            }
        }

        private static PhaseNavigationResult CreateBlockedResult(PhaseCatalogNavigationPlan navigationPlan)
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

        private static string ResolveTargetSceneName(SceneCompositionRequest applyRequest)
        {
            if (applyRequest.ScenesToLoad != null && applyRequest.ScenesToLoad.Count > 0)
            {
                return applyRequest.ScenesToLoad[0];
            }

            return applyRequest.ActiveScene;
        }

        private static string DescribePhase(PhaseDefinitionAsset phase)
            => phase != null && phase.PhaseId.IsValid ? phase.PhaseId.Value : "<none>";

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
