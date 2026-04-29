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
        private const string SessionTransitionContextSource = "SessionTransitionContext";

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

        public async Task ExecuteAsync(SessionTransitionContext context, CancellationToken ct = default)
        {
            if (!context.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SessionTransitionContext invalido recebido pelo orquestrador. origin='{context.Origin}' intent='{context.IntentKind}' reason='{Normalize(context.Reason)}'.");
            }

            string normalizedReason = Normalize(context.Reason);
            string signature = Normalize(context.ContextSignature);
            SessionTransitionPlan plan = _planResolver.Resolve(context);
            bool expectedPhaseLocalEntryReady = plan.RequiresPhaseLocalEntryReady;

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PlanResolved source='{SessionTransitionContextSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' signature='{signature}' runContinuation='{plan.Context.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' executionKind='{plan.Execution.Kind}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='<not_dispatched>' published='false' payloadSource='<none>' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteStarted source='{SessionTransitionContextSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' signature='{signature}' runContinuation='{plan.Context.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' continuityShape='{plan.Composition.ContinuityShape}' reconstructionShape='{plan.Composition.ReconstructionShape}' executionKind='{plan.Execution.Kind}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='<not_dispatched>' published='false' payloadSource='<none>' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            bool usesGameplayPreparePort = plan.Execution.Kind == SessionTransitionExecutionKind.InitialEntry;
            string dispatchPortName = usesGameplayPreparePort
                ? nameof(ISessionTransitionGameplayPrepareExecutionPort)
                : nameof(ISessionTransitionExecutionPort);

            SessionTransitionExecutionDispatchResult executionResult = usesGameplayPreparePort
                ? await _gameplayPrepareExecutionPort.ExecuteAsync(plan.Context, plan, ct)
                : await _executionPort.DispatchAsync(plan, ct);

            bool resultAllowsPhaseLocalEntryReady = executionResult.AllowsPhaseLocalEntryReady;

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecutionDispatchCompleted source='{SessionTransitionContextSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' signature='{signature}' runContinuation='{plan.Context.ResolvedContinuation}' composition='{plan.Composition}' execution='{plan.Execution}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' wasExecuted='{executionResult.WasExecuted}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='{resultAllowsPhaseLocalEntryReady}' published='false' payloadSource='<not_resolved>' dispatchPort='{dispatchPortName}' failureReason='{Normalize(executionResult.FailureReason)}' detail='{Normalize(executionResult.Detail)}' reason='{normalizedReason}'.",
                resultAllowsPhaseLocalEntryReady ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            if (expectedPhaseLocalEntryReady && !resultAllowsPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] Execution contract expects PhaseLocalEntryReady, but the operational result did not allow publication. source='{SessionTransitionContextSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' wasExecuted='{executionResult.WasExecuted}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='{resultAllowsPhaseLocalEntryReady}' failureReason='{Normalize(executionResult.FailureReason)}' detail='{Normalize(executionResult.Detail)}' reason='{normalizedReason}'.");
            }

            if (!expectedPhaseLocalEntryReady && resultAllowsPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] Execution contract does not expect PhaseLocalEntryReady, but the operational result allowed publication. source='{SessionTransitionContextSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' wasExecuted='{executionResult.WasExecuted}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='{resultAllowsPhaseLocalEntryReady}' failureReason='{Normalize(executionResult.FailureReason)}' detail='{Normalize(executionResult.Detail)}' reason='{normalizedReason}'.");
            }

            if (!executionResult.WasExecuted)
            {
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] SkipNoExecution source='{SessionTransitionContextSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' signature='{signature}' executionKind='{plan.Execution.Kind}' executionStatus='{executionResult.Status}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='{resultAllowsPhaseLocalEntryReady}' published='false' payloadSource='<none>' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return;
            }

            bool publishedPhaseLocalEntryReady = false;
            string resolvedPayloadSource = "<none>";

            if (expectedPhaseLocalEntryReady && resultAllowsPhaseLocalEntryReady)
            {
                bool publishedFromDispatchResult = executionResult.TryGetPhaseLocalEntryReadyEvent(out var phaseLocalEntryReadyEvent);
                resolvedPayloadSource = publishedFromDispatchResult ? "dispatch_result" : "post_dispatch_runtime";

                if (!publishedFromDispatchResult)
                {
                    phaseLocalEntryReadyEvent = ResolveCanonicalPhaseLocalEntryReadyEventOrFail(
                        plan,
                        dispatchPortName,
                        normalizedReason);
                }

                PublishPhaseLocalEntryReadyOrFail(
                    phaseLocalEntryReadyEvent,
                    SessionTransitionContextSource,
                    dispatchPortName,
                    resolvedPayloadSource,
                    normalizedReason,
                    publishedFromDispatchResult,
                    expectedPhaseLocalEntryReady,
                    resultAllowsPhaseLocalEntryReady);

                publishedPhaseLocalEntryReady = true;
            }

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted source='{SessionTransitionContextSource}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' signature='{signature}' runContinuation='{plan.Context.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='{resultAllowsPhaseLocalEntryReady}' published='{publishedPhaseLocalEntryReady}' payloadSource='{resolvedPayloadSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        private static void PublishPhaseLocalEntryReadyOrFail(
            SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent,
            string source,
            string dispatchPortName,
            string payloadSource,
            string normalizedReason,
            bool publishedFromDispatchResult,
            bool expectedPhaseLocalEntryReady,
            bool resultAllowsPhaseLocalEntryReady)
        {
            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PhaseLocalEntryReadyResolved source='{Normalize(source)}' dispatchPort='{Normalize(dispatchPortName)}' published='false' payloadSource='{Normalize(payloadSource)}' origin='{phaseLocalEntryReadyEvent.Plan.Context.Origin}' intent='{phaseLocalEntryReadyEvent.Plan.IntentKind}' runContinuation='{phaseLocalEntryReadyEvent.Plan.Context.ResolvedContinuation}' executionKind='{phaseLocalEntryReadyEvent.Plan.Execution.Kind}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='{resultAllowsPhaseLocalEntryReady}' execution='{phaseLocalEntryReadyEvent.Plan.Execution}' routeId='{phaseLocalEntryReadyEvent.RouteId}' routeKind='{phaseLocalEntryReadyEvent.RouteKind}' scene='{phaseLocalEntryReadyEvent.SceneName}' actorSetRef='{phaseLocalEntryReadyEvent.ActorSetRef}' sessionSignature='{phaseLocalEntryReadyEvent.SessionSignature}' phaseSignature='{phaseLocalEntryReadyEvent.PhaseSignature}' participationSignature='{phaseLocalEntryReadyEvent.ParticipationSignature}' cycleSignature='{phaseLocalEntryReadyEvent.CycleSignature}' reason='{Normalize(normalizedReason)}' canonicalPayload='{phaseLocalEntryReadyEvent.HasCanonicalPayload.ToString().ToLowerInvariant()}' dispatchResultConfirmed='{publishedFromDispatchResult.ToString().ToLowerInvariant()}'.",
                DebugUtility.Colors.Info);

            if (!phaseLocalEntryReadyEvent.HasCanonicalPayload)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SessionTransition confirmed PhaseLocalEntryReady but canonical payload could not be resolved. source='{Normalize(source)}' dispatchPort='{Normalize(dispatchPortName)}' origin='{phaseLocalEntryReadyEvent.Plan.Context.Origin}' intent='{phaseLocalEntryReadyEvent.Plan.IntentKind}' runContinuation='{phaseLocalEntryReadyEvent.Plan.Context.ResolvedContinuation}' executionKind='{phaseLocalEntryReadyEvent.Plan.Execution.Kind}' payloadSource='{Normalize(payloadSource)}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='{resultAllowsPhaseLocalEntryReady}' reason='{Normalize(normalizedReason)}'.");
            }

            EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Raise(phaseLocalEntryReadyEvent);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] PhaseLocalEntryReadyPublished source='{Normalize(source)}' dispatchPort='{Normalize(dispatchPortName)}' published='true' payloadSource='{Normalize(payloadSource)}' origin='{phaseLocalEntryReadyEvent.Plan.Context.Origin}' intent='{phaseLocalEntryReadyEvent.Plan.IntentKind}' runContinuation='{phaseLocalEntryReadyEvent.Plan.Context.ResolvedContinuation}' executionKind='{phaseLocalEntryReadyEvent.Plan.Execution.Kind}' expectedPhaseLocalEntryReady='{expectedPhaseLocalEntryReady}' resultAllowsPhaseLocalEntryReady='{resultAllowsPhaseLocalEntryReady}' execution='{phaseLocalEntryReadyEvent.Plan.Execution}' routeId='{phaseLocalEntryReadyEvent.RouteId}' routeKind='{phaseLocalEntryReadyEvent.RouteKind}' scene='{phaseLocalEntryReadyEvent.SceneName}' actorSetRef='{phaseLocalEntryReadyEvent.ActorSetRef}' sessionSignature='{phaseLocalEntryReadyEvent.SessionSignature}' phaseSignature='{phaseLocalEntryReadyEvent.PhaseSignature}' participationSignature='{phaseLocalEntryReadyEvent.ParticipationSignature}' cycleSignature='{phaseLocalEntryReadyEvent.CycleSignature}' reason='{Normalize(normalizedReason)}'.",
                DebugUtility.Colors.Success);
        }

        private static SessionTransitionPhaseLocalEntryReadyEvent ResolveCanonicalPhaseLocalEntryReadyEventOrFail(
            SessionTransitionPlan plan,
            string dispatchPortName,
            string normalizedReason)
        {
            try
            {
                return BuildPlanPhaseLocalEntryReadyEventOrFail(
                    plan,
                    ResolvePhaseLocalEntryReadyEventSource(plan, dispatchPortName));
            }
            catch (Exception ex)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                $"[FATAL][H1][SessionTransition] SessionTransition confirmed PhaseLocalEntryReady but canonical payload could not be resolved. source='{Normalize(SessionTransitionContextSource)}' dispatchPort='{Normalize(dispatchPortName)}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' runContinuation='{plan.Context.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' payloadSource='post_dispatch_runtime' reason='{Normalize(normalizedReason)}'.",
                    ex);
                throw;
            }
        }


        private static string ResolvePhaseLocalEntryReadyEventSource(
            SessionTransitionPlan plan,
            string dispatchPortName)
        {
            return plan.IntentKind == SessionTransitionIntentKind.PhaseOrdinalNavigation
                ? PhaseFlowSignalVocabulary.SessionTransitionPhaseOrdinalNavigationSource
                : Normalize(dispatchPortName);
        }

        private static SessionTransitionPhaseLocalEntryReadyEvent BuildPlanPhaseLocalEntryReadyEventOrFail(
            SessionTransitionPlan plan,
            string source)
        {
            if (!plan.IsValid || !plan.RequiresPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                $"[FATAL][H1][SessionTransition] SessionTransitionPlan invalido ou sem contrato derivado de PhaseLocalEntryReady ao construir SessionTransitionPhaseLocalEntryReadyEvent. source='{Normalize(source)}'.");
            }

            PhaseLocalEntryReadyRuntimePayload payload = ResolvePhaseLocalEntryReadyRuntimePayloadOrFail(source);
            GameplaySessionContextSnapshot sessionContext = payload.PhaseRuntime.SessionContext;
            if (!sessionContext.IsValid ||
                !sessionContext.MacroRouteId.IsValid ||
                sessionContext.MacroRouteRef == null ||
                sessionContext.MacroRouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] SessionTransitionPlan requer session context gameplay valido para publicar PhaseLocalEntryReady. source='{Normalize(source)}' runContinuation='{plan.Context.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' reason='{Normalize(plan.Reason)}'.");
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
                $"[OBS][GameplaySessionFlow][SessionTransition] PhaseLocalEntryReadyPayloadResolved payloadResolvedAfterExecution='true' payloadSource='current_runtime_post_execution' payloadOrdering='post_execution_runtime_payload' source='{Normalize(source)}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' continuation='{plan.Context.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' routeId='{sessionContext.MacroRouteId}' routeKind='{sessionContext.MacroRouteRef.RouteKind}' scene='{sceneName}' contextSignature='{contextSignature}' sessionSignature='{sessionSignature}' phaseSignature='{phaseSignature}' participationSignature='{participationSignature}' actorSetRef='{actorSetRef}' reason='{Normalize(plan.Reason)}'.",
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
                $"[FATAL][H1][SessionTransition] Campo obrigatorio ausente ao montar SessionTransitionPhaseLocalEntryReadyEvent a partir do runtime corrente pos-execucao. payloadResolvedAfterExecution='true' field='{Normalize(fieldName)}' source='{Normalize(source)}' origin='{plan.Context.Origin}' intent='{plan.IntentKind}' continuation='{plan.Context.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' routeId='{routeId}' scene='{Normalize(sceneName)}' reason='{Normalize(plan.Reason)}'.");
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
                $"phase-local-entry-ready|context:{AsText(contextSignature)}|routeId:{routeId}|routeKind:{routeKind}|scene:{AsText(sceneName)}|reason:{AsText(reason)}|session:{AsText(sessionSignature)}|phase:{AsText(phaseSignature)}|participation:{AsText(participationSignature)}|actorSetRef:{AsText(actorSetRef)}|intent:{plan.IntentKind}|runContinuation:{plan.Context.ResolvedContinuation}|composition:{plan.Composition}|execution:{plan.Execution}";
        }

        private static string ResolveActiveSceneNameOrFail(SessionTransitionPlan plan, string source)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    $"[FATAL][H1][SessionTransition] ActiveScene vazio ao publicar SessionTransitionPhaseLocalEntryReadyEvent a partir de SessionTransitionPlan. source='{Normalize(source)}' continuation='{plan.Context.ResolvedContinuation}' executionKind='{plan.Execution.Kind}' reason='{Normalize(plan.Reason)}'.");
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
        private readonly SessionTransitionOrchestrator _orchestrator;

        public RunContinuationOperationalHandoffService(SessionTransitionOrchestrator orchestrator)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public async Task DispatchAsync(_ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts.RunContinuationSelection selection)
        {
            SessionTransitionContext context = new SessionTransitionContext(selection);

            DebugUtility.Log<RunContinuationOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_accepted target='SessionTransitionOrchestrator' origin='{context.Origin}' intent='{context.IntentKind}' continuation='{context.ResolvedContinuation}' reason='{Normalize(context.Reason)}' nextState='{Normalize(context.NextState)}'.",
                DebugUtility.Colors.Info);

            await _orchestrator.ExecuteAsync(context);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
