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
    public interface ISessionTransitionPhaseOrdinalNavigationExecutionService
    {
        Task<PhaseNavigationResult> NavigateAsync(SessionTransitionPlan plan, CancellationToken ct = default);
    }

    /// <summary>
    /// Executa a aplicação local da navegação ordinal já resolvida/commitada pelo PhaseCatalog.
    /// O Orchestrator continua sendo o único publisher de SessionTransitionPhaseLocalEntryReadyEvent.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionTransitionPhaseOrdinalNavigationExecutionService : ISessionTransitionPhaseOrdinalNavigationExecutionService
    {
        private const string Source = PhaseFlowSignalVocabulary.SessionTransitionPhaseOrdinalNavigationSource;

        private readonly IRestartContextService _restartContextService;
        private readonly IPhaseCatalogNavigationService _phaseCatalogNavigationService;
        private readonly GameplayPhaseFlowService _gameplayPhaseFlowService;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;

        public SessionTransitionPhaseOrdinalNavigationExecutionService(
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

        public async Task<PhaseNavigationResult> NavigateAsync(SessionTransitionPlan plan, CancellationToken ct = default)
        {
            if (!plan.IsValid || plan.Execution.Kind != SessionTransitionExecutionKind.PhaseOrdinalNavigation || plan.IntentKind != SessionTransitionIntentKind.PhaseOrdinalNavigation)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPhaseOrdinalNavigationExecutionService),
                    $"[FATAL][H1][SessionTransition] PhaseOrdinalNavigation execution recebeu plano invalido. intent='{plan.IntentKind}' executionKind='{plan.Execution.Kind}' reason='{Normalize(plan.Reason)}'.");
            }

            string reason = Normalize(plan.Reason);
            string targetPhaseId = Normalize(plan.Context.OrdinalNavigationTargetPhaseId);
            if (string.IsNullOrWhiteSpace(targetPhaseId))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPhaseOrdinalNavigationExecutionService),
                    $"[FATAL][H1][SessionTransition] PhaseOrdinalNavigation execution sem targetPhaseId. kind='{plan.Context.OrdinalNavigationKind}' reason='{reason}'.");
            }

            GameplayStartSnapshot currentSnapshot = ResolveCurrentSnapshotOrFail(reason);
            PhaseDefinitionAsset targetPhaseRef = _phaseCatalogNavigationService.Catalog.ResolveSpecificPhaseOrFail(targetPhaseId);
            ValidateCommittedTargetOrFail(targetPhaseRef, plan, reason);

            PhaseNavigationSelectionContext selectionContext = await ApplyTargetPhaseAsync(
                targetPhaseRef,
                currentSnapshot,
                plan.Context.OrdinalNavigationKind,
                ResolveDirection(plan.Context.OrdinalNavigationKind),
                reason,
                ct);

            DebugUtility.Log<SessionTransitionPhaseOrdinalNavigationExecutionService>(
                $"[OBS][GameplaySessionFlow][SessionTransition] phase_ordinal_navigation_content_apply_completed source='{Source}' kind='{plan.Context.OrdinalNavigationKind}' fromPhase='{DescribePhase(currentSnapshot.PhaseDefinitionRef)}' toPhase='{DescribePhase(targetPhaseRef)}' reason='{reason}'.",
                DebugUtility.Colors.Success);

            return new PhaseNavigationResult(
                BuildRequest(plan.Context.OrdinalNavigationKind, targetPhaseId, reason),
                PhaseNavigationOutcome.Changed,
                currentSnapshot.PhaseDefinitionRef,
                string.Empty,
                _phaseCatalogNavigationService.TraversalMode,
                wasWrapped: false,
                selectionContext);
        }

        private async Task<PhaseNavigationSelectionContext> ApplyTargetPhaseAsync(
            PhaseDefinitionAsset targetPhaseRef,
            GameplayStartSnapshot currentSnapshot,
            PhaseOrdinalNavigationKind kind,
            PhaseNavigationDirection direction,
            string reason,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

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

            DebugUtility.Log<SessionTransitionPhaseOrdinalNavigationExecutionService>(
                $"[OBS][GameplaySessionFlow][SessionTransition] phase_ordinal_navigation_content_apply_started source='{Source}' kind='{kind}' fromPhase='{DescribePhase(currentSnapshot.PhaseDefinitionRef)}' toPhase='{DescribePhase(targetPhaseRef)}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] reason='{reason}'.",
                DebugUtility.Colors.Info);

            await _sceneCompositionExecutor.ApplyAsync(applyRequest);

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                targetPhaseRef,
                applyRequest.ScenesToLoad,
                applyRequest.ActiveScene,
                Source);

            return new PhaseNavigationSelectionContext(
                currentSnapshot,
                targetPhaseRef,
                phaseSelectedEvent,
                reason,
                ResolveTargetSceneName(applyRequest),
                direction,
                forceFullReload: false);
        }

        private GameplayStartSnapshot ResolveCurrentSnapshotOrFail(string reason)
        {
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) || !snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPhaseOrdinalNavigationExecutionService),
                    $"[FATAL][H1][SessionTransition] PhaseOrdinalNavigation requer GameplayStartSnapshot valido. reason='{reason}'.");
            }

            return snapshot;
        }

        private void ValidateCommittedTargetOrFail(PhaseDefinitionAsset targetPhaseRef, SessionTransitionPlan plan, string reason)
        {
            PhaseDefinitionAsset committed = _phaseCatalogNavigationService.CurrentCommitted;
            if (committed == null || targetPhaseRef == null ||
                !committed.PhaseId.IsValid || !targetPhaseRef.PhaseId.IsValid ||
                !string.Equals(committed.PhaseId.Value, targetPhaseRef.PhaseId.Value, StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionPhaseOrdinalNavigationExecutionService),
                    $"[FATAL][H1][SessionTransition] PhaseOrdinalNavigation execution requer target ordinal ja commitado pelo PhaseCatalog. committed='{DescribePhase(committed)}' target='{DescribePhase(targetPhaseRef)}' kind='{plan.Context.OrdinalNavigationKind}' reason='{reason}'.");
            }
        }

        private static PhaseNavigationRequest BuildRequest(PhaseOrdinalNavigationKind kind, string targetPhaseId, string reason)
        {
            return kind switch
            {
                PhaseOrdinalNavigationKind.Next => PhaseNavigationRequest.Next(reason),
                PhaseOrdinalNavigationKind.Previous => PhaseNavigationRequest.Previous(reason),
                PhaseOrdinalNavigationKind.SpecificPhase => PhaseNavigationRequest.Specific(targetPhaseId, reason),
                PhaseOrdinalNavigationKind.FirstPhase => PhaseNavigationRequest.FirstPhase(targetPhaseId, reason),
                _ => throw new InvalidOperationException($"[FATAL][H1][SessionTransition] PhaseOrdinalNavigationKind invalido. kind='{kind}' reason='{reason}'."),
            };
        }

        private static PhaseNavigationDirection ResolveDirection(PhaseOrdinalNavigationKind kind)
        {
            return kind switch
            {
                PhaseOrdinalNavigationKind.Next => PhaseNavigationDirection.Next,
                PhaseOrdinalNavigationKind.Previous => PhaseNavigationDirection.Previous,
                PhaseOrdinalNavigationKind.SpecificPhase => PhaseNavigationDirection.Specific,
                PhaseOrdinalNavigationKind.FirstPhase => PhaseNavigationDirection.Specific,
                _ => PhaseNavigationDirection.Specific,
            };
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
