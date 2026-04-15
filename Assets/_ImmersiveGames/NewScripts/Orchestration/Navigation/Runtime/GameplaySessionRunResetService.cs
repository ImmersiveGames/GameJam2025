using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionRunResetService : IGameplaySessionRunResetService
    {
        private readonly IRestartContextService _restartContextService;
        private readonly IGameNavigationService _navigationService;
        private readonly IPhaseDefinitionCatalog _phaseDefinitionCatalog;
        private readonly IPhaseCatalogRuntimeStateService _phaseCatalogRuntimeStateService;
        private readonly IRunContinuationOwnershipService _runContinuationOwnershipService;
        private readonly IPostRunResultService _postRunResultService;

        public GameplaySessionRunResetService(
            IRestartContextService restartContextService,
            IGameNavigationService navigationService,
            IPhaseDefinitionCatalog phaseDefinitionCatalog,
            IPhaseCatalogRuntimeStateService phaseCatalogRuntimeStateService,
            IRunContinuationOwnershipService runContinuationOwnershipService,
            IPostRunResultService postRunResultService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _phaseDefinitionCatalog = phaseDefinitionCatalog ?? throw new ArgumentNullException(nameof(phaseDefinitionCatalog));
            _phaseCatalogRuntimeStateService = phaseCatalogRuntimeStateService ?? throw new ArgumentNullException(nameof(phaseCatalogRuntimeStateService));
            _runContinuationOwnershipService = runContinuationOwnershipService ?? throw new ArgumentNullException(nameof(runContinuationOwnershipService));
            _postRunResultService = postRunResultService ?? throw new ArgumentNullException(nameof(postRunResultService));
        }

        public async Task AcceptAsync(GameplayRunResetRequest request, CancellationToken ct = default)
        {
            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetRequestReceived kind='{request.Kind}' targetPolicy='{request.TargetPolicy}' reason='{Normalize(request.Reason)}' selectedContinuation='{request.Selection.SelectedContinuation}' nextState='{Normalize(request.Selection.NextState)}'.",
                DebugUtility.Colors.Info);

            if (!request.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionRunResetService),
                    $"[FATAL][H1][GameplaySessionFlow][RunReset] Run reset request invalido recebido. kind='{request.Kind}' targetPolicy='{request.TargetPolicy}' reason='{Normalize(request.Reason)}'.");
            }

            ct.ThrowIfCancellationRequested();

            GameplayStartSnapshot baseSnapshot = ResolveBaseSnapshotOrFail(request);
            PhaseDefinitionAsset targetPhaseRef = ResolveTargetPhaseOrFail(request, baseSnapshot);
            SceneRouteId routeId = ResolveGameplayRouteId(baseSnapshot);
            string normalizedReason = NormalizeReason(request);

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetExecutionStarted kind='{request.Kind}' targetPolicy='{request.TargetPolicy}' reason='{normalizedReason}' routeId='{routeId}' currentPhase='{DescribePhase(baseSnapshot.PhaseDefinitionRef)}' targetPhase='{DescribePhase(targetPhaseRef)}'.",
                DebugUtility.Colors.Info);

            CloseRunLevelContexts(request, normalizedReason);

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetContextClosed kind='{request.Kind}' reason='{normalizedReason}' currentCleared='true' lastPreserved='true'.",
                DebugUtility.Colors.Info);

            FreezeTargetPhase(targetPhaseRef, baseSnapshot, normalizedReason);

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetTargetResolved kind='{request.Kind}' targetPolicy='{request.TargetPolicy}' routeId='{routeId}' targetPhase='{DescribePhase(targetPhaseRef)}' currentCommitted='{DescribePhase(_phaseCatalogRuntimeStateService.CurrentCommitted)}' pendingTarget='{DescribePhase(_phaseCatalogRuntimeStateService.PendingTarget)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetGameplayReentryRequested kind='{request.Kind}' routeId='{routeId}' targetScene='GameplayScene' targetPhase='{DescribePhase(targetPhaseRef)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            await _navigationService.StartGameplayRouteAsync(routeId, SceneTransitionPayload.Empty, normalizedReason);

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetMacroExecuted kind='{request.Kind}' routeId='{routeId}' targetPhase='{DescribePhase(targetPhaseRef)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);

            string completionLabel = request.Kind == RunContinuationKind.ResetRun
                ? "ResetRunCompleted"
                : "RetryCompleted";

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] {completionLabel} routeId='{routeId}' targetPhase='{DescribePhase(targetPhaseRef)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        private GameplayStartSnapshot ResolveBaseSnapshotOrFail(GameplayRunResetRequest request)
        {
            if (_restartContextService.TryGetCurrent(out GameplayStartSnapshot current) && current.IsValid)
            {
                return current;
            }

            if (_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot last) && last.IsValid)
            {
                return last;
            }

            HardFailFastH1.Trigger(typeof(GameplaySessionRunResetService),
                $"[FATAL][H1][GameplaySessionFlow][RunReset] Missing gameplay start snapshot for run reset. kind='{request.Kind}' reason='{Normalize(request.Reason)}'.");
            return GameplayStartSnapshot.Empty;
        }

        private PhaseDefinitionAsset ResolveTargetPhaseOrFail(GameplayRunResetRequest request, GameplayStartSnapshot snapshot)
        {
            PhaseDefinitionAsset targetPhaseRef = request.TargetPolicy switch
            {
                GameplayRunResetTargetPolicy.CurrentCatalogPhase => snapshot.PhaseDefinitionRef != null
                    ? snapshot.PhaseDefinitionRef
                    : _phaseCatalogRuntimeStateService.CurrentCommitted,
                GameplayRunResetTargetPolicy.FirstCatalogPhase => _phaseDefinitionCatalog.ResolveInitialOrFail(),
                _ => null,
            };

            if (targetPhaseRef == null || !targetPhaseRef.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionRunResetService),
                    $"[FATAL][H1][GameplaySessionFlow][RunReset] Could not resolve target phase. kind='{request.Kind}' targetPolicy='{request.TargetPolicy}' reason='{Normalize(request.Reason)}'.");
            }

            return targetPhaseRef;
        }

        private SceneRouteId ResolveGameplayRouteId(GameplayStartSnapshot snapshot)
        {
            return snapshot.MacroRouteId.IsValid
                ? snapshot.MacroRouteId
                : _navigationService.ResolveGameplayRouteIdOrFail();
        }

        private void CloseRunLevelContexts(GameplayRunResetRequest request, string normalizedReason)
        {
            _runContinuationOwnershipService.ClearCurrentContext($"RunReset/{request.Kind}");
            _postRunResultService.Clear($"RunReset/{request.Kind}");
            _restartContextService.Clear(normalizedReason);
        }

        private void FreezeTargetPhase(PhaseDefinitionAsset targetPhaseRef, GameplayStartSnapshot baseSnapshot, string reason)
        {
            _phaseCatalogRuntimeStateService.SetPendingTarget(targetPhaseRef, reason);
            _phaseCatalogRuntimeStateService.CommitCurrentTarget(targetPhaseRef, reason);

            GameplayStartSnapshot targetSnapshot = new GameplayStartSnapshot(
                targetPhaseRef,
                baseSnapshot.MacroRouteId,
                baseSnapshot.MacroRouteRef,
                targetPhaseRef.BuildCanonicalIntroContentId(),
                reason,
                0,
                string.Empty);

            _restartContextService.UpdateGameplayStartSnapshot(targetSnapshot);
        }

        private static string NormalizeReason(GameplayRunResetRequest request)
        {
            return string.IsNullOrWhiteSpace(request.Reason)
                ? $"RunDecision/{request.Kind}"
                : request.Reason.Trim();
        }

        private static string DescribePhase(PhaseDefinitionAsset phaseDefinition)
        {
            return phaseDefinition != null && phaseDefinition.PhaseId.IsValid
                ? phaseDefinition.PhaseId.Value
                : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
