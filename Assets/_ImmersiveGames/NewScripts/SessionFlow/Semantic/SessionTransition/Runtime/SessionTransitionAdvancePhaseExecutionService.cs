using System;
using System.Threading;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public interface ISessionTransitionAdvancePhaseExecutionService
    {
        PhaseNavigationResult Advance(SessionTransitionPlan plan, CancellationToken ct = default);
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
        private readonly ISessionActivityPhaseChangeCascadeService _cascadeService;

        public SessionTransitionAdvancePhaseExecutionService(
            IRestartContextService restartContextService,
            IPhaseCatalogNavigationService phaseCatalogNavigationService,
            ISessionActivityPhaseChangeCascadeService cascadeService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _phaseCatalogNavigationService = phaseCatalogNavigationService ?? throw new ArgumentNullException(nameof(phaseCatalogNavigationService));
            _cascadeService = cascadeService ?? throw new ArgumentNullException(nameof(cascadeService));
        }

        public PhaseNavigationResult Advance(SessionTransitionPlan plan, CancellationToken ct = default)
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

            SessionActivityPhaseChangeCascadeResolution resolution = _cascadeService.BeginPhaseChangeCascade(
                operation: "AdvancePhase",
                navigationKind: PhaseOrdinalNavigationKind.Next,
                direction: PhaseNavigationDirection.Next,
                targetPhaseRef: navigationPlan.TargetPhaseRef,
                navigationPlan: navigationPlan,
                reason: reason,
                source: Source,
                plan: plan,
                ct: ct);

            if (resolution.ResultPresentationDisposition == SessionActivityPhaseChangeCascadeStageDisposition.Execute)
            {
                _cascadeService.OpenPhaseChangeCascadeResultPresentation(
                    resolution,
                    navigationPlan,
                    plan,
                    Source);
            }

            return BuildDeferredResult(navigationPlan);
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

        private static PhaseNavigationResult BuildDeferredResult(PhaseCatalogNavigationPlan navigationPlan)
        {
            return new PhaseNavigationResult(
                navigationPlan.Request,
                PhaseNavigationOutcome.Deferred,
                navigationPlan.CurrentCommitted,
                navigationPlan.CatalogName,
                navigationPlan.TraversalMode,
                navigationPlan.WasWrapped,
                default);
        }

        private static string DescribePhase(PhaseDefinitionAsset phase)
            => phase != null && phase.PhaseId.IsValid ? phase.PhaseId.Value : "<none>";

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
