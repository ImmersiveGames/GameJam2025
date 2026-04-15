using System;
using System.Collections.Generic;
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
    public enum PhaseNavigationOutcome
    {
        Changed = 0,
        BlockedAtFirst = 1,
        BlockedAtLast = 2,
        RejectedNotReady = 3,
        InvalidCurrentPhase = 4,
        InvalidCatalog = 5,
        SpecificPhaseIdInvalid = 6,
        SpecificPhaseMissing = 7,
        TargetAlreadyCurrent = 8
    }

    public readonly struct PhaseNavigationRequest
    {
        public PhaseNavigationRequest(PhaseNavigationDirection direction, string reason, string targetPhaseId = null)
        {
            Direction = direction;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            TargetPhaseId = string.IsNullOrWhiteSpace(targetPhaseId) ? string.Empty : targetPhaseId.Trim();
        }

        public PhaseNavigationDirection Direction { get; }
        public string Reason { get; }
        public string TargetPhaseId { get; }
        public bool HasTargetPhaseId => !string.IsNullOrWhiteSpace(TargetPhaseId);

        public static PhaseNavigationRequest Next(string reason = null)
            => new(PhaseNavigationDirection.Next, reason);

        public static PhaseNavigationRequest Previous(string reason = null)
            => new(PhaseNavigationDirection.Previous, reason);

        public static PhaseNavigationRequest Specific(string phaseId, string reason = null)
            => new(PhaseNavigationDirection.Specific, reason, phaseId);
    }

    public readonly struct PhaseNavigationResult
    {
        public PhaseNavigationResult(
            PhaseNavigationRequest request,
            PhaseNavigationOutcome outcome,
            PhaseDefinitionAsset currentPhaseRef,
            string catalogName,
            PhaseCatalogTraversalMode traversalMode,
            bool wasWrapped,
            PhaseNavigationSelectionContext selectionContext)
        {
            Request = request;
            Outcome = outcome;
            CurrentPhaseRef = currentPhaseRef;
            CatalogName = string.IsNullOrWhiteSpace(catalogName) ? string.Empty : catalogName.Trim();
            TraversalMode = traversalMode;
            WasWrapped = wasWrapped;
            SelectionContext = selectionContext;
        }

        public PhaseNavigationRequest Request { get; }
        public PhaseNavigationOutcome Outcome { get; }
        public PhaseDefinitionAsset CurrentPhaseRef { get; }
        public string CatalogName { get; }
        public PhaseCatalogTraversalMode TraversalMode { get; }
        public bool WasWrapped { get; }
        public PhaseNavigationSelectionContext SelectionContext { get; }

        public PhaseNavigationDirection Direction => Request.Direction;
        public string Reason => Request.Reason;
        public PhaseDefinitionAsset TargetPhaseRef => SelectionContext.TargetPhaseRef;
        public string FromPhaseId => CurrentPhaseRef != null && CurrentPhaseRef.PhaseId.IsValid ? CurrentPhaseRef.PhaseId.Value : string.Empty;
        public string ToPhaseId => TargetPhaseRef != null && TargetPhaseRef.PhaseId.IsValid ? TargetPhaseRef.PhaseId.Value : string.Empty;
        public bool HasSelectionContext => Outcome == PhaseNavigationOutcome.Changed && SelectionContext.IsValid;
        public bool IsBlockedAtBoundary =>
            Outcome == PhaseNavigationOutcome.BlockedAtFirst ||
            Outcome == PhaseNavigationOutcome.BlockedAtLast;
    }

    public enum PhaseNavigationDirection
    {
        Next = 0,
        Previous = 1,
        Specific = 2
    }

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

        public Task<PhaseNavigationResult> NavigateAsync(PhaseNavigationRequest request, CancellationToken ct = default)
        {
            return ExecuteNavigationAsync(request, ct);
        }

        public Task<PhaseNavigationResult> NextPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            return NavigateAsync(PhaseNavigationRequest.Next(reason), ct);
        }

        public Task<PhaseNavigationResult> AdvancePhaseAsync(string reason = null, CancellationToken ct = default)
        {
            return NextPhaseAsync(reason, ct);
        }

        public Task<PhaseNavigationResult> PreviousPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            return NavigateAsync(PhaseNavigationRequest.Previous(reason), ct);
        }

        public Task<PhaseNavigationResult> GoToSpecificPhaseAsync(string phaseId, string reason = null, CancellationToken ct = default)
        {
            return NavigateAsync(PhaseNavigationRequest.Specific(phaseId, reason), ct);
        }

        private async Task<PhaseNavigationResult> ExecuteNavigationAsync(PhaseNavigationRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            PhaseNavigationRequest normalizedRequest = new PhaseNavigationRequest(request.Direction, request.Reason, request.TargetPhaseId);
            string directionLabel = PhaseNextPhaseServiceSupport.DescribeDirection(normalizedRequest.Direction);

            DebugUtility.Log<PhaseNextPhaseService>(
                $"[OBS][PhaseFlow] {directionLabel}Requested owner='PhaseNextPhaseService' reason='{normalizedRequest.Reason}'.",
                DebugUtility.Colors.Info);

            await _gate.WaitAsync(ct);
            try
            {
                PhaseNavigationResult selectionResult = _selectionService.SelectPhase(normalizedRequest);
                if (selectionResult.Outcome != PhaseNavigationOutcome.Changed)
                {
                    return selectionResult;
                }

                PhaseNavigationSelectionContext selectionContext = selectionResult.SelectionContext;

                PhaseNavigationCompositionContext compositionContext = await _compositionService.ApplyNavigationPhaseAsync(selectionContext, ct);
                await _handoffService.CompleteIntroHandoffAsync(compositionContext, ct);

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][PhaseFlow] PhaseChanged direction='{directionLabel}' from='{PhaseNextPhaseServiceSupport.DescribePhase(selectionContext.CurrentPhaseRef)}' to='{PhaseNextPhaseServiceSupport.DescribePhase(selectionContext.TargetPhaseRef)}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{normalizedRequest.Reason}'.",
                    DebugUtility.Colors.Success);

                return selectionResult;
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    public readonly struct PhaseNavigationSelectionContext
    {
        public PhaseNavigationSelectionContext(
            GameplayStartSnapshot currentSnapshot,
            PhaseDefinitionAsset targetPhaseRef,
            PhaseDefinitionSelectedEvent phaseSelectedEvent,
            string reason,
            string targetSceneName,
            PhaseNavigationDirection direction)
        {
            CurrentSnapshot = currentSnapshot;
            TargetPhaseRef = targetPhaseRef;
            PhaseSelectedEvent = phaseSelectedEvent;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            TargetSceneName = string.IsNullOrWhiteSpace(targetSceneName) ? string.Empty : targetSceneName.Trim();
            Direction = direction;
        }

        public GameplayStartSnapshot CurrentSnapshot { get; }
        public PhaseDefinitionAsset CurrentPhaseRef => CurrentSnapshot.PhaseDefinitionRef;
        public PhaseDefinitionAsset TargetPhaseRef { get; }
        public PhaseDefinitionSelectedEvent PhaseSelectedEvent { get; }
        public string Reason { get; }
        public string TargetSceneName { get; }
        public PhaseNavigationDirection Direction { get; }

        public int SelectionVersion => PhaseSelectedEvent.SelectionVersion;
        public string TargetIntroContentId => TargetPhaseRef != null ? TargetPhaseRef.BuildCanonicalIntroContentId() : string.Empty;
        public bool IsValid =>
            CurrentSnapshot.IsValid &&
            TargetPhaseRef != null &&
            PhaseSelectedEvent.IsValid &&
            !string.IsNullOrWhiteSpace(TargetSceneName);
    }

    public readonly struct PhaseNavigationCompositionContext
    {
        public PhaseNavigationCompositionContext(
            PhaseNavigationSelectionContext selectionContext,
            SceneCompositionRequest applyRequest)
        {
            SelectionContext = selectionContext;
            ApplyRequest = applyRequest;
        }

        public PhaseNavigationSelectionContext SelectionContext { get; }
        public SceneCompositionRequest ApplyRequest { get; }
        public bool IsValid => SelectionContext.IsValid;
    }

    public interface IPhaseNextPhaseSelectionService
    {
        PhaseNavigationResult SelectPhase(PhaseNavigationRequest request);
    }

    public interface IPhaseNextPhaseCompositionService
    {
        Task<PhaseNavigationCompositionContext> ApplyNavigationPhaseAsync(PhaseNavigationSelectionContext selectionContext, CancellationToken ct);
    }

    public interface IPhaseNextPhaseEntryHandoffService
    {
        Task CompleteIntroHandoffAsync(PhaseNavigationCompositionContext compositionContext, CancellationToken ct);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseNextPhaseSelectionService : IPhaseNextPhaseSelectionService
    {
        public PhaseNavigationResult SelectPhase(PhaseNavigationRequest request)
        {
            string normalizedReason = PhaseNextPhaseServiceSupport.NormalizeReason(request.Reason);
            PhaseNavigationRequest normalizedRequest = new PhaseNavigationRequest(request.Direction, normalizedReason, request.TargetPhaseId);

            GameplayStartSnapshot currentSnapshot = ResolveCurrentGameplaySnapshotOrFail(normalizedReason);
            IPhaseDefinitionCatalog catalog = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<IPhaseDefinitionCatalog>("IPhaseDefinitionCatalog");
            IPhaseCatalogRuntimeStateService runtimeStateService = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<IPhaseCatalogRuntimeStateService>("IPhaseCatalogRuntimeStateService");
            PhaseDefinitionAsset currentPhaseRef = runtimeStateService.CurrentCommitted;
            if (currentPhaseRef == null || !currentPhaseRef.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Catalog runtime state requires a committed current phase. reason='{normalizedReason}'.");
            }

            string catalogName = PhaseNextPhaseServiceSupport.DescribeCatalog(catalog);
            PhaseCatalogTraversalMode traversalMode = catalog.TraversalMode;
            if (normalizedRequest.Direction == PhaseNavigationDirection.Specific)
            {
                return SelectSpecificPhase(normalizedRequest, currentSnapshot, catalog, currentPhaseRef, catalogName, traversalMode);
            }

            bool moveNext = normalizedRequest.Direction == PhaseNavigationDirection.Next;
            IReadOnlyList<string> phaseIds = catalog.PhaseIds;
            int currentIndex = ResolveCurrentPhaseIndexOrFail(catalog, currentPhaseRef);
            int targetIndex = moveNext ? currentIndex + 1 : currentIndex - 1;
            bool wrapped = false;

            if (phaseIds == null || phaseIds.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionCatalog '{catalogName}' has no phaseIds available for traversal.");
            }

            if (targetIndex >= phaseIds.Count)
            {
                if (traversalMode == PhaseCatalogTraversalMode.Finite)
                {
                    DebugUtility.LogWarning<PhaseNextPhaseSelectionService>(
                        $"[WARN][PhaseFlow] NavigationBlocked outcome='BlockedAtLast' action='Next' currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(currentPhaseRef)}' catalog='{catalogName}' traversalMode='Finite' reason='end_of_catalog'.");
                    return BuildBlockedResult(normalizedRequest, PhaseNavigationOutcome.BlockedAtLast, currentPhaseRef, catalogName, traversalMode);
                }

                targetIndex = 0;
                wrapped = true;
            }

            if (targetIndex < 0)
            {
                if (traversalMode == PhaseCatalogTraversalMode.Finite)
                {
                    DebugUtility.LogWarning<PhaseNextPhaseSelectionService>(
                        $"[WARN][PhaseFlow] NavigationBlocked outcome='BlockedAtFirst' action='Previous' currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(currentPhaseRef)}' catalog='{catalogName}' traversalMode='Finite' reason='start_of_catalog'.");
                    return BuildBlockedResult(normalizedRequest, PhaseNavigationOutcome.BlockedAtFirst, currentPhaseRef, catalogName, traversalMode);
                }

                targetIndex = phaseIds.Count - 1;
                wrapped = true;
            }

            string targetPhaseId = phaseIds[targetIndex];
            if (!catalog.TryGet(targetPhaseId, out PhaseDefinitionAsset targetPhaseRef) || targetPhaseRef == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Traversal resolved an invalid target phaseId='{targetPhaseId}' from catalog '{catalogName}'.");
            }

            return BuildChangedResult(normalizedRequest, currentSnapshot, currentPhaseRef, targetPhaseRef, catalogName, traversalMode, wrapped);
        }

        private static PhaseNavigationResult BuildChangedResult(
            PhaseNavigationRequest request,
            GameplayStartSnapshot currentSnapshot,
            PhaseDefinitionAsset currentPhaseRef,
            PhaseDefinitionAsset targetPhaseRef,
            string catalogName,
            PhaseCatalogTraversalMode traversalMode,
            bool wasWrapped)
        {
            int nextSelectionVersion = Math.Max(currentSnapshot.SelectionVersion + 1, 1);

            PhaseDefinitionSelectedEvent phaseSelectedEvent = new PhaseDefinitionSelectedEvent(
                targetPhaseRef,
                currentSnapshot.MacroRouteId,
                currentSnapshot.MacroRouteRef,
                nextSelectionVersion,
                request.Reason);

            string targetSceneName = SceneManager.GetActiveScene().name;
            PhaseNavigationSelectionContext selectionContext = new PhaseNavigationSelectionContext(
                currentSnapshot,
                targetPhaseRef,
                phaseSelectedEvent,
                request.Reason,
                targetSceneName,
                request.Direction);

            UpdateCatalogRuntimeState(targetPhaseRef, request.Reason);

            if (wasWrapped)
            {
                DebugUtility.Log<PhaseNextPhaseSelectionService>(
                    $"[OBS][PhaseFlow] NavigationWrapped action='{PhaseNextPhaseServiceSupport.DescribeDirection(request.Direction)}' from='{PhaseNextPhaseServiceSupport.DescribePhase(currentPhaseRef)}' to='{PhaseNextPhaseServiceSupport.DescribePhase(targetPhaseRef)}' traversalMode='{traversalMode}' catalog='{catalogName}' reason='{request.Reason}'.",
                    DebugUtility.Colors.Info);
            }

            PublishSelection(selectionContext);
            return new PhaseNavigationResult(
                request,
                PhaseNavigationOutcome.Changed,
                currentPhaseRef,
                catalogName,
                traversalMode,
                wasWrapped,
                selectionContext);
        }

        private static PhaseNavigationResult SelectSpecificPhase(
            PhaseNavigationRequest request,
            GameplayStartSnapshot currentSnapshot,
            IPhaseDefinitionCatalog catalog,
            PhaseDefinitionAsset currentPhaseRef,
            string catalogName,
            PhaseCatalogTraversalMode traversalMode)
        {
            string specificPhaseId = request.TargetPhaseId;
            if (string.IsNullOrWhiteSpace(specificPhaseId))
            {
                DebugUtility.LogWarning<PhaseNextPhaseSelectionService>(
                    $"[WARN][PhaseFlow] NavigationBlocked outcome='SpecificPhaseIdInvalid' action='Specific' currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(currentPhaseRef)}' catalog='{catalogName}' reason='empty_phaseId'.");
                return BuildBlockedResult(request, PhaseNavigationOutcome.SpecificPhaseIdInvalid, currentPhaseRef, catalogName, traversalMode);
            }

            if (!catalog.TryGet(specificPhaseId, out _) )
            {
                DebugUtility.LogWarning<PhaseNextPhaseSelectionService>(
                    $"[WARN][PhaseFlow] NavigationBlocked outcome='SpecificPhaseMissing' action='Specific' currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(currentPhaseRef)}' targetPhaseId='{PhaseDefinitionId.Normalize(specificPhaseId)}' catalog='{catalogName}' reason='phase_not_found'.");
                return BuildBlockedResult(request, PhaseNavigationOutcome.SpecificPhaseMissing, currentPhaseRef, catalogName, traversalMode);
            }

            PhaseDefinitionAsset targetPhaseRef = catalog.ResolveSpecificPhaseOrFail(specificPhaseId);
            if (targetPhaseRef == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Catalog '{catalogName}' returned null while resolving specific phaseId='{PhaseDefinitionId.Normalize(specificPhaseId)}'.");
            }

            if (string.Equals(currentPhaseRef.PhaseId.Value, targetPhaseRef.PhaseId.Value, StringComparison.OrdinalIgnoreCase))
            {
                DebugUtility.LogWarning<PhaseNextPhaseSelectionService>(
                    $"[WARN][PhaseFlow] NavigationBlocked outcome='TargetAlreadyCurrent' action='Specific' currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(currentPhaseRef)}' targetPhaseId='{targetPhaseRef.PhaseId}' catalog='{catalogName}' reason='target_equals_current'.");
                return BuildBlockedResult(request, PhaseNavigationOutcome.TargetAlreadyCurrent, currentPhaseRef, catalogName, traversalMode);
            }

            return BuildChangedResult(request, currentSnapshot, currentPhaseRef, targetPhaseRef, catalogName, traversalMode, false);
        }

        private static void UpdateCatalogRuntimeState(PhaseDefinitionAsset targetPhaseRef, string reason)
        {
            IPhaseCatalogRuntimeStateService runtimeStateService = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<IPhaseCatalogRuntimeStateService>(
                "IPhaseCatalogRuntimeStateService");

            // O alvo fica pendente ate a composicao e o handoff terminarem.
            runtimeStateService.SetPendingTarget(targetPhaseRef, reason);
            runtimeStateService.CommitCurrentTarget(targetPhaseRef, reason);
        }

        private static PhaseNavigationResult BuildBlockedResult(
            PhaseNavigationRequest request,
            PhaseNavigationOutcome outcome,
            PhaseDefinitionAsset currentPhaseRef,
            string catalogName,
            PhaseCatalogTraversalMode traversalMode)
        {
            return new PhaseNavigationResult(
                request,
                outcome,
                currentPhaseRef,
                catalogName,
                traversalMode,
                false,
                default);
        }

        private static int ResolveCurrentPhaseIndexOrFail(IPhaseDefinitionCatalog catalog, PhaseDefinitionAsset currentPhaseRef)
        {
            if (catalog == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Missing catalog while resolving current phase index.");
            }

            if (currentPhaseRef == null || !currentPhaseRef.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Current phase is invalid while resolving navigation target.");
            }

            IReadOnlyList<string> phaseIds = catalog.PhaseIds;
            if (phaseIds == null || phaseIds.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Catalog '{PhaseNextPhaseServiceSupport.DescribeCatalog(catalog)}' has no phaseIds.");
            }

            string currentPhaseId = currentPhaseRef.PhaseId.Value;
            for (int i = 0; i < phaseIds.Count; i++)
            {
                if (string.Equals(phaseIds[i], currentPhaseId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Current phaseId='{currentPhaseId}' is not present in catalog '{PhaseNextPhaseServiceSupport.DescribeCatalog(catalog)}'.");
            return -1;
        }

        private static void PublishSelection(PhaseNavigationSelectionContext selectionContext)
        {
            EventBus<PhaseDefinitionSelectedEvent>.Raise(selectionContext.PhaseSelectedEvent);

            DebugUtility.Log<PhaseNextPhaseSelectionService>(
                $"[OBS][PhaseFlow] PhaseSelected direction='{PhaseNextPhaseServiceSupport.DescribeDirection(selectionContext.Direction)}' from='{PhaseNextPhaseServiceSupport.DescribePhase(selectionContext.CurrentPhaseRef)}' to='{PhaseNextPhaseServiceSupport.DescribePhase(selectionContext.TargetPhaseRef)}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static GameplayStartSnapshot ResolveCurrentGameplaySnapshotOrFail(string reason)
        {
            if (!PhaseNextPhaseServiceSupport.TryResolveGlobal<IRestartContextService>(out var restartContextService) ||
                restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] IRestartContextService ausente ao executar phase navigation. reason='{reason}'.");
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
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Phase navigation requer um gameplay snapshot valido. reason='{reason}'.");
            }

            return currentSnapshot;
        }

    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseNextPhaseCompositionService : IPhaseNextPhaseCompositionService
    {
        public async Task<PhaseNavigationCompositionContext> ApplyNavigationPhaseAsync(PhaseNavigationSelectionContext selectionContext, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!selectionContext.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseCompositionService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Invalid selection context received by phase navigation composition service.");
            }

            string currentPhaseId = PhaseNextPhaseServiceSupport.DescribePhase(selectionContext.CurrentPhaseRef);
            string targetPhaseId = PhaseNextPhaseServiceSupport.DescribePhase(selectionContext.TargetPhaseRef);

            ISceneCompositionExecutor sceneCompositionExecutor = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<ISceneCompositionExecutor>("ISceneCompositionExecutor");

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentAppliedExecutorConfirmed executor='SceneCompositionExecutor' owner='PhaseNextPhaseCompositionService' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionRequest applyRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                selectionContext.TargetPhaseRef,
                selectionContext.Reason,
                selectionContext.PhaseSelectedEvent.SelectionSignature);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentAppliedStart direction='{PhaseNextPhaseServiceSupport.DescribeDirection(selectionContext.Direction)}' from='{currentPhaseId}' to='{targetPhaseId}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] scenesToUnload=[{string.Join(",", applyRequest.ScenesToUnload)}] correlationId='{applyRequest.CorrelationId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            await sceneCompositionExecutor.ApplyAsync(applyRequest, ct);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentCleared phaseId='{currentPhaseId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseLegacyBridgeBypassed path='phase_enabled_navigation' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                selectionContext.TargetPhaseRef,
                applyRequest.ScenesToLoad,
                applyRequest.ActiveScene,
                source: "PhaseDefinitionNavigation");

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentReadModelCommitted owner='PhaseContentSceneRuntimeApplier' phaseId='{targetPhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentApplied currentPhaseRef='{currentPhaseId}' targetPhaseRef='{targetPhaseId}' phaseId='{targetPhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Success);

            return new PhaseNavigationCompositionContext(selectionContext, applyRequest);
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

        public async Task CompleteIntroHandoffAsync(PhaseNavigationCompositionContext compositionContext, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!compositionContext.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseEntryHandoffService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Invalid composition context received by phase navigation handoff service.");
            }

            PhaseNavigationSelectionContext selectionContext = compositionContext.SelectionContext;
            if (!_introStageSessionService.TryGetCurrentSession(out IntroStageSession introSession) || !introSession.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseEntryHandoffService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] IntroStageSessionService nao disponibilizou a session canonica apos a composicao da phase navigation.");
            }

            if (introSession.PhaseDefinitionRef == null ||
                !string.Equals(introSession.PhaseDefinitionRef.PhaseId.Value, selectionContext.TargetPhaseRef.PhaseId.Value, StringComparison.Ordinal) ||
                introSession.SelectionVersion != selectionContext.SelectionVersion ||
                !string.Equals(introSession.LocalContentId, selectionContext.TargetIntroContentId, StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseEntryHandoffService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] IntroStageSession canonica nao corresponde a phase selecionada. expectedPhaseId='{selectionContext.TargetPhaseRef.PhaseId}' expectedVersion='{selectionContext.SelectionVersion}' expectedContentId='{selectionContext.TargetIntroContentId}' actualPhaseId='{(introSession.PhaseDefinitionRef != null ? introSession.PhaseDefinitionRef.PhaseId.Value : "<none>")}' actualVersion='{introSession.SelectionVersion}' actualContentId='{introSession.LocalContentId}'.");
            }

            string targetPhaseName = selectionContext.TargetPhaseRef.name;
            string introDispatchSource = "PhaseDefinitionNavigation";

            DebugUtility.Log<PhaseNextPhaseEntryHandoffService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNavigationIntroSessionResolved owner='IntroStageSessionService' direction='{PhaseNextPhaseServiceSupport.DescribeDirection(selectionContext.Direction)}' targetPhaseRef='{targetPhaseName}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}' signature='{introSession.SessionSignature}' hasIntroStage='{introSession.HasIntroStage}' hasRunResultStage='{introSession.HasRunResultStage}'.",
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

            string targetPhaseId = selectionContext.TargetPhaseRef.PhaseId.Value;
            ClearPendingCatalogTarget(selectionContext.Reason);
            DebugUtility.Log<PhaseNextPhaseEntryHandoffService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentAppliedCompleted current='{targetPhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}' introSkipped='{introCompletedEvent.WasSkipped.ToString().ToLowerInvariant()}' introReason='{PhaseNextPhaseServiceSupport.NormalizeReason(introCompletedEvent.Reason)}' source='{introCompletedEvent.Source}'.",
                DebugUtility.Colors.Info);
        }

        private static void ClearPendingCatalogTarget(string reason)
        {
            IPhaseCatalogRuntimeStateService runtimeStateService = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<IPhaseCatalogRuntimeStateService>(
                "IPhaseCatalogRuntimeStateService");

            // O pending so e limpo depois do handoff canonico concluir.
            runtimeStateService.ClearPendingTarget(reason);
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
            => string.IsNullOrWhiteSpace(reason) ? "PhaseDefinition/Navigation" : reason.Trim();

        public static string DescribeDirection(PhaseNavigationDirection direction)
        {
            return direction switch
            {
                PhaseNavigationDirection.Previous => "Previous",
                PhaseNavigationDirection.Specific => "Specific",
                _ => "Next"
            };
        }

        public static string DescribePhase(PhaseDefinitionAsset phaseDefinition)
        {
            return phaseDefinition != null && phaseDefinition.PhaseId.IsValid
                ? phaseDefinition.PhaseId.Value
                : "<none>";
        }

        public static string DescribeCatalog(IPhaseDefinitionCatalog catalog)
        {
            if (catalog is UnityEngine.Object unityObject)
            {
                return unityObject.name;
            }

            return catalog != null ? catalog.GetType().Name : "<none>";
        }
    }
}
