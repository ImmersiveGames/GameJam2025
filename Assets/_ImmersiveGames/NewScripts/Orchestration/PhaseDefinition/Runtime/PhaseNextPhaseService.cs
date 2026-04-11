using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseNextPhaseService : IPhaseNextPhaseService
    {
        private readonly IPhaseNextPhaseSelectionService _selectionService;
        private readonly IPhaseNextPhaseCompositionService _compositionService;
        private readonly IPhaseNextPhaseEntryHandoffService _handoffService;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public PhaseNextPhaseService(
            IPhaseNextPhaseSelectionService selectionService,
            IPhaseNextPhaseCompositionService compositionService,
            IPhaseNextPhaseEntryHandoffService handoffService)
        {
            _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
            _compositionService = compositionService ?? throw new ArgumentNullException(nameof(compositionService));
            _handoffService = handoffService ?? throw new ArgumentNullException(nameof(handoffService));
        }

        public async Task NextPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = PhaseNextPhaseServiceSupport.NormalizeReason(reason);

            DebugUtility.Log<PhaseNextPhaseService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNextPhaseExecutionStarted owner='PhaseNextPhaseService' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            await _gate.WaitAsync(ct);
            try
            {
                if (!_selectionService.TrySelectNextPhase(normalizedReason, out PhaseNextPhaseSelectionContext selectionContext))
                {
                    return;
                }

                PhaseNextPhaseCompositionContext compositionContext = await _compositionService.ApplyNextPhaseAsync(selectionContext, ct);
                await _handoffService.CompleteIntroHandoffAsync(compositionContext, ct);

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNextPhaseExecutionCompleted owner='PhaseNextPhaseService' phaseId='{selectionContext.NextPhaseRef.PhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Success);
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    public readonly struct PhaseNextPhaseSelectionContext
    {
        public PhaseNextPhaseSelectionContext(
            GameplayStartSnapshot currentSnapshot,
            PhaseDefinitionAsset nextPhaseRef,
            PhaseDefinitionSelectedEvent phaseSelectedEvent,
            string reason,
            string targetSceneName)
        {
            CurrentSnapshot = currentSnapshot;
            NextPhaseRef = nextPhaseRef;
            PhaseSelectedEvent = phaseSelectedEvent;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            TargetSceneName = string.IsNullOrWhiteSpace(targetSceneName) ? string.Empty : targetSceneName.Trim();
        }

        public GameplayStartSnapshot CurrentSnapshot { get; }
        public PhaseDefinitionAsset NextPhaseRef { get; }
        public PhaseDefinitionSelectedEvent PhaseSelectedEvent { get; }
        public string Reason { get; }
        public string TargetSceneName { get; }

        public int SelectionVersion => PhaseSelectedEvent.SelectionVersion;
        public string NextIntroContentId => NextPhaseRef != null ? NextPhaseRef.BuildCanonicalIntroContentId() : string.Empty;
        public bool IsValid =>
            CurrentSnapshot.IsValid &&
            NextPhaseRef != null &&
            PhaseSelectedEvent.IsValid &&
            !string.IsNullOrWhiteSpace(TargetSceneName);
    }

    public readonly struct PhaseNextPhaseCompositionContext
    {
        public PhaseNextPhaseCompositionContext(
            PhaseNextPhaseSelectionContext selectionContext,
            SceneCompositionRequest applyRequest)
        {
            SelectionContext = selectionContext;
            ApplyRequest = applyRequest;
        }

        public PhaseNextPhaseSelectionContext SelectionContext { get; }
        public SceneCompositionRequest ApplyRequest { get; }
        public bool IsValid => SelectionContext.IsValid;
    }

    public interface IPhaseNextPhaseSelectionService
    {
        bool TrySelectNextPhase(string reason, out PhaseNextPhaseSelectionContext selectionContext);
    }

    public interface IPhaseNextPhaseCompositionService
    {
        Task<PhaseNextPhaseCompositionContext> ApplyNextPhaseAsync(PhaseNextPhaseSelectionContext selectionContext, CancellationToken ct);
    }

    public interface IPhaseNextPhaseEntryHandoffService
    {
        Task CompleteIntroHandoffAsync(PhaseNextPhaseCompositionContext compositionContext, CancellationToken ct);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseNextPhaseSelectionService : IPhaseNextPhaseSelectionService
    {
        public bool TrySelectNextPhase(string reason, out PhaseNextPhaseSelectionContext selectionContext)
        {
            selectionContext = default;

            string normalizedReason = PhaseNextPhaseServiceSupport.NormalizeReason(reason);
            GameplayStartSnapshot currentSnapshot = ResolveCurrentGameplaySnapshotOrFail(normalizedReason);
            IPhaseDefinitionCatalog catalog = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<IPhaseDefinitionCatalog>("IPhaseDefinitionCatalog");

            if (!catalog.TryGetNext(currentSnapshot.PhaseDefinitionRef.PhaseId.Value, out PhaseDefinitionAsset nextPhaseRef) ||
                nextPhaseRef == null)
            {
                DebugUtility.LogWarning(typeof(PhaseNextPhaseSelectionService),
                    $"[WARN][GameplaySessionFlow][PhaseDefinition] PhaseNextUnavailable current='{currentSnapshot.PhaseDefinitionRef.PhaseId}' reason='end_of_catalog'.");
                return false;
            }

            string currentPhaseId = currentSnapshot.PhaseDefinitionRef.PhaseId.Value;
            string currentPhaseName = currentSnapshot.PhaseDefinitionRef.name;
            string nextPhaseId = nextPhaseRef.PhaseId.Value;
            string nextPhaseName = nextPhaseRef.name;
            int nextSelectionVersion = Math.Max(currentSnapshot.SelectionVersion + 1, 1);

            PhaseDefinitionSelectedEvent phaseSelectedEvent = new PhaseDefinitionSelectedEvent(
                nextPhaseRef,
                currentSnapshot.MacroRouteId,
                currentSnapshot.MacroRouteRef,
                nextSelectionVersion,
                normalizedReason);

            DebugUtility.Log<PhaseNextPhaseSelectionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNextPhaseOwnerConfirmed owner='PhaseNextPhaseSelectionService' currentPhaseId='{currentPhaseId}' nextPhaseId='{nextPhaseId}' routeId='{currentSnapshot.MacroRouteId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<PhaseNextPhaseSelectionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNextPhaseSelected currentPhaseRef='{currentPhaseName}' nextPhaseRef='{nextPhaseName}' routeId='{currentSnapshot.MacroRouteId}' v='{nextSelectionVersion}' reason='{normalizedReason}' signature='{phaseSelectedEvent.SelectionSignature}'.",
                DebugUtility.Colors.Info);

            SyncRestartContextFromPhaseSelection(phaseSelectedEvent);
            EventBus<PhaseDefinitionSelectedEvent>.Raise(phaseSelectedEvent);

            string targetSceneName = SceneManager.GetActiveScene().name;
            selectionContext = new PhaseNextPhaseSelectionContext(
                currentSnapshot,
                nextPhaseRef,
                phaseSelectedEvent,
                normalizedReason,
                targetSceneName);

            return true;
        }

        private static GameplayStartSnapshot ResolveCurrentGameplaySnapshotOrFail(string reason)
        {
            if (!PhaseNextPhaseServiceSupport.TryResolveGlobal<IRestartContextService>(out var restartContextService) ||
                restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] IRestartContextService ausente ao executar NextPhase. reason='{reason}'.");
            }

            if (!restartContextService.TryGetCurrent(out GameplayStartSnapshot currentSnapshot) ||
                !currentSnapshot.IsValid ||
                !currentSnapshot.HasPhaseDefinitionRef ||
                currentSnapshot.PhaseDefinitionRef == null ||
                !currentSnapshot.PhaseDefinitionRef.PhaseId.IsValid ||
                currentSnapshot.MacroRouteRef == null ||
                !currentSnapshot.MacroRouteId.IsValid ||
                currentSnapshot.MacroRouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] NextPhase requer um gameplay snapshot valido. reason='{reason}'.");
            }

            return currentSnapshot;
        }

        private static void SyncRestartContextFromPhaseSelection(PhaseDefinitionSelectedEvent phaseSelectedEvent)
        {
            if (!PhaseNextPhaseServiceSupport.TryResolveGlobal<IRestartContextService>(out var restartContextService) ||
                restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] RestartContextService ausente durante publicacao phase-owned de PhaseDefinitionSelectedEvent.");
            }

            GameplayStartSnapshot gameplayStartSnapshot = new GameplayStartSnapshot(
                phaseSelectedEvent.PhaseDefinitionRef,
                phaseSelectedEvent.MacroRouteId,
                phaseSelectedEvent.MacroRouteRef,
                phaseSelectedEvent.PhaseDefinitionRef.BuildCanonicalIntroContentId(),
                phaseSelectedEvent.Reason,
                phaseSelectedEvent.SelectionVersion,
                phaseSelectedEvent.SelectionSignature);

            restartContextService.RegisterGameplayStart(gameplayStartSnapshot);

            DebugUtility.Log<PhaseNextPhaseSelectionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] GameplayStartSnapshotLinked owner='PhaseNextPhaseSelectionService' phaseId='{phaseSelectedEvent.PhaseId}' routeId='{phaseSelectedEvent.MacroRouteId}' v='{phaseSelectedEvent.SelectionVersion}' reason='{phaseSelectedEvent.Reason}' signature='{gameplayStartSnapshot.PhaseSignature}'.",
                DebugUtility.Colors.Info);
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseNextPhaseCompositionService : IPhaseNextPhaseCompositionService
    {
        public async Task<PhaseNextPhaseCompositionContext> ApplyNextPhaseAsync(PhaseNextPhaseSelectionContext selectionContext, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!selectionContext.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseCompositionService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Invalid selection context received by next-phase composition service.");
            }

            string currentPhaseId = selectionContext.CurrentSnapshot.PhaseDefinitionRef.PhaseId.Value;
            string currentPhaseName = selectionContext.CurrentSnapshot.PhaseDefinitionRef.name;
            string nextPhaseId = selectionContext.NextPhaseRef.PhaseId.Value;
            string nextPhaseName = selectionContext.NextPhaseRef.name;

            ISceneCompositionExecutor sceneCompositionExecutor = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<ISceneCompositionExecutor>("ISceneCompositionExecutor");

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentAppliedExecutorConfirmed executor='SceneCompositionExecutor' owner='PhaseNextPhaseCompositionService' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionRequest applyRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                selectionContext.NextPhaseRef,
                selectionContext.Reason,
                selectionContext.PhaseSelectedEvent.SelectionSignature);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentAppliedStart from='{currentPhaseId}' to='{nextPhaseId}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] scenesToUnload=[{string.Join(",", applyRequest.ScenesToUnload)}] correlationId='{applyRequest.CorrelationId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            await sceneCompositionExecutor.ApplyAsync(applyRequest, ct);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentCleared phaseId='{currentPhaseId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseLegacyBridgeBypassed path='phase_enabled_next_phase' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                selectionContext.NextPhaseRef,
                applyRequest.ScenesToLoad,
                applyRequest.ActiveScene,
                source: "PhaseDefinitionNextPhase");

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentReadModelCommitted owner='PhaseContentSceneRuntimeApplier' phaseId='{nextPhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentApplied currentPhaseRef='{currentPhaseName}' nextPhaseRef='{nextPhaseName}' phaseId='{nextPhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Success);

            return new PhaseNextPhaseCompositionContext(selectionContext, applyRequest);
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseNextPhaseEntryHandoffService : IPhaseNextPhaseEntryHandoffService
    {
        private readonly IIntroStageSessionService _introStageSessionService;
        private readonly IIntroStageLifecycleDispatchService _introStageLifecycleDispatchService;

        public PhaseNextPhaseEntryHandoffService(
            IIntroStageSessionService introStageSessionService,
            IIntroStageLifecycleDispatchService introStageLifecycleDispatchService)
        {
            _introStageSessionService = introStageSessionService ?? throw new ArgumentNullException(nameof(introStageSessionService));
            _introStageLifecycleDispatchService = introStageLifecycleDispatchService ?? throw new ArgumentNullException(nameof(introStageLifecycleDispatchService));
        }

        public async Task CompleteIntroHandoffAsync(PhaseNextPhaseCompositionContext compositionContext, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!compositionContext.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseEntryHandoffService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Invalid composition context received by next-phase handoff service.");
            }

            PhaseNextPhaseSelectionContext selectionContext = compositionContext.SelectionContext;
            if (!_introStageSessionService.TryGetCurrentSession(out IntroStageSession introSession) || !introSession.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseEntryHandoffService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] IntroStageSessionService nao disponibilizou a session canonica apos a composicao da next-phase.");
            }

            if (introSession.PhaseDefinitionRef == null ||
                !string.Equals(introSession.PhaseDefinitionRef.PhaseId.Value, selectionContext.NextPhaseRef.PhaseId.Value, StringComparison.Ordinal) ||
                introSession.SelectionVersion != selectionContext.SelectionVersion ||
                !string.Equals(introSession.LocalContentId, selectionContext.NextIntroContentId, StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseEntryHandoffService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] IntroStageSession canonica nao corresponde a phase selecionada. expectedPhaseId='{selectionContext.NextPhaseRef.PhaseId}' expectedVersion='{selectionContext.SelectionVersion}' expectedContentId='{selectionContext.NextIntroContentId}' actualPhaseId='{(introSession.PhaseDefinitionRef != null ? introSession.PhaseDefinitionRef.PhaseId.Value : "<none>")}' actualVersion='{introSession.SelectionVersion}' actualContentId='{introSession.LocalContentId}'.");
            }

            string nextPhaseName = selectionContext.NextPhaseRef.name;
            string introDispatchSource = "PhaseDefinitionNextPhase";

            DebugUtility.Log<PhaseNextPhaseEntryHandoffService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNextPhaseIntroSessionResolved owner='IntroStageSessionService' nextPhaseRef='{nextPhaseName}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}' signature='{introSession.SessionSignature}' hasIntroStage='{introSession.HasIntroStage}' hasRunResultStage='{introSession.HasRunResultStage}'.",
                DebugUtility.Colors.Info);

            using var introWaitCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task<IntroStageCompletedEvent> introCompletionTask = WaitForIntroCompletionAsync(
                introSession.SessionSignature,
                introWaitCts.Token);

            _introStageLifecycleDispatchService.DispatchIntroStage(
                introDispatchSource,
                introSession,
                selectionContext.CurrentSnapshot.MacroRouteRef.RouteKind,
                selectionContext.Reason);

            IntroStageCompletedEvent introCompletedEvent = await introCompletionTask;

            string nextPhaseId = selectionContext.NextPhaseRef.PhaseId.Value;
            DebugUtility.Log<PhaseNextPhaseEntryHandoffService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentAppliedCompleted current='{nextPhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}' introSkipped='{introCompletedEvent.WasSkipped.ToString().ToLowerInvariant()}' introReason='{PhaseNextPhaseServiceSupport.NormalizeReason(introCompletedEvent.Reason)}' source='{introCompletedEvent.Source}'.",
                DebugUtility.Colors.Info);
        }

        private static async Task<IntroStageCompletedEvent> WaitForIntroCompletionAsync(
            string expectedSignature,
            CancellationToken ct)
        {
            var completionSource = new TaskCompletionSource<IntroStageCompletedEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventBinding<IntroStageCompletedEvent> binding = null!;

            binding = new EventBinding<IntroStageCompletedEvent>(evt =>
            {
                if (!evt.Session.IsValid)
                {
                    return;
                }

                if (!string.Equals(evt.Source, "GameplaySessionFlow", StringComparison.Ordinal))
                {
                    return;
                }

                if (!string.Equals(evt.Session.SessionSignature, expectedSignature, StringComparison.Ordinal))
                {
                    return;
                }

                completionSource.TrySetResult(evt);
            });

            EventBus<IntroStageCompletedEvent>.Register(binding);

            CancellationTokenRegistration cancellationRegistration = default;

            try
            {
                if (ct.CanBeCanceled)
                {
                    cancellationRegistration = ct.Register(() => completionSource.TrySetCanceled(ct));
                }

                return await completionSource.Task;
            }
            finally
            {
                cancellationRegistration.Dispose();
                EventBus<IntroStageCompletedEvent>.Unregister(binding);
            }
        }
    }

    internal static class PhaseNextPhaseServiceSupport
    {
        public static bool TryResolveGlobal<T>(out T service)
            where T : class
        {
            service = null;

            if (DependencyManager.Provider == null)
            {
                return false;
            }

            return DependencyManager.Provider.TryGetGlobal<T>(out service) && service != null;
        }

        public static T ResolveRequiredGlobal<T>(string label)
            where T : class
        {
            if (!TryResolveGlobal<T>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseServiceSupport),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Missing required global service: {label}.");
            }

            return service;
        }

        public static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "PhaseDefinition/NextPhase" : reason.Trim();
    }
}
