using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public interface ISessionTransitionGameplayPrepareExecutionPort
    {
        Task<SessionTransitionExecutionDispatchResult> ExecuteAsync(
            SessionTransitionContext context,
            SessionTransitionPlan plan,
            CancellationToken ct = default);
    }

    public sealed class SessionTransitionGameplayPrepareExecutionPort : ISessionTransitionGameplayPrepareExecutionPort
    {
        private const string GameplaySessionPrepareSource = "GameplaySessionPrepare";

        private readonly IPhaseDefinitionSelectionService _phaseSelectionService;
        private readonly GameplayPhaseFlowService _gameplayPhaseFlowService;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;
        private readonly ISceneFlowRouteActorSetRefContext _routeActorSetContext;
        private readonly IGameplayPhaseRuntimeService _phaseRuntimeService;
        private readonly IGameplayParticipationFlowService _participationFlowService;

        public SessionTransitionGameplayPrepareExecutionPort(
            IPhaseDefinitionSelectionService phaseSelectionService,
            GameplayPhaseFlowService gameplayPhaseFlowService,
            ISceneCompositionExecutor sceneCompositionExecutor,
            ISceneFlowRouteActorSetRefContext routeActorSetContext,
            IGameplayPhaseRuntimeService phaseRuntimeService,
            IGameplayParticipationFlowService participationFlowService)
        {
            _phaseSelectionService = phaseSelectionService ?? throw new ArgumentNullException(nameof(phaseSelectionService));
            _gameplayPhaseFlowService = gameplayPhaseFlowService ?? throw new ArgumentNullException(nameof(gameplayPhaseFlowService));
            _sceneCompositionExecutor = sceneCompositionExecutor ?? throw new ArgumentNullException(nameof(sceneCompositionExecutor));
            _routeActorSetContext = routeActorSetContext ?? throw new ArgumentNullException(nameof(routeActorSetContext));
            _phaseRuntimeService = phaseRuntimeService ?? throw new ArgumentNullException(nameof(phaseRuntimeService));
            _participationFlowService = participationFlowService ?? throw new ArgumentNullException(nameof(participationFlowService));
        }

        public async Task<SessionTransitionExecutionDispatchResult> ExecuteAsync(
            SessionTransitionContext context,
            SessionTransitionPlan plan,
            CancellationToken ct = default)
        {
            ValidateGameplayPrepareInputsOrFail(context, plan);

            string reason = Normalize(context.Reason);
            string signature = Normalize(context.ContextSignature);

            PhaseDefinitionAsset selectedPhaseDefinitionRef = _phaseSelectionService.ResolveOrFail();
            PhaseDefinitionSelectedEvent phaseSelectedEvent = _gameplayPhaseFlowService.PublishPhaseDefinitionSelected(
                selectedPhaseDefinitionRef,
                context.RouteId,
                context.RouteRef,
                reason);

            SceneCompositionRequest phaseCompositionRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                selectedPhaseDefinitionRef,
                reason,
                phaseSelectedEvent.SelectionSignature,
                forceFullReload: false);

            DebugUtility.Log<SessionTransitionGameplayPrepareExecutionPort>(
                $"[OBS][GameplaySessionFlow][SessionTransition] GameplayPrepareExecutionStarted source='{GameplaySessionPrepareSource}' routeId='{context.RouteId}' origin='{context.Origin}' intent='{context.IntentKind}' signature='{signature}' phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' planIntent='{plan.IntentKind}' runContinuation='{context.ResolvedContinuation}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(phaseCompositionRequest);

            const string canonicalPhaseContentAppliedSource = PhaseFlowSignalVocabulary.GameplaySessionFlowSource;
            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                selectedPhaseDefinitionRef,
                phaseCompositionRequest.ScenesToLoad,
                phaseCompositionRequest.ActiveScene,
                canonicalPhaseContentAppliedSource);

            SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent = BuildPhaseLocalEntryReadyEventOrFail(
                context,
                plan,
                GameplaySessionPrepareSource);

            DebugUtility.Log<SessionTransitionGameplayPrepareExecutionPort>(
                $"[OBS][GameplaySessionFlow][SessionTransition] GameplayPrepareExecutionCompleted source='{GameplaySessionPrepareSource}' routeId='{context.RouteId}' origin='{context.Origin}' intent='{context.IntentKind}' signature='{signature}' phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} allowsPhaseLocalEntryReady='True' reason='{reason}'.",
                DebugUtility.Colors.Success);

            return SessionTransitionExecutionDispatchResult.PhaseLocalEntryReadyConfirmed(
                plan.Execution.Kind,
                $"Gameplay prepare composition applied. phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' scenesAdded='{compositionResult.ScenesAdded}' scenesRemoved='{compositionResult.ScenesRemoved}'.",
                phaseLocalEntryReadyEvent);
        }

        private static void ValidateGameplayPrepareInputsOrFail(SessionTransitionContext context, SessionTransitionPlan plan)
        {
            if (!context.RouteId.IsValid || context.RouteRef == null || context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    "[FATAL][H1][SessionTransition] Gameplay prepare execution port recebeu SessionTransitionContext invalido.");
            }

            if (!context.IsGameplayInitialEntry)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] Gameplay prepare execution port requer InitialEntry. routeId='{context.RouteId}' origin='{context.Origin}' intent='{context.IntentKind}' reason='{Normalize(context.Reason)}'.");
            }

            if (!plan.IsValid || plan.Context.Origin != SessionTransitionOrigin.InitialEntry)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] Gameplay prepare execution port recebeu plano invalido ou origin nao-inicial. origin='{plan.Context.Origin}' intent='{plan.IntentKind}' executionKind='{plan.Execution.Kind}' reason='{Normalize(plan.Reason)}'.");
            }

            if (plan.Execution.Kind != SessionTransitionExecutionKind.InitialEntry || !plan.RequiresPhaseLocalEntryReady)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] Gameplay prepare InitialEntry requer execution InitialEntry com contrato derivado de PhaseLocalEntryReady. executionKind='{plan.Execution.Kind}' expectedPhaseLocalEntryReady='{plan.RequiresPhaseLocalEntryReady}' intent='{plan.IntentKind}' reason='{Normalize(plan.Reason)}'.");
            }
        }

        private SessionTransitionPhaseLocalEntryReadyEvent BuildPhaseLocalEntryReadyEventOrFail(
            SessionTransitionContext context,
            SessionTransitionPlan plan,
            string source)
        {
            string sceneName = ResolveSceneNameOrFail(context);
            EnsureSpawnRegistryReadyOrFail(sceneName, source);
            PhaseLocalEntryReadyRuntimePayload payload = ResolvePhaseLocalEntryReadyRuntimePayloadOrFail(source);

            if (payload.ActorSetRouteKind != SceneRouteKind.Gameplay || payload.ActorSetRouteKind != context.RouteRef.RouteKind)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] ActorSetRef canonico incompativel com gameplay prepare. routeKind='{payload.ActorSetRouteKind}' source='{Normalize(payload.ActorSetRouteSource)}' contextRouteKind='{context.RouteRef.RouteKind}'.");
            }

            return CreatePhaseLocalEntryReadyEventOrFail(
                plan,
                source,
                context.RouteId,
                context.RouteRef.RouteKind,
                sceneName,
                context.Reason,
                context.ContextSignature,
                payload);
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

        private PhaseLocalEntryReadyRuntimePayload ResolvePhaseLocalEntryReadyRuntimePayloadOrFail(string source)
        {
            if (!_routeActorSetContext.TryGetCurrent(out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out string routeSource) ||
                routeKind != SceneRouteKind.Gameplay ||
                !actorSetRef.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] ActorSetRef canonico ausente no gameplay prepare execution port. source='{Normalize(source)}' routeKind='{routeKind}' routeSource='{Normalize(routeSource)}'.");
            }

            if (!_phaseRuntimeService.TryGetCurrent(out GameplayPhaseRuntimeSnapshot phaseRuntime) ||
                !phaseRuntime.IsValid ||
                phaseRuntime.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] GameplayPhaseRuntimeSnapshot invalido no gameplay prepare execution port. source='{Normalize(source)}'.");
            }

            ParticipationReadinessSnapshot readiness = ParticipationReadinessSnapshot.Empty;
            bool hasReadiness = _participationFlowService.TryGetCurrentReadiness(out readiness);

            if (!_participationFlowService.TryGetCurrent(out ParticipationSnapshot participationSnapshot) ||
                !hasReadiness ||
                !participationSnapshot.IsValid ||
                !readiness.IsValid ||
                !readiness.CanEnterGameplay)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] Participation snapshot invalida no gameplay prepare execution port. source='{Normalize(source)}' readinessState='{readiness.State}' canEnterGameplay='{readiness.CanEnterGameplay}'.");
            }

            return new PhaseLocalEntryReadyRuntimePayload(
                actorSetRef,
                routeKind,
                routeSource,
                phaseRuntime,
                participationSnapshot);
        }

        private static void EnsureSpawnRegistryReadyOrFail(string sceneName, string source)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] DependencyManager.Provider indisponivel ao validar spawn registry no gameplay prepare execution port. source='{Normalize(source)}'.");
            }

            if (!DependencyManager.Provider.TryGetForScene<IWorldSpawnServiceRegistry>(sceneName, out var spawnRegistry) || spawnRegistry == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] IWorldSpawnServiceRegistry ausente para scene='{sceneName}' no gameplay prepare execution port. source='{Normalize(source)}'.");
            }

            if (spawnRegistry.Services == null || spawnRegistry.Services.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    $"[FATAL][H1][SessionTransition] IWorldSpawnServiceRegistry ainda sem servicos registrados para scene='{sceneName}' no gameplay prepare execution port. source='{Normalize(source)}'.");
            }
        }

        private static string ResolveSceneNameOrFail(SessionTransitionContext context)
        {
            if (context.SceneName == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    "[FATAL][H1][SessionTransition] SceneName ausente no gameplay prepare execution port.");
            }

            string sceneName = context.SceneName.Trim();
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionGameplayPrepareExecutionPort),
                    "[FATAL][H1][SessionTransition] SceneName vazio no gameplay prepare execution port.");
            }

            return sceneName;
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
}
