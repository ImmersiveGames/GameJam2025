using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation
{
    public enum PhaseOrdinalNavigationKind
    {
        Unknown = 0,
        Next = 1,
        Previous = 2,
        SpecificPhase = 3,
        FirstPhase = 4,
    }

    public readonly struct PhaseOrdinalNavigationRequest
    {
        public PhaseOrdinalNavigationRequest(PhaseOrdinalNavigationKind kind, string targetPhaseId, string reason)
        {
            Kind = kind;
            TargetPhaseId = string.IsNullOrWhiteSpace(targetPhaseId) ? string.Empty : targetPhaseId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public PhaseOrdinalNavigationKind Kind { get; }
        public string TargetPhaseId { get; }
        public string Reason { get; }
        public bool HasTargetPhaseId => !string.IsNullOrWhiteSpace(TargetPhaseId);
    }

    public interface IPhaseOrdinalNavigationRequestService
    {
        Task<PhaseNavigationResult> NavigateAsync(PhaseOrdinalNavigationRequest request, CancellationToken ct = default);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseOrdinalNavigationRequestService : IPhaseOrdinalNavigationRequestService
    {
        private readonly IRestartContextService _restartContextService;
        private readonly IPhaseCatalogNavigationService _phaseCatalogNavigationService;
        private readonly SessionTransitionOrchestrator _sessionTransitionOrchestrator;

        public PhaseOrdinalNavigationRequestService(
            IRestartContextService restartContextService,
            IPhaseCatalogNavigationService phaseCatalogNavigationService,
            SessionTransitionOrchestrator sessionTransitionOrchestrator)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _phaseCatalogNavigationService = phaseCatalogNavigationService ?? throw new ArgumentNullException(nameof(phaseCatalogNavigationService));
            _sessionTransitionOrchestrator = sessionTransitionOrchestrator ?? throw new ArgumentNullException(nameof(sessionTransitionOrchestrator));
        }

        public async Task<PhaseNavigationResult> NavigateAsync(PhaseOrdinalNavigationRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ValidateRequestOrFail(request);

            string reason = Normalize(request.Reason);
            GameplayStartSnapshot currentSnapshot = ResolveCurrentSnapshotOrFail(reason);
            PhaseCatalogNavigationPlan navigationPlan = ResolvePlan(request, reason);
            if (navigationPlan.Outcome != PhaseNavigationOutcome.Changed)
            {
                return CreateBlockedResult(navigationPlan);
            }

            ValidateCurrentSnapshotMatchesCatalogOrFail(currentSnapshot, navigationPlan, reason);
            _phaseCatalogNavigationService.Commit(navigationPlan);

            SessionTransitionContext context = BuildSessionTransitionContextOrFail(request, navigationPlan, reason);
            await _sessionTransitionOrchestrator.ExecuteAsync(context, ct);

            _phaseCatalogNavigationService.ClearPendingTarget(reason);

            DebugUtility.Log<PhaseOrdinalNavigationRequestService>(
                $"[OBS][QA][PhaseNavigation] ordinal_navigation_completed kind='{request.Kind}' fromPhase='{DescribePhase(navigationPlan.CurrentCommitted)}' toPhase='{DescribePhase(navigationPlan.TargetPhaseRef)}' routedTo='SessionTransitionOrchestrator' phaseLocalEntryPublisher='SessionTransitionOrchestrator' reason='{reason}'.",
                DebugUtility.Colors.Success);

            return new PhaseNavigationResult(
                navigationPlan.Request,
                PhaseNavigationOutcome.Changed,
                navigationPlan.CurrentCommitted,
                navigationPlan.CatalogName,
                navigationPlan.TraversalMode,
                navigationPlan.WasWrapped,
                default);
        }

        private PhaseCatalogNavigationPlan ResolvePlan(PhaseOrdinalNavigationRequest request, string reason)
        {
            switch (request.Kind)
            {
                case PhaseOrdinalNavigationKind.Next:
                    return _phaseCatalogNavigationService.ResolveNext(reason);
                case PhaseOrdinalNavigationKind.Previous:
                    return _phaseCatalogNavigationService.ResolvePrevious(reason);
                case PhaseOrdinalNavigationKind.SpecificPhase:
                    return _phaseCatalogNavigationService.ResolveSpecificPhase(request.TargetPhaseId, reason);
                case PhaseOrdinalNavigationKind.FirstPhase:
                    return _phaseCatalogNavigationService.ResolveFirstPhase(reason);
                default:
                    HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                        $"[FATAL][H1][QA][PhaseNavigation] PhaseOrdinalNavigationKind invalido. kind='{request.Kind}' reason='{reason}'.");
                    return default;
            }
        }

        private SessionTransitionContext BuildSessionTransitionContextOrFail(
            PhaseOrdinalNavigationRequest request,
            PhaseCatalogNavigationPlan navigationPlan,
            string reason)
        {
            string sceneName = ResolveActiveSceneNameOrFail(reason);
            string targetPhaseId = navigationPlan.TargetPhaseRef != null && navigationPlan.TargetPhaseRef.PhaseId.IsValid
                ? navigationPlan.TargetPhaseRef.PhaseId.Value
                : string.Empty;

            if (string.IsNullOrWhiteSpace(targetPhaseId))
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] PhaseOrdinalNavigation sem targetPhaseId canonico apos commit. kind='{request.Kind}' reason='{reason}'.");
            }

            string contextSignature = $"qa-phase-ordinal|kind:{request.Kind}|from:{DescribePhase(navigationPlan.CurrentCommitted)}|to:{DescribePhase(navigationPlan.TargetPhaseRef)}|reason:{reason}";
            return SessionTransitionContext.CreatePhaseOrdinalNavigation(
                contextSignature,
                sceneName,
                profile: "QA/PhaseNavigation",
                reason: reason,
                nextState: "PhaseOrdinalNavigation",
                ordinalNavigationKind: request.Kind,
                targetPhaseId: targetPhaseId);
        }

        private GameplayStartSnapshot ResolveCurrentSnapshotOrFail(string reason)
        {
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) || !snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] Navegacao ordinal requer GameplayStartSnapshot valido. reason='{reason}'.");
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
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] Current snapshot/catalog mismatch. snapshotPhase='{DescribePhase(currentSnapshot.PhaseDefinitionRef)}' catalogCurrent='{DescribePhase(navigationPlan.CurrentCommitted)}' target='{DescribePhase(navigationPlan.TargetPhaseRef)}' reason='{reason}'.");
            }
        }

        private static void ValidateRequestOrFail(PhaseOrdinalNavigationRequest request)
        {
            if (request.Kind == PhaseOrdinalNavigationKind.Unknown)
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    "[FATAL][H1][QA][PhaseNavigation] PhaseOrdinalNavigationRequest sem kind valido.");
            }

            if (request.Kind == PhaseOrdinalNavigationKind.SpecificPhase && !request.HasTargetPhaseId)
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] SpecificPhase requer targetPhaseId. reason='{Normalize(request.Reason)}'.");
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

        private static string ResolveActiveSceneNameOrFail(string reason)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] ActiveScene vazio para PhaseOrdinalNavigation. reason='{reason}'.");
            }

            return sceneName.Trim();
        }

        private static string DescribePhase(PhaseDefinitionAsset phase)
        {
            if (phase == null)
            {
                return "<null>";
            }

            return phase.PhaseId.IsValid ? phase.PhaseId.Value : phase.name;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
