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

    public enum PhaseNavigationRequestKind
    {
        Next = 0,
        Previous = 1,
        Specific = 2,
        RestartCatalog = 3
    }

    public readonly struct PhaseNavigationRequest
    {
        public PhaseNavigationRequest(PhaseNavigationRequestKind kind, PhaseNavigationDirection direction, string reason, string targetPhaseId = null)
        {
            Kind = kind;
            Direction = direction;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            TargetPhaseId = string.IsNullOrWhiteSpace(targetPhaseId) ? string.Empty : targetPhaseId.Trim();
        }

        public PhaseNavigationRequestKind Kind { get; }
        public PhaseNavigationDirection Direction { get; }
        public string Reason { get; }
        public string TargetPhaseId { get; }
        public bool HasTargetPhaseId => !string.IsNullOrWhiteSpace(TargetPhaseId);

        public static PhaseNavigationRequest Next(string reason = null)
            => new(PhaseNavigationRequestKind.Next, PhaseNavigationDirection.Next, reason);

        public static PhaseNavigationRequest Previous(string reason = null)
            => new(PhaseNavigationRequestKind.Previous, PhaseNavigationDirection.Previous, reason);

        public static PhaseNavigationRequest Specific(string phaseId, string reason = null)
            => new(PhaseNavigationRequestKind.Specific, PhaseNavigationDirection.Specific, reason, phaseId);

        public static PhaseNavigationRequest RestartCatalog(string phaseId, string reason = null)
            => new(PhaseNavigationRequestKind.RestartCatalog, PhaseNavigationDirection.Specific, reason, phaseId);
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

        public Task<PhaseNavigationResult> PreviousPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            return NavigateAsync(PhaseNavigationRequest.Previous(reason), ct);
        }

        public Task<PhaseNavigationResult> GoToSpecificPhaseAsync(string phaseId, string reason = null, CancellationToken ct = default)
        {
            return NavigateAsync(PhaseNavigationRequest.Specific(phaseId, reason), ct);
        }

        public Task<PhaseNavigationResult> RestartCatalogAsync(string reason = null, CancellationToken ct = default)
        {
            return RestartCatalogInternalAsync(reason, ct);
        }

        private async Task<PhaseNavigationResult> ExecuteNavigationAsync(PhaseNavigationRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            PhaseNavigationRequest normalizedRequest = new PhaseNavigationRequest(request.Kind, request.Direction, request.Reason, request.TargetPhaseId);
            string directionLabel = PhaseNextPhaseServiceSupport.DescribeDirection(normalizedRequest.Direction);
            string requestKindLabel = DescribeRequestKind(normalizedRequest.Kind);

            DebugUtility.Log<PhaseNextPhaseService>(
                $"[OBS][PhaseFlow][Request] NavigationRequested kind='{requestKindLabel}' direction='{directionLabel}' owner='PhaseNextPhaseService' reason='{normalizedRequest.Reason}'.",
                DebugUtility.Colors.Info);

            await _gate.WaitAsync(ct);
            try
            {
                GameplayStartSnapshot currentSnapshot = ResolveCurrentGameplaySnapshotOrFail(normalizedRequest.Reason);
                PhaseCatalogNavigationPlan navigationPlan = _selectionService.SelectPhase(normalizedRequest, currentSnapshot);
                if (navigationPlan.Outcome != PhaseNavigationOutcome.Changed)
                {
                    return BuildBlockedResult(navigationPlan);
                }

                GameplayPhaseFlowService gameplayPhaseFlowService = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<GameplayPhaseFlowService>(
                    "GameplayPhaseFlowService");
                PhaseDefinitionSelectedEvent phaseSelectedEvent = gameplayPhaseFlowService.PublishPhaseDefinitionSelected(
                    navigationPlan.TargetPhaseRef,
                    currentSnapshot.MacroRouteId,
                    currentSnapshot.MacroRouteRef,
                    navigationPlan.Reason);

                bool forceFullReload = navigationPlan.RequestKind == PhaseNavigationRequestKind.RestartCatalog;

                string targetSceneName = SceneManager.GetActiveScene().name;
                PhaseNavigationSelectionContext selectionContext = new PhaseNavigationSelectionContext(
                    currentSnapshot,
                    navigationPlan.TargetPhaseRef,
                    phaseSelectedEvent,
                    navigationPlan.Reason,
                    targetSceneName,
                    navigationPlan.Direction,
                    forceFullReload);

                PhaseNavigationCompositionContext compositionContext = await _compositionService.ApplyNavigationPhaseAsync(selectionContext, ct);
                await _handoffService.CompleteIntroHandoffAsync(compositionContext, ct);

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][PhaseFlow][Completion] PhaseNavigationCompleted kind='{requestKindLabel}' outcome='{navigationPlan.Outcome}' direction='{directionLabel}' from='{PhaseNextPhaseServiceSupport.DescribePhase(selectionContext.CurrentPhaseRef)}' to='{PhaseNextPhaseServiceSupport.DescribePhase(selectionContext.TargetPhaseRef)}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{navigationPlan.Reason}'.",
                    DebugUtility.Colors.Success);

                return BuildChangedResult(navigationPlan, selectionContext);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<PhaseNavigationResult> RestartCatalogInternalAsync(string reason, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = PhaseNextPhaseServiceSupport.NormalizeReason(reason);
            DebugUtility.Log<PhaseNextPhaseService>(
                $"[OBS][PhaseFlow] RestartCatalogRequested owner='PhaseNextPhaseService' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            IPhaseCatalogRuntimeStateService runtimeStateService = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<IPhaseCatalogRuntimeStateService>("IPhaseCatalogRuntimeStateService");
            PhaseDefinitionAsset currentCommittedPhase = runtimeStateService.CurrentCommitted;
            PhaseDefinitionAsset initialPhase = runtimeStateService.Catalog.ResolveInitialOrFail();

            DebugUtility.Log<PhaseNextPhaseService>(
                $"[OBS][PhaseFlow] RestartCatalogResolvedInitial currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(currentCommittedPhase)}' initialPhase='{PhaseNextPhaseServiceSupport.DescribePhase(initialPhase)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            if (currentCommittedPhase != null &&
                initialPhase != null &&
                string.Equals(currentCommittedPhase.PhaseId.Value, initialPhase.PhaseId.Value, StringComparison.OrdinalIgnoreCase))
            {
                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][PhaseFlow] RestartCatalogForcedReapply current='{PhaseNextPhaseServiceSupport.DescribePhase(currentCommittedPhase)}' target='{PhaseNextPhaseServiceSupport.DescribePhase(initialPhase)}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }

            PhaseNavigationRequest restartRequest = PhaseNavigationRequest.RestartCatalog(initialPhase.PhaseId.Value, normalizedReason);
            PhaseNavigationResult result = await NavigateAsync(restartRequest, ct);

            DebugUtility.Log<PhaseNextPhaseService>(
                $"[OBS][PhaseFlow] RestartCatalogCompleted outcome='{result.Outcome}' targetPhase='{PhaseNextPhaseServiceSupport.DescribePhase(result.TargetPhaseRef)}' reason='{normalizedReason}'.",
                result.Outcome == PhaseNavigationOutcome.Changed ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            return result;
        }

        private static GameplayStartSnapshot ResolveCurrentGameplaySnapshotOrFail(string reason)
        {
            if (!PhaseNextPhaseServiceSupport.TryResolveGlobal<IRestartContextService>(out var restartContextService) ||
                restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseService),
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
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Phase navigation requer um gameplay snapshot valido. reason='{reason}'.");
            }

            return currentSnapshot;
        }

        private static PhaseNavigationResult BuildChangedResult(PhaseCatalogNavigationPlan navigationPlan, PhaseNavigationSelectionContext selectionContext)
        {
            return new PhaseNavigationResult(
                navigationPlan.Request,
                navigationPlan.Outcome,
                navigationPlan.CurrentCommitted,
                navigationPlan.CatalogName,
                navigationPlan.TraversalMode,
                navigationPlan.WasWrapped,
                selectionContext);
        }

        private static PhaseNavigationResult BuildBlockedResult(PhaseCatalogNavigationPlan navigationPlan)
        {
            return new PhaseNavigationResult(
                navigationPlan.Request,
                navigationPlan.Outcome,
                navigationPlan.CurrentCommitted,
                navigationPlan.CatalogName,
                navigationPlan.TraversalMode,
                false,
                default);
        }

        private static string DescribeRequestKind(PhaseNavigationRequestKind kind)
        {
            return kind == PhaseNavigationRequestKind.RestartCatalog
                ? "RestartCatalog"
                : kind == PhaseNavigationRequestKind.Previous
                    ? "Previous"
                    : kind == PhaseNavigationRequestKind.Next
                        ? "Next"
                        : "Specific";
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
            PhaseNavigationDirection direction,
            bool forceFullReload)
        {
            CurrentSnapshot = currentSnapshot;
            TargetPhaseRef = targetPhaseRef;
            PhaseSelectedEvent = phaseSelectedEvent;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            TargetSceneName = string.IsNullOrWhiteSpace(targetSceneName) ? string.Empty : targetSceneName.Trim();
            Direction = direction;
            ForceFullReload = forceFullReload;
        }

        public GameplayStartSnapshot CurrentSnapshot { get; }
        public PhaseDefinitionAsset CurrentPhaseRef => CurrentSnapshot.PhaseDefinitionRef;
        public PhaseDefinitionAsset TargetPhaseRef { get; }
        public PhaseDefinitionSelectedEvent PhaseSelectedEvent { get; }
        public string Reason { get; }
        public string TargetSceneName { get; }
        public PhaseNavigationDirection Direction { get; }
        public bool ForceFullReload { get; }

        public int SelectionVersion => PhaseSelectedEvent.SelectionVersion;
        public string TargetIntroContentId => TargetPhaseRef != null ? PhaseDefinitionId.BuildCanonicalIntroContentId(TargetPhaseRef.PhaseId) : string.Empty;
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
        PhaseCatalogNavigationPlan SelectPhase(PhaseNavigationRequest request, GameplayStartSnapshot currentSnapshot);
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
        public PhaseCatalogNavigationPlan SelectPhase(PhaseNavigationRequest request, GameplayStartSnapshot currentSnapshot)
        {
            if (!currentSnapshot.IsValid ||
                !currentSnapshot.HasPhaseDefinitionRef ||
                currentSnapshot.PhaseDefinitionRef == null ||
                !currentSnapshot.PhaseDefinitionRef.PhaseId.IsValid ||
                currentSnapshot.MacroRouteRef == null ||
                !currentSnapshot.MacroRouteId.IsValid ||
                currentSnapshot.MacroRouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Phase navigation requires a valid gameplay snapshot.");
            }

            string normalizedReason = PhaseNextPhaseServiceSupport.NormalizeReason(request.Reason);
            PhaseNavigationRequest normalizedRequest = new PhaseNavigationRequest(request.Kind, request.Direction, normalizedReason, request.TargetPhaseId);
            string requestKindLabel = DescribeRequestKind(normalizedRequest.Kind);

            IPhaseCatalogNavigationService catalogNavigationService = PhaseNextPhaseServiceSupport.ResolveRequiredGlobal<IPhaseCatalogNavigationService>("IPhaseCatalogNavigationService");
            IPhaseDefinitionCatalog catalog = catalogNavigationService.Catalog;
            PhaseDefinitionAsset currentPhaseRef = catalogNavigationService.CurrentCommitted;
            if (currentPhaseRef == null || !currentPhaseRef.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseSelectionService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Catalog runtime state requires a committed current phase. reason='{normalizedReason}'.");
            }

            string catalogName = PhaseNextPhaseServiceSupport.DescribeCatalog(catalog);
            PhaseCatalogTraversalMode traversalMode = catalogNavigationService.TraversalMode;
            DebugUtility.Log<PhaseNextPhaseSelectionService>(
                $"[OBS][PhaseFlow][Selection] PhaseNavigationSelectionReceived kind='{requestKindLabel}' direction='{PhaseNextPhaseServiceSupport.DescribeDirection(normalizedRequest.Direction)}' currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(currentPhaseRef)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            PhaseCatalogNavigationPlan navigationPlan = normalizedRequest.Kind switch
            {
                PhaseNavigationRequestKind.RestartCatalog => catalogNavigationService.RestartCatalog(normalizedReason),
                PhaseNavigationRequestKind.Specific => ResolveSpecificPhasePlan(
                    normalizedRequest,
                    catalogNavigationService,
                    catalogName,
                    traversalMode),
                _ => normalizedRequest.Direction == PhaseNavigationDirection.Previous
                    ? catalogNavigationService.ResolvePrevious(normalizedReason)
                    : catalogNavigationService.ResolveNext(normalizedReason)
            };

            if (navigationPlan.Outcome == PhaseNavigationOutcome.Changed)
            {
                catalogNavigationService.Commit(navigationPlan);

                if (navigationPlan.WasWrapped)
                {
                    DebugUtility.Log<PhaseNextPhaseSelectionService>(
                        $"[OBS][PhaseFlow] NavigationWrapped action='{PhaseNextPhaseServiceSupport.DescribeDirection(navigationPlan.Direction)}' from='{PhaseNextPhaseServiceSupport.DescribePhase(currentPhaseRef)}' to='{PhaseNextPhaseServiceSupport.DescribePhase(navigationPlan.TargetPhaseRef)}' traversalMode='{traversalMode}' catalog='{catalogName}' reason='{navigationPlan.Reason}'.",
                        DebugUtility.Colors.Info);
                }
            }

            return navigationPlan;
        }

        private static PhaseCatalogNavigationPlan ResolveSpecificPhasePlan(
            PhaseNavigationRequest request,
            IPhaseCatalogNavigationService catalogNavigationService,
            string catalogName,
            PhaseCatalogTraversalMode traversalMode)
        {
            string specificPhaseId = request.TargetPhaseId;
            if (string.IsNullOrWhiteSpace(specificPhaseId))
            {
                DebugUtility.LogWarning<PhaseNextPhaseSelectionService>(
                    $"[WARN][PhaseFlow] NavigationBlocked outcome='SpecificPhaseIdInvalid' action='Specific' currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(catalogNavigationService.CurrentCommitted)}' catalog='{catalogName}' reason='empty_phaseId'.");
                return PhaseCatalogNavigationPlan.CreateBlocked(
                    request,
                    PhaseNavigationOutcome.SpecificPhaseIdInvalid,
                    catalogNavigationService.CurrentCommitted,
                    traversalMode,
                    catalogName);
            }

            if (!catalogNavigationService.Catalog.TryGet(specificPhaseId, out _))
            {
                DebugUtility.LogWarning<PhaseNextPhaseSelectionService>(
                    $"[WARN][PhaseFlow] NavigationBlocked outcome='SpecificPhaseMissing' action='Specific' currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(catalogNavigationService.CurrentCommitted)}' targetPhaseId='{PhaseDefinitionId.Normalize(specificPhaseId)}' catalog='{catalogName}' reason='phase_not_found'.");
                return PhaseCatalogNavigationPlan.CreateBlocked(
                    request,
                    PhaseNavigationOutcome.SpecificPhaseMissing,
                    catalogNavigationService.CurrentCommitted,
                    traversalMode,
                    catalogName);
            }

            PhaseCatalogNavigationPlan navigationPlan = catalogNavigationService.ResolveSpecificPhase(specificPhaseId, request.Reason);
            if (!navigationPlan.IsChanged)
            {
                if (navigationPlan.Outcome == PhaseNavigationOutcome.TargetAlreadyCurrent)
                {
                    DebugUtility.LogWarning<PhaseNextPhaseSelectionService>(
                        $"[WARN][PhaseFlow] NavigationBlocked outcome='TargetAlreadyCurrent' action='Specific' currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(catalogNavigationService.CurrentCommitted)}' targetPhaseId='{PhaseDefinitionId.Normalize(specificPhaseId)}' catalog='{catalogName}' reason='target_equals_current'.");
                }

                return navigationPlan;
            }

            return navigationPlan;
        }

        private static string DescribeRequestKind(PhaseNavigationRequestKind kind)
        {
            return kind == PhaseNavigationRequestKind.RestartCatalog
                ? "RestartCatalog"
                : kind == PhaseNavigationRequestKind.Previous
                    ? "Previous"
                    : kind == PhaseNavigationRequestKind.Next
                        ? "Next"
                        : "Specific";
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
                $"[OBS][PhaseFlow][Composition] PhaseNavigationCompositionExecutorConfirmed executor='SceneCompositionExecutor' owner='PhaseNextPhaseCompositionService' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionRequest applyRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                selectionContext.TargetPhaseRef,
                selectionContext.Reason,
                selectionContext.PhaseSelectedEvent.SelectionSignature,
                selectionContext.ForceFullReload);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][PhaseFlow][Composition] PhaseNavigationCompositionStarted direction='{PhaseNextPhaseServiceSupport.DescribeDirection(selectionContext.Direction)}' from='{currentPhaseId}' to='{targetPhaseId}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] scenesToUnload=[{string.Join(",", applyRequest.ScenesToUnload)}] correlationId='{applyRequest.CorrelationId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            await sceneCompositionExecutor.ApplyAsync(applyRequest, ct);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][PhaseFlow][Composition] PhaseNavigationCompositionClearedPrevious phaseId='{currentPhaseId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][PhaseFlow][Composition] PhaseNavigationCompositionLegacyBridgeBypassed path='phase_enabled_navigation' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                selectionContext.TargetPhaseRef,
                applyRequest.ScenesToLoad,
                applyRequest.ActiveScene,
                source: "PhaseDefinitionNavigation");

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][PhaseFlow][Composition] PhaseNavigationCompositionReadModelCommitted owner='PhaseContentSceneRuntimeApplier' phaseId='{targetPhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' reason='{selectionContext.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<PhaseNextPhaseCompositionService>(
                $"[OBS][PhaseFlow][Composition] PhaseNavigationCompositionCompleted currentPhaseRef='{currentPhaseId}' targetPhaseRef='{targetPhaseId}' phaseId='{targetPhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}'.",
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
                $"[OBS][PhaseFlow][Handoff] PhaseNavigationIntroSessionResolved owner='IntroStageSessionService' direction='{PhaseNextPhaseServiceSupport.DescribeDirection(selectionContext.Direction)}' targetPhaseRef='{targetPhaseName}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}' signature='{introSession.SessionSignature}' hasIntroStage='{introSession.HasIntroStage}'.",
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
                $"[OBS][PhaseFlow][Handoff] PhaseNavigationHandoffCompleted current='{targetPhaseId}' routeId='{selectionContext.CurrentSnapshot.MacroRouteId}' v='{selectionContext.SelectionVersion}' reason='{selectionContext.Reason}' introSkipped='{introCompletedEvent.WasSkipped.ToString().ToLowerInvariant()}' introReason='{PhaseNextPhaseServiceSupport.NormalizeReason(introCompletedEvent.Reason)}' source='{introCompletedEvent.Source}'.",
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
