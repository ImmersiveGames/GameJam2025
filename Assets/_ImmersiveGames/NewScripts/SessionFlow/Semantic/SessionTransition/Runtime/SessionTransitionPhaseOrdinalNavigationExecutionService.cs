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
    public interface ISessionTransitionPhaseOrdinalNavigationExecutionService
    {
        PhaseNavigationResult Navigate(SessionTransitionPlan plan, CancellationToken ct = default);
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
        private readonly ISessionActivityPhaseChangeCascadeService _cascadeService;

        public SessionTransitionPhaseOrdinalNavigationExecutionService(
            IRestartContextService restartContextService,
            IPhaseCatalogNavigationService phaseCatalogNavigationService,
            ISessionActivityPhaseChangeCascadeService cascadeService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _phaseCatalogNavigationService = phaseCatalogNavigationService ?? throw new ArgumentNullException(nameof(phaseCatalogNavigationService));
            _cascadeService = cascadeService ?? throw new ArgumentNullException(nameof(cascadeService));
        }

        public PhaseNavigationResult Navigate(SessionTransitionPlan plan, CancellationToken ct = default)
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
            PhaseCatalogNavigationPlan navigationPlan = ResolveNavigationPlanOrFail(plan.Context.OrdinalNavigationKind, targetPhaseId, reason);
            PhaseDefinitionAsset targetPhaseRef = navigationPlan.TargetPhaseRef;
            SessionActivityPhaseChangeCascadeResolution resolution = _cascadeService.BeginPhaseChangeCascade(
                operation: "PhaseOrdinalNavigation",
                navigationKind: plan.Context.OrdinalNavigationKind,
                direction: ResolveDirection(plan.Context.OrdinalNavigationKind),
                targetPhaseRef: targetPhaseRef,
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

            return BuildDeferredResult(plan.Context.OrdinalNavigationKind, targetPhaseId, reason, currentSnapshot, _phaseCatalogNavigationService.TraversalMode);
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

        private static PhaseNavigationResult BuildDeferredResult(
            PhaseOrdinalNavigationKind kind,
            string targetPhaseId,
            string reason,
            GameplayStartSnapshot currentSnapshot,
            PhaseCatalogTraversalMode traversalMode)
        {
            PhaseNavigationRequest request = BuildRequest(kind, targetPhaseId, reason);
            return new PhaseNavigationResult(
                request,
                PhaseNavigationOutcome.Deferred,
                currentSnapshot.PhaseDefinitionRef,
                string.Empty,
                traversalMode,
                wasWrapped: false,
                default);
        }

        private PhaseCatalogNavigationPlan ResolveNavigationPlanOrFail(
            PhaseOrdinalNavigationKind kind,
            string targetPhaseId,
            string reason)
        {
            return kind switch
            {
                PhaseOrdinalNavigationKind.Next => _phaseCatalogNavigationService.ResolveNext(reason),
                PhaseOrdinalNavigationKind.Previous => _phaseCatalogNavigationService.ResolvePrevious(reason),
                PhaseOrdinalNavigationKind.SpecificPhase => _phaseCatalogNavigationService.ResolveSpecificPhase(targetPhaseId, reason),
                PhaseOrdinalNavigationKind.FirstPhase => _phaseCatalogNavigationService.ResolveFirstPhase(reason),
                _ => throw new InvalidOperationException($"[FATAL][H1][SessionTransition] PhaseOrdinalNavigationKind invalido. kind='{kind}' reason='{reason}'."),
            };
        }

        private static string DescribePhase(PhaseDefinitionAsset phase)
            => phase != null && phase.PhaseId.IsValid ? phase.PhaseId.Value : "<none>";

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
