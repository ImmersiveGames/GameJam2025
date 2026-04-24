using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public interface ISessionTransitionExecutionPort
    {
        Task<bool> DispatchAsync(SessionTransitionPlan plan, CancellationToken ct = default);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionTransitionExecutionPort : ISessionTransitionExecutionPort
    {
        private readonly IGameplaySessionFlowContinuityService _continuityService;

        public SessionTransitionExecutionPort(IGameplaySessionFlowContinuityService continuityService)
        {
            _continuityService = continuityService ?? throw new ArgumentNullException(nameof(continuityService));
        }

        public async Task<bool> DispatchAsync(SessionTransitionPlan plan, CancellationToken ct = default)
        {
            string normalizedReason = Normalize(plan.Reason);

            if (plan.Execution.Kind == SessionTransitionExecutionKind.ResetCurrentPhase)
            {
                await _continuityService.ResetCurrentPhaseAsync(normalizedReason, ct);
                return true;
            }

            if (plan.Execution.Kind == SessionTransitionExecutionKind.NextPhase)
            {
                await _continuityService.NextPhaseAsync(normalizedReason, ct);
                return true;
            }

            if (plan.Execution.Kind == SessionTransitionExecutionKind.ExitToMenu)
            {
                await _continuityService.ExitToMenuAsync(normalizedReason, ct);
                return true;
            }

            return false;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionTransitionOrchestrator
    {
        private readonly SessionTransitionPlanResolver _planResolver;
        private readonly ISessionTransitionExecutionPort _executionPort;

        public SessionTransitionOrchestrator(SessionTransitionPlanResolver planResolver, ISessionTransitionExecutionPort executionPort)
        {
            _planResolver = planResolver ?? throw new ArgumentNullException(nameof(planResolver));
            _executionPort = executionPort ?? throw new ArgumentNullException(nameof(executionPort));
        }

        public async Task ExecuteAsync(SceneTransitionContext context, CancellationToken ct = default)
        {
            if (!context.RouteId.IsValid || context.RouteRef == null || context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] SceneTransitionContext invalido recebido pelo orquestrador gameplay prepare.");
            }

            string normalizedReason = Normalize(context.Reason);
            string signature = SceneTransitionSignature.Compute(context);
            SessionTransitionPlan plan = _planResolver.Resolve(context);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PlanResolved source='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' phaseLocalEntryReady='{plan.EmitsPhaseLocalEntryReady}' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteStarted source='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' phaseLocalEntryReady='{plan.EmitsPhaseLocalEntryReady}' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            if (!plan.EmitsPhaseLocalEntryReady)
            {
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] SkipNoContent source='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' continuation='{plan.ResolvedContinuation}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return;
            }

            await ExecuteGameplayPrepareRailAsync(context, plan, ct);

            SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent =
                BuildGameplayPreparePhaseLocalEntryReadyEventOrFail(context, plan);
            if (!phaseLocalEntryReadyEvent.HasCanonicalPayload)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] SessionTransitionPhaseLocalEntryReadyEvent construido sem payload canonico para gameplay prepare.");
            }

            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Raise(phaseLocalEntryReadyEvent);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PhaseLocalEntryReadyPublished source='GameplaySessionPrepare' routeId='{phaseLocalEntryReadyEvent.RouteId}' routeKind='{phaseLocalEntryReadyEvent.RouteKind}' scene='{phaseLocalEntryReadyEvent.SceneName}' actorSetRef='{phaseLocalEntryReadyEvent.ActorSetRef}' sessionSignature='{phaseLocalEntryReadyEvent.SessionSignature}' phaseSignature='{phaseLocalEntryReadyEvent.PhaseSignature}' participationSignature='{phaseLocalEntryReadyEvent.ParticipationSignature}' cycleSignature='{phaseLocalEntryReadyEvent.CycleSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted source='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' continuation='{plan.ResolvedContinuation}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        public async Task ExecuteAsync(SessionTransitionPlan plan, CancellationToken ct = default)
        {
            if (!plan.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] SessionTransitionPlan invalido recebido pelo orquestrador.");
            }

            string normalizedReason = Normalize(plan.Reason);
            bool shouldEmitPhaseLocalEntryReady = plan.EmitsPhaseLocalEntryReady;

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteStarted continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' phaseLocalEntryReady='{shouldEmitPhaseLocalEntryReady}' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            bool executed = await _executionPort.DispatchAsync(plan, ct);
            if (executed)
            {
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] SkipNoContent continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' phaseLocalEntryReady='{shouldEmitPhaseLocalEntryReady}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Warning);
        }

        private static async Task ExecuteGameplayPrepareRailAsync(
            SceneTransitionContext context,
            SessionTransitionPlan plan,
            CancellationToken ct)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionSelectionService>(out var phaseSelectionService) || phaseSelectionService == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] IPhaseDefinitionSelectionService missing for gameplay prepare orchestration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<GameplayPhaseFlowService>(out var gameplayPhaseFlowService) || gameplayPhaseFlowService == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] GameplayPhaseFlowService missing for gameplay prepare orchestration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneCompositionExecutor>(out var sceneCompositionExecutor) || sceneCompositionExecutor == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] ISceneCompositionExecutor missing for gameplay prepare orchestration.");
            }

            string reason = Normalize(context.Reason);
            string signature = SceneTransitionSignature.Compute(context);

            PhaseDefinitionAsset selectedPhaseDefinitionRef = phaseSelectionService.ResolveOrFail();
            PhaseDefinitionSelectedEvent phaseSelectedEvent = gameplayPhaseFlowService.PublishPhaseDefinitionSelected(
                selectedPhaseDefinitionRef,
                context.RouteId,
                context.RouteRef,
                reason);

            SceneCompositionRequest phaseCompositionRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                selectedPhaseDefinitionRef,
                reason,
                phaseSelectedEvent.SelectionSignature,
                forceFullReload: false);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] OrchestrationStarted source='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' continuation='{plan.ResolvedContinuation}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await sceneCompositionExecutor.ApplyAsync(phaseCompositionRequest);

            const string canonicalPhaseContentAppliedSource = PhaseFlowSignalVocabulary.GameplaySessionFlowSource;
            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                selectedPhaseDefinitionRef,
                phaseCompositionRequest.ScenesToLoad,
                phaseCompositionRequest.ActiveScene,
                canonicalPhaseContentAppliedSource);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] OrchestrationCompleted source='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private static SessionTransitionPhaseLocalEntryReadyEvent BuildGameplayPreparePhaseLocalEntryReadyEventOrFail(
            SceneTransitionContext context,
            SessionTransitionPlan plan)
        {
            if (!plan.IsValid || !plan.EmitsPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] SessionTransitionPlan invalido ao construir SessionTransitionPhaseLocalEntryReadyEvent para gameplay prepare.");
            }

            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] DependencyManager.Provider indisponivel ao construir SessionTransitionPhaseLocalEntryReadyEvent para gameplay prepare.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out var routeActorSetContext) || routeActorSetContext == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] ISceneFlowRouteActorSetRefContext ausente ao construir SessionTransitionPhaseLocalEntryReadyEvent para gameplay prepare.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRuntimeService>(out var phaseRuntimeService) || phaseRuntimeService == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] IGameplayPhaseRuntimeService ausente ao construir SessionTransitionPhaseLocalEntryReadyEvent para gameplay prepare.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var participationFlowService) || participationFlowService == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] IGameplayParticipationFlowService ausente ao construir SessionTransitionPhaseLocalEntryReadyEvent para gameplay prepare.");
            }

            if (!DependencyManager.Provider.TryGetForScene<IWorldSpawnServiceRegistry>(context.TargetActiveScene, out var spawnRegistry) || spawnRegistry == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] IWorldSpawnServiceRegistry ausente para scene='{context.TargetActiveScene}' ao construir SessionTransitionPhaseLocalEntryReadyEvent.");
            }

            if (!routeActorSetContext.TryGetCurrent(out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out string routeSource) ||
                routeKind != SceneRouteKind.Gameplay ||
                !actorSetRef.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] ActorSetRef canonico ausente para gameplay prepare. routeKind='{routeKind}' source='{Normalize(routeSource)}' contextRouteKind='{context.RouteRef.RouteKind}'.");
            }

            if (!phaseRuntimeService.TryGetCurrent(out GameplayPhaseRuntimeSnapshot phaseRuntime) ||
                !phaseRuntime.IsValid ||
                phaseRuntime.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] GameplayPhaseRuntimeSnapshot invalido ao construir SessionTransitionPhaseLocalEntryReadyEvent para gameplay prepare.");
            }

            ParticipationReadinessSnapshot readiness = ParticipationReadinessSnapshot.Empty;
            bool hasReadiness = participationFlowService.TryGetCurrentReadiness(out readiness);

            if (!participationFlowService.TryGetCurrent(out ParticipationSnapshot participationSnapshot) ||
                !hasReadiness ||
                !participationSnapshot.IsValid ||
                !readiness.IsValid ||
                !readiness.CanEnterGameplay)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] Participation snapshot invalida ao construir SessionTransitionPhaseLocalEntryReadyEvent. readinessState='{readiness.State}' canEnterGameplay='{readiness.CanEnterGameplay}'.");
            }

            if (spawnRegistry.Services == null || spawnRegistry.Services.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] IWorldSpawnServiceRegistry ainda sem servicos registrados para scene='{context.TargetActiveScene}' ao construir SessionTransitionPhaseLocalEntryReadyEvent.");
            }

            string sceneName = ResolveSceneNameOrFail(context);
            string normalizedReason = Normalize(context.Reason);
            string sessionSignature = phaseRuntime.SessionContext.SessionSignature;
            string phaseSignature = phaseRuntime.PhaseRuntimeSignature;
            string participationSignature = participationSnapshot.Signature.Value;
            string cycleSignature = BuildPhaseLocalEntryReadyCycleSignature(
                context,
                plan,
                sceneName,
                sessionSignature,
                phaseSignature,
                participationSignature,
                actorSetRef.Value);

            return new SessionTransitionPhaseLocalEntryReadyEvent(
                plan,
                "GameplaySessionPrepare",
                context.RouteId,
                context.RouteRef.RouteKind,
                sceneName,
                normalizedReason,
                sessionSignature,
                phaseSignature,
                participationSignature,
                actorSetRef.Value,
                cycleSignature);
        }

        private static string BuildPhaseLocalEntryReadyCycleSignature(
            SceneTransitionContext context,
            SessionTransitionPlan plan,
            string sceneName,
            string sessionSignature,
            string phaseSignature,
            string participationSignature,
            string actorSetRef)
        {
            return
                $"phase-local-entry-ready|context:{AsText(context.ContextSignature)}|routeId:{context.RouteId}|routeKind:{context.RouteRef.RouteKind}|scene:{AsText(sceneName)}|reason:{AsText(context.Reason)}|session:{AsText(sessionSignature)}|phase:{AsText(phaseSignature)}|participation:{AsText(participationSignature)}|actorSetRef:{AsText(actorSetRef)}|continuation:{plan.ResolvedContinuation}|composition:{plan.Composition}|execution:{plan.Execution}";
        }

        private static string ResolveSceneNameOrFail(SceneTransitionContext context)
        {
            if (context.TargetActiveScene == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] TargetActiveScene ausente ao publicar SessionTransitionPhaseLocalEntryReadyEvent.");
            }

            string sceneName = context.TargetActiveScene.Trim();
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] TargetActiveScene vazio ao publicar SessionTransitionPhaseLocalEntryReadyEvent.");
            }

            return sceneName;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunContinuationOperationalHandoffService : IRunContinuationOperationalHandoffService
    {
        private readonly SessionTransitionPlanResolver _planResolver;
        private readonly SessionTransitionOrchestrator _orchestrator;

        public RunContinuationOperationalHandoffService(
            SessionTransitionPlanResolver planResolver,
            SessionTransitionOrchestrator orchestrator)
        {
            _planResolver = planResolver ?? throw new ArgumentNullException(nameof(planResolver));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public async Task DispatchAsync(_ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts.RunContinuationSelection selection)
        {
            SessionTransitionContext context = new SessionTransitionContext(selection);
            SessionTransitionPlan plan = _planResolver.Resolve(context);

            DebugUtility.Log<RunContinuationOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_accepted target='SessionTransitionExecution' continuation='{plan.ResolvedContinuation}' reason='{Normalize(plan.Reason)}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            await _orchestrator.ExecuteAsync(plan);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
