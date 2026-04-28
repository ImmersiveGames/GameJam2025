using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionTransitionOrchestrator
    {
        private const string GameplaySessionPrepareSource = "GameplaySessionPrepare";
        private const string RunContinuationSelectionSource = "RunContinuationSelection";

        private readonly SessionTransitionPlanResolver _planResolver;
        private readonly ISessionTransitionExecutionPort _executionPort;
        private readonly ISessionTransitionGameplayPrepareExecutionPort _gameplayPrepareExecutionPort;

        public SessionTransitionOrchestrator(
            SessionTransitionPlanResolver planResolver,
            ISessionTransitionExecutionPort executionPort,
            ISessionTransitionGameplayPrepareExecutionPort gameplayPrepareExecutionPort)
        {
            _planResolver = planResolver ?? throw new ArgumentNullException(nameof(planResolver));
            _executionPort = executionPort ?? throw new ArgumentNullException(nameof(executionPort));
            _gameplayPrepareExecutionPort = gameplayPrepareExecutionPort ?? throw new ArgumentNullException(nameof(gameplayPrepareExecutionPort));
        }

        public Task ExecuteAsync(SceneTransitionContext context, CancellationToken ct = default)
        {
            HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                "[FATAL][H1][SessionTransition] ExecuteAsync(SceneTransitionContext) sem origin tipada foi desativado. SceneTransitionContext is InitialEntry-only. Reentry/PostRun/PhaseNavigation must use SessionTransitionContext.");
            return Task.CompletedTask;
        }

        public async Task ExecuteAsync(SceneTransitionContext context, SessionTransitionOrigin origin, CancellationToken ct = default)
        {
            if (!context.RouteId.IsValid || context.RouteRef == null || context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] SceneTransitionContext invalido recebido pelo orquestrador gameplay prepare.");
            }

            if (origin != SessionTransitionOrigin.InitialEntry)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SceneTransitionContext is InitialEntry-only. Reentry/PostRun/PhaseNavigation must use SessionTransitionContext. origin='{origin}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' reason='{Normalize(context.Reason)}'.");
            }

            if (!context.IsGameplayInitialEntry)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SceneTransitionContext InitialEntry rail requer payload GameplayInitialEntry. Reentry/PostRun/PhaseNavigation must use SessionTransitionContext. routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' reason='{Normalize(context.Reason)}'.");
            }

            string normalizedReason = Normalize(context.Reason);
            string signature = SceneTransitionSignature.Compute(context);
            SessionTransitionPlan plan = _planResolver.Resolve(context, origin);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PlanResolved source='{GameplaySessionPrepareSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' legacyContinuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' executionKind='{plan.Execution.Kind}' phaseLocalEntryReady='{plan.EmitsPhaseLocalEntryReady}' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteStarted source='{GameplaySessionPrepareSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' legacyContinuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' phaseLocalEntryReady='{plan.EmitsPhaseLocalEntryReady}' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            if (!plan.EmitsPhaseLocalEntryReady)
            {
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] SkipNoContent source='{GameplaySessionPrepareSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' legacyContinuation='{plan.ResolvedContinuation}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return;
            }

            SessionTransitionExecutionDispatchResult executionResult = await _gameplayPrepareExecutionPort.ExecuteAsync(context, plan, ct);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecutionDispatchCompleted source='{GameplaySessionPrepareSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' legacyContinuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' wasExecuted='{executionResult.WasExecuted}' allowsPhaseLocalEntryReady='{executionResult.AllowsPhaseLocalEntryReady}' failureReason='{Normalize(executionResult.FailureReason)}' detail='{Normalize(executionResult.Detail)}' reason='{normalizedReason}'.",
                executionResult.AllowsPhaseLocalEntryReady ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            if (plan.EmitsPhaseLocalEntryReady && !executionResult.AllowsPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SessionTransitionPlan InitialEntry declara PhaseLocalEntryReady, mas o resultado operacional nao permite publicacao. source='{GameplaySessionPrepareSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' wasExecuted='{executionResult.WasExecuted}' failureReason='{Normalize(executionResult.FailureReason)}' detail='{Normalize(executionResult.Detail)}' reason='{normalizedReason}'.");
            }

            if (!executionResult.WasExecuted)
            {
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] SkipNoExecution source='{GameplaySessionPrepareSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' phaseLocalEntryReady='{plan.EmitsPhaseLocalEntryReady}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return;
            }

            if (plan.EmitsPhaseLocalEntryReady)
            {
                if (!executionResult.TryGetPhaseLocalEntryReadyEvent(out var phaseLocalEntryReadyEvent))
                {
                    HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                        $"[FATAL][H1][SessionTransition] SessionTransitionGameplayPrepareExecutionPort confirmou PhaseLocalEntryReady sem evento canonico. source='{GameplaySessionPrepareSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' reason='{normalizedReason}'.");
                }

                PublishPhaseLocalEntryReadyOrFail(
                    phaseLocalEntryReadyEvent,
                    GameplaySessionPrepareSource,
                    normalizedReason);
            }

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted source='{GameplaySessionPrepareSource}' routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' intent='{plan.IntentKind}' legacyContinuation='{plan.ResolvedContinuation}' reason='{normalizedReason}'.",
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
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteStarted source='{RunContinuationSelectionSource}' origin='{plan.Context.Origin}' continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' executionKind='{plan.Execution.Kind}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' phaseLocalEntryReady='{shouldEmitPhaseLocalEntryReady}' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            SessionTransitionExecutionDispatchResult executionResult = await _executionPort.DispatchAsync(plan, ct);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecutionDispatchCompleted source='{RunContinuationSelectionSource}' origin='{plan.Context.Origin}' continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' wasExecuted='{executionResult.WasExecuted}' allowsPhaseLocalEntryReady='{executionResult.AllowsPhaseLocalEntryReady}' failureReason='{Normalize(executionResult.FailureReason)}' detail='{Normalize(executionResult.Detail)}' reason='{normalizedReason}'.",
                executionResult.AllowsPhaseLocalEntryReady ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            if (shouldEmitPhaseLocalEntryReady && !executionResult.AllowsPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SessionTransitionPlan declara PhaseLocalEntryReady, mas o resultado operacional nao permite publicacao. source='{RunContinuationSelectionSource}' origin='{plan.Context.Origin}' continuation='{plan.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' wasExecuted='{executionResult.WasExecuted}' failureReason='{Normalize(executionResult.FailureReason)}' detail='{Normalize(executionResult.Detail)}' reason='{normalizedReason}'.");
            }

            if (!executionResult.WasExecuted)
            {
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] SkipNoExecution source='{RunContinuationSelectionSource}' origin='{plan.Context.Origin}' continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' phaseLocalEntryReady='{shouldEmitPhaseLocalEntryReady}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return;
            }

            if (shouldEmitPhaseLocalEntryReady)
            {
                PublishPhaseLocalEntryReadyOrFail(
                    BuildPlanPhaseLocalEntryReadyEventOrFail(plan, RunContinuationSelectionSource),
                    RunContinuationSelectionSource,
                    normalizedReason);
            }

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted source='{RunContinuationSelectionSource}' origin='{plan.Context.Origin}' continuation='{plan.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' executionKind='{plan.Execution.Kind}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' phaseLocalEntryReady='{shouldEmitPhaseLocalEntryReady}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        private static void PublishPhaseLocalEntryReadyOrFail(
            SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent,
            string source,
            string normalizedReason)
        {
            if (!phaseLocalEntryReadyEvent.HasCanonicalPayload)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SessionTransitionPhaseLocalEntryReadyEvent construido sem payload canonico. source='{Normalize(source)}' intent='{phaseLocalEntryReadyEvent.Plan.IntentKind}' legacyContinuation='{phaseLocalEntryReadyEvent.Plan.ResolvedContinuation}' executionKind='{phaseLocalEntryReadyEvent.Plan.Execution.Kind}' reason='{Normalize(normalizedReason)}'.");
            }

            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Raise(phaseLocalEntryReadyEvent);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PhaseLocalEntryReadyPublished source='{Normalize(source)}' intent='{phaseLocalEntryReadyEvent.Plan.IntentKind}' legacyContinuation='{phaseLocalEntryReadyEvent.Plan.ResolvedContinuation}' executionKind='{phaseLocalEntryReadyEvent.Plan.Execution.Kind}' execution='{phaseLocalEntryReadyEvent.Plan.Execution}' routeId='{phaseLocalEntryReadyEvent.RouteId}' routeKind='{phaseLocalEntryReadyEvent.RouteKind}' scene='{phaseLocalEntryReadyEvent.SceneName}' actorSetRef='{phaseLocalEntryReadyEvent.ActorSetRef}' sessionSignature='{phaseLocalEntryReadyEvent.SessionSignature}' phaseSignature='{phaseLocalEntryReadyEvent.PhaseSignature}' participationSignature='{phaseLocalEntryReadyEvent.ParticipationSignature}' cycleSignature='{phaseLocalEntryReadyEvent.CycleSignature}' reason='{Normalize(normalizedReason)}'.",
                DebugUtility.Colors.Success);
        }


        private static SessionTransitionPhaseLocalEntryReadyEvent BuildPlanPhaseLocalEntryReadyEventOrFail(
            SessionTransitionPlan plan,
            string source)
        {
            if (!plan.IsValid || !plan.EmitsPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SessionTransitionPlan invalido ao construir SessionTransitionPhaseLocalEntryReadyEvent. source='{Normalize(source)}'.");
            }

            PhaseLocalEntryReadyRuntimePayload payload = ResolvePhaseLocalEntryReadyRuntimePayloadOrFail(source);
            GameplaySessionContextSnapshot sessionContext = payload.PhaseRuntime.SessionContext;
            if (!sessionContext.IsValid ||
                !sessionContext.MacroRouteId.IsValid ||
                sessionContext.MacroRouteRef == null ||
                sessionContext.MacroRouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SessionTransitionPlan requer session context gameplay valido para publicar PhaseLocalEntryReady. source='{Normalize(source)}' continuation='{plan.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' reason='{Normalize(plan.Reason)}'.");
            }

            if (payload.ActorSetRouteKind != SceneRouteKind.Gameplay || payload.ActorSetRouteKind != sessionContext.MacroRouteRef.RouteKind)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] ActorSetRef canonico incompativel com SessionTransitionPlan. routeKind='{payload.ActorSetRouteKind}' source='{Normalize(payload.ActorSetRouteSource)}' sessionRouteKind='{sessionContext.MacroRouteRef.RouteKind}'.");
            }

            string sceneName = ResolveActiveSceneNameOrFail(plan, source);
            // O payload de RunContinuation/PostRun é resolvido depois da execução operacional.
            // No rail RestartCurrentPhase, isso preserva a ordem validada:
            // PhaseResetCompletedEvent -> GameplayPhaseFlowService rearm -> PhaseLocalEntryReady payload.
            string contextSignature = plan.ContextSignature;
            string sessionSignature = payload.PhaseRuntime.SessionContext.SessionSignature;
            string phaseSignature = payload.PhaseRuntime.PhaseRuntimeSignature;
            string participationSignature = payload.ParticipationSnapshot.Signature.Value;
            string actorSetRef = payload.ActorSetRef.Value;

            ValidateRequiredPhaseLocalEntryReadyValueOrFail(
                nameof(contextSignature),
                contextSignature,
                plan,
                source,
                sessionContext.MacroRouteId,
                sceneName);

            ValidateRequiredPhaseLocalEntryReadyValueOrFail(
                nameof(sessionSignature),
                sessionSignature,
                plan,
                source,
                sessionContext.MacroRouteId,
                sceneName);

            ValidateRequiredPhaseLocalEntryReadyValueOrFail(
                nameof(phaseSignature),
                phaseSignature,
                plan,
                source,
                sessionContext.MacroRouteId,
                sceneName);

            ValidateRequiredPhaseLocalEntryReadyValueOrFail(
                nameof(participationSignature),
                participationSignature,
                plan,
                source,
                sessionContext.MacroRouteId,
                sceneName);

            ValidateRequiredPhaseLocalEntryReadyValueOrFail(
                nameof(actorSetRef),
                actorSetRef,
                plan,
                source,
                sessionContext.MacroRouteId,
                sceneName);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PhaseLocalEntryReadyPayloadResolved payloadResolvedAfterExecution='true' payloadSource='current_runtime_post_execution' rearmOrdering='PhaseResetCompletedEvent->GameplayPhaseFlowService.OnPhaseResetCompleted->HandlePhaseRearm->BuildPlanPhaseLocalEntryReadyEventOrFail' source='{Normalize(source)}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' continuation='{plan.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' routeId='{sessionContext.MacroRouteId}' routeKind='{sessionContext.MacroRouteRef.RouteKind}' scene='{sceneName}' contextSignature='{contextSignature}' sessionSignature='{sessionSignature}' phaseSignature='{phaseSignature}' participationSignature='{participationSignature}' actorSetRef='{actorSetRef}' reason='{Normalize(plan.Reason)}'.",
                DebugUtility.Colors.Info);

            return CreatePhaseLocalEntryReadyEventOrFail(
                plan,
                source,
                sessionContext.MacroRouteId,
                sessionContext.MacroRouteRef.RouteKind,
                sceneName,
                plan.Reason,
                contextSignature,
                payload);
        }

        private static void ValidateRequiredPhaseLocalEntryReadyValueOrFail(
            string fieldName,
            string value,
            SessionTransitionPlan plan,
            string source,
            SceneRouteId routeId,
            string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                $"[FATAL][H1][SessionTransition] Campo obrigatorio ausente ao montar SessionTransitionPhaseLocalEntryReadyEvent a partir do runtime corrente pos-execucao. payloadResolvedAfterExecution='true' field='{Normalize(fieldName)}' source='{Normalize(source)}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' continuation='{plan.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' routeId='{routeId}' scene='{Normalize(sceneName)}' reason='{Normalize(plan.Reason)}'.");
        }

        private static SessionTransitionPhaseLocalEntryReadyEvent CreatePhaseLocalEntryReadyEventOrFail(
            SessionTransitionPlan plan,
            string source,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName,
            string reason,
            string contextSignature,
            PhaseLocalEntryReadyRuntimePayload payload)
        {
            string normalizedReason = Normalize(reason);
            string sessionSignature = payload.PhaseRuntime.SessionContext.SessionSignature;
            string phaseSignature = payload.PhaseRuntime.PhaseRuntimeSignature;
            string participationSignature = payload.ParticipationSnapshot.Signature.Value;
            string cycleSignature = BuildPhaseLocalEntryReadyCycleSignature(
                contextSignature,
                plan,
                routeId,
                routeKind,
                sceneName,
                normalizedReason,
                sessionSignature,
                phaseSignature,
                participationSignature,
                payload.ActorSetRef.Value);

            return new SessionTransitionPhaseLocalEntryReadyEvent(
                plan,
                source,
                routeId,
                routeKind,
                sceneName,
                normalizedReason,
                sessionSignature,
                phaseSignature,
                participationSignature,
                payload.ActorSetRef.Value,
                cycleSignature);
        }

        private static PhaseLocalEntryReadyRuntimePayload ResolvePhaseLocalEntryReadyRuntimePayloadOrFail(string source)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] DependencyManager.Provider indisponivel ao construir SessionTransitionPhaseLocalEntryReadyEvent. source='{Normalize(source)}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out var routeActorSetContext) || routeActorSetContext == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] ISceneFlowRouteActorSetRefContext ausente ao construir SessionTransitionPhaseLocalEntryReadyEvent. source='{Normalize(source)}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRuntimeService>(out var phaseRuntimeService) || phaseRuntimeService == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] IGameplayPhaseRuntimeService ausente ao construir SessionTransitionPhaseLocalEntryReadyEvent. source='{Normalize(source)}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var participationFlowService) || participationFlowService == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] IGameplayParticipationFlowService ausente ao construir SessionTransitionPhaseLocalEntryReadyEvent. source='{Normalize(source)}'.");
            }

            if (!routeActorSetContext.TryGetCurrent(out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out string routeSource) ||
                routeKind != SceneRouteKind.Gameplay ||
                !actorSetRef.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] ActorSetRef canonico ausente ao construir SessionTransitionPhaseLocalEntryReadyEvent. source='{Normalize(source)}' routeKind='{routeKind}' routeSource='{Normalize(routeSource)}'.");
            }

            if (!phaseRuntimeService.TryGetCurrent(out GameplayPhaseRuntimeSnapshot phaseRuntime) ||
                !phaseRuntime.IsValid ||
                phaseRuntime.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] GameplayPhaseRuntimeSnapshot invalido ao construir SessionTransitionPhaseLocalEntryReadyEvent. source='{Normalize(source)}'.");
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
                    $"[FATAL][H1][SessionTransition] Participation snapshot invalida ao construir SessionTransitionPhaseLocalEntryReadyEvent. source='{Normalize(source)}' readinessState='{readiness.State}' canEnterGameplay='{readiness.CanEnterGameplay}'.");
            }

            return new PhaseLocalEntryReadyRuntimePayload(
                actorSetRef,
                routeKind,
                routeSource,
                phaseRuntime,
                participationSnapshot);
        }

        private static string BuildPhaseLocalEntryReadyCycleSignature(
            string contextSignature,
            SessionTransitionPlan plan,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName,
            string reason,
            string sessionSignature,
            string phaseSignature,
            string participationSignature,
            string actorSetRef)
        {
            return
                $"phase-local-entry-ready|context:{AsText(contextSignature)}|routeId:{routeId}|routeKind:{routeKind}|scene:{AsText(sceneName)}|reason:{AsText(reason)}|session:{AsText(sessionSignature)}|phase:{AsText(phaseSignature)}|participation:{AsText(participationSignature)}|actorSetRef:{AsText(actorSetRef)}|intent:{plan.IntentKind}|legacyContinuation:{plan.ResolvedContinuation}|composition:{plan.Composition}|execution:{plan.Execution}";
        }

        private static string ResolveActiveSceneNameOrFail(SessionTransitionPlan plan, string source)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] ActiveScene vazio ao publicar SessionTransitionPhaseLocalEntryReadyEvent a partir de SessionTransitionPlan. source='{Normalize(source)}' continuation='{plan.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' reason='{Normalize(plan.Reason)}'.");
            }

            return sceneName.Trim();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        private readonly struct PhaseLocalEntryReadyRuntimePayload
        {
            public PhaseLocalEntryReadyRuntimePayload(
                ActorSetRef actorSetRef,
                SceneRouteKind actorSetRouteKind,
                string actorSetRouteSource,
                GameplayPhaseRuntimeSnapshot phaseRuntime,
                ParticipationSnapshot participationSnapshot)
            {
                ActorSetRef = actorSetRef;
                ActorSetRouteKind = actorSetRouteKind;
                ActorSetRouteSource = Normalize(actorSetRouteSource);
                PhaseRuntime = phaseRuntime;
                ParticipationSnapshot = participationSnapshot;
            }

            public ActorSetRef ActorSetRef { get; }
            public SceneRouteKind ActorSetRouteKind { get; }
            public string ActorSetRouteSource { get; }
            public GameplayPhaseRuntimeSnapshot PhaseRuntime { get; }
            public ParticipationSnapshot ParticipationSnapshot { get; }
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
