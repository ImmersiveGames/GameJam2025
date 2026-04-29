using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
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
        private const string Source = PhaseFlowSignalVocabulary.SessionTransitionPhaseOrdinalNavigationSource;

        private readonly IRestartContextService _restartContextService;
        private readonly IPhaseCatalogNavigationService _phaseCatalogNavigationService;
        private readonly GameplayPhaseFlowService _gameplayPhaseFlowService;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;
        private readonly ISceneFlowRouteActorSetRefContext _routeActorSetContext;
        private readonly IGameplayPhaseRuntimeService _phaseRuntimeService;
        private readonly IGameplayParticipationFlowService _participationFlowService;

        public PhaseOrdinalNavigationRequestService(
            IRestartContextService restartContextService,
            IPhaseCatalogNavigationService phaseCatalogNavigationService,
            GameplayPhaseFlowService gameplayPhaseFlowService,
            ISceneCompositionExecutor sceneCompositionExecutor,
            ISceneFlowRouteActorSetRefContext routeActorSetContext,
            IGameplayPhaseRuntimeService phaseRuntimeService,
            IGameplayParticipationFlowService participationFlowService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _phaseCatalogNavigationService = phaseCatalogNavigationService ?? throw new ArgumentNullException(nameof(phaseCatalogNavigationService));
            _gameplayPhaseFlowService = gameplayPhaseFlowService ?? throw new ArgumentNullException(nameof(gameplayPhaseFlowService));
            _sceneCompositionExecutor = sceneCompositionExecutor ?? throw new ArgumentNullException(nameof(sceneCompositionExecutor));
            _routeActorSetContext = routeActorSetContext ?? throw new ArgumentNullException(nameof(routeActorSetContext));
            _phaseRuntimeService = phaseRuntimeService ?? throw new ArgumentNullException(nameof(phaseRuntimeService));
            _participationFlowService = participationFlowService ?? throw new ArgumentNullException(nameof(participationFlowService));
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

            PhaseNavigationSelectionContext selectionContext = await ApplyTargetPhaseAsync(navigationPlan, currentSnapshot, request.Kind, reason, ct);
            _phaseCatalogNavigationService.ClearPendingTarget(reason);

            SessionTransitionPlan sessionTransitionPlan = BuildSessionTransitionPlanOrFail(request, selectionContext, reason);
            PublishPhaseLocalEntryReadyOrFail(sessionTransitionPlan, reason);

            DebugUtility.Log<PhaseOrdinalNavigationRequestService>(
                $"[OBS][QA][PhaseNavigation] ordinal_navigation_completed kind='{request.Kind}' fromPhase='{DescribePhase(navigationPlan.CurrentCommitted)}' toPhase='{DescribePhase(navigationPlan.TargetPhaseRef)}' reason='{reason}'.",
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

        private async Task<PhaseNavigationSelectionContext> ApplyTargetPhaseAsync(
            PhaseCatalogNavigationPlan navigationPlan,
            GameplayStartSnapshot currentSnapshot,
            PhaseOrdinalNavigationKind kind,
            string reason,
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

            DebugUtility.Log<PhaseOrdinalNavigationRequestService>(
                $"[OBS][QA][PhaseNavigation] ordinal_navigation_content_apply_started kind='{kind}' fromPhase='{DescribePhase(navigationPlan.CurrentCommitted)}' toPhase='{DescribePhase(targetPhaseRef)}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] reason='{reason}'.",
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
                navigationPlan.Direction,
                forceFullReload: false);
        }

        private SessionTransitionPlan BuildSessionTransitionPlanOrFail(
            PhaseOrdinalNavigationRequest request,
            PhaseNavigationSelectionContext selectionContext,
            string reason)
        {
            string sceneName = ResolveActiveSceneNameOrFail(reason);
            string contextSignature = $"qa-phase-ordinal|kind:{request.Kind}|from:{DescribePhase(selectionContext.CurrentPhaseRef)}|to:{DescribePhase(selectionContext.TargetPhaseRef)}|selection:{selectionContext.PhaseSelectedEvent.SelectionSignature}|reason:{reason}";
            SessionTransitionContext context = SessionTransitionContext.CreatePhaseOrdinalNavigation(
                contextSignature,
                sceneName,
                profile: "QA/PhaseNavigation",
                reason: reason,
                nextState: "PhaseOrdinalNavigation",
                ordinalNavigationKind: request.Kind,
                targetPhaseId: selectionContext.TargetPhaseRef.PhaseId.Value);

            SessionTransitionComposition composition = new SessionTransitionComposition(
                new SessionTransitionAxisMap(
                    SessionTransitionIntentKind.PhaseOrdinalNavigation,
                    RunContinuationKind.Unknown,
                    SessionTransitionPhaseAction.OrdinalNavigation,
                    SessionTransitionResetAction.None,
                    reconstruction: false,
                    contentSpawn: true,
                    carryOver: true),
                new SessionTransitionContinuityShape(
                    SessionTransitionPreservationMask.SessionState |
                    SessionTransitionPreservationMask.WorldState |
                    SessionTransitionPreservationMask.ContentState |
                    SessionTransitionPreservationMask.ActorState |
                    SessionTransitionPreservationMask.ObjectState,
                    SessionTransitionResetScopeKind.None,
                    SessionTransitionCarryOverKind.Selective),
                new SessionTransitionReconstructionShape(
                    SessionTransitionReconstructionKind.None,
                    SessionTransitionResetScopeKind.None),
                emitsPhaseLocalEntryReady: true,
                new[]
                {
                    SessionTransitionAxisId.Continuity,
                    SessionTransitionAxisId.PhaseTransition,
                    SessionTransitionAxisId.ContentSpawn,
                    SessionTransitionAxisId.CarryOver,
                });

            SessionTransitionExecution execution = new SessionTransitionExecution(
                SessionTransitionExecutionKind.PhaseOrdinalNavigation,
                SessionTransitionHandoffAction.None);

            return new SessionTransitionPlan(context, composition, execution);
        }

        private void PublishPhaseLocalEntryReadyOrFail(SessionTransitionPlan plan, string reason)
        {
            if (!plan.IsValid || !plan.EmitsPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] Plano ordinal invalido para PhaseLocalEntryReady. intent='{plan.IntentKind}' reason='{reason}'.");
            }

            PhaseLocalEntryReadyRuntimePayload payload = ResolvePhaseLocalEntryReadyRuntimePayloadOrFail(reason);
            GameplaySessionContextSnapshot sessionContext = payload.PhaseRuntime.SessionContext;
            string sceneName = ResolveActiveSceneNameOrFail(reason);
            string cycleSignature = $"phase-local-entry-ready|context:{plan.ContextSignature}|routeId:{sessionContext.MacroRouteId}|routeKind:{sessionContext.MacroRouteRef.RouteKind}|scene:{sceneName}|reason:{reason}|session:{payload.PhaseRuntime.SessionContext.SessionSignature}|phase:{payload.PhaseRuntime.PhaseRuntimeSignature}|participation:{payload.ParticipationSnapshot.Signature.Value}|actorSetRef:{payload.ActorSetRef.Value}|intent:{plan.IntentKind}|ordinal:{plan.Context.OrdinalNavigationKind}|execution:{plan.Execution}";

            var evt = new SessionTransitionPhaseLocalEntryReadyEvent(
                plan,
                Source,
                sessionContext.MacroRouteId,
                sessionContext.MacroRouteRef.RouteKind,
                sceneName,
                reason,
                payload.PhaseRuntime.SessionContext.SessionSignature,
                payload.PhaseRuntime.PhaseRuntimeSignature,
                payload.ParticipationSnapshot.Signature.Value,
                payload.ActorSetRef.Value,
                cycleSignature);

            if (!evt.HasCanonicalPayload)
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] PhaseLocalEntryReady ordinal construido sem payload canonico. kind='{plan.Context.OrdinalNavigationKind}' reason='{reason}'.");
            }

            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Raise(evt);

            DebugUtility.Log<PhaseOrdinalNavigationRequestService>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PhaseLocalEntryReadyPublished source='{Source}' intent='{evt.Plan.IntentKind}' ordinalKind='{evt.Plan.Context.OrdinalNavigationKind}' executionKind='{evt.Plan.Execution.Kind}' routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' scene='{evt.SceneName}' actorSetRef='{evt.ActorSetRef}' sessionSignature='{evt.SessionSignature}' phaseSignature='{evt.PhaseSignature}' participationSignature='{evt.ParticipationSignature}' cycleSignature='{evt.CycleSignature}' reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private PhaseLocalEntryReadyRuntimePayload ResolvePhaseLocalEntryReadyRuntimePayloadOrFail(string reason)
        {
            if (!_routeActorSetContext.TryGetCurrent(out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out string routeSource) ||
                routeKind != SceneRouteKind.Gameplay ||
                !actorSetRef.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] ActorSetRef canonico ausente para PhaseLocalEntryReady ordinal. routeKind='{routeKind}' routeSource='{Normalize(routeSource)}' reason='{reason}'.");
            }

            if (!_phaseRuntimeService.TryGetCurrent(out GameplayPhaseRuntimeSnapshot phaseRuntime) ||
                !phaseRuntime.IsValid ||
                phaseRuntime.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] GameplayPhaseRuntimeSnapshot invalido para PhaseLocalEntryReady ordinal. reason='{reason}'.");
            }

            ParticipationReadinessSnapshot readiness = ParticipationReadinessSnapshot.Empty;
            bool hasReadiness = _participationFlowService.TryGetCurrentReadiness(out readiness);
            if (!_participationFlowService.TryGetCurrent(out ParticipationSnapshot participationSnapshot) ||
                !hasReadiness ||
                !participationSnapshot.IsValid ||
                !readiness.IsValid ||
                !readiness.CanEnterGameplay)
            {
                HardFailFastH1.Trigger(typeof(PhaseOrdinalNavigationRequestService),
                    $"[FATAL][H1][QA][PhaseNavigation] Participation snapshot invalida para PhaseLocalEntryReady ordinal. readinessState='{readiness.State}' canEnterGameplay='{readiness.CanEnterGameplay}' reason='{reason}'.");
            }

            return new PhaseLocalEntryReadyRuntimePayload(actorSetRef, phaseRuntime, participationSnapshot);
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
                    $"[FATAL][H1][QA][PhaseNavigation] ActiveScene vazio para PhaseLocalEntryReady ordinal. reason='{reason}'.");
            }

            return sceneName.Trim();
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

        private readonly struct PhaseLocalEntryReadyRuntimePayload
        {
            public PhaseLocalEntryReadyRuntimePayload(
                ActorSetRef actorSetRef,
                GameplayPhaseRuntimeSnapshot phaseRuntime,
                ParticipationSnapshot participationSnapshot)
            {
                ActorSetRef = actorSetRef;
                PhaseRuntime = phaseRuntime;
                ParticipationSnapshot = participationSnapshot;
            }

            public ActorSetRef ActorSetRef { get; }
            public GameplayPhaseRuntimeSnapshot PhaseRuntime { get; }
            public ParticipationSnapshot ParticipationSnapshot { get; }
        }
    }
}
