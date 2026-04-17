using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
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
        private readonly IPhaseCatalogRuntimeStateService _phaseCatalogRuntimeStateService;

        public GameplaySessionRunResetService(
            IRestartContextService restartContextService,
            IGameNavigationService navigationService,
            IPhaseCatalogRuntimeStateService phaseCatalogRuntimeStateService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _phaseCatalogRuntimeStateService = phaseCatalogRuntimeStateService ?? throw new ArgumentNullException(nameof(phaseCatalogRuntimeStateService));
        }

        public async Task AcceptAsync(GameplayRunResetRequest request, CancellationToken ct = default)
        {
            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetRequestReceived kind='{request.Kind}' reason='{Normalize(request.Reason)}' selectedContinuation='{request.Selection.SelectedContinuation}' targetPhase='{DescribePhase(request.TargetPhaseRef)}'.",
                DebugUtility.Colors.Info);

            if (!request.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionRunResetService),
                    $"[FATAL][H1][GameplaySessionFlow][RunReset] Run reset request invalido recebido. kind='{request.Kind}' reason='{Normalize(request.Reason)}' targetPhase='{DescribePhase(request.TargetPhaseRef)}'.");
            }

            ct.ThrowIfCancellationRequested();

            GameplayStartSnapshot baseSnapshot = ResolveBaseSnapshotOrFail(request);
            PhaseDefinitionAsset targetPhaseRef = request.TargetPhaseRef;
            SceneRouteId routeId = ResolveGameplayRouteId(baseSnapshot);
            string explicitReason = request.Reason;

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetExecutionStarted kind='{request.Kind}' reason='{explicitReason}' routeId='{routeId}' currentPhase='{DescribePhase(baseSnapshot.PhaseDefinitionRef)}' targetPhase='{DescribePhase(targetPhaseRef)}'.",
                DebugUtility.Colors.Info);

            ClearRestartContext(explicitReason);

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetContextClosed kind='{request.Kind}' reason='{explicitReason}' restartContextCleared='true'.",
                DebugUtility.Colors.Info);

            ApplyExplicitTargetPhaseState(targetPhaseRef, baseSnapshot, explicitReason);

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetTargetApplied kind='{request.Kind}' routeId='{routeId}' targetPhase='{DescribePhase(targetPhaseRef)}' currentCommitted='{DescribePhase(_phaseCatalogRuntimeStateService.CurrentCommitted)}' pendingTarget='{DescribePhase(_phaseCatalogRuntimeStateService.PendingTarget)}' reason='{explicitReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetGameplayReentryRequested kind='{request.Kind}' routeId='{routeId}' targetScene='GameplayScene' targetPhase='{DescribePhase(targetPhaseRef)}' reason='{explicitReason}'.",
                DebugUtility.Colors.Info);

            await _navigationService.StartGameplayRouteAsync(routeId, SceneTransitionPayload.Empty, explicitReason);

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] RunResetMacroExecuted kind='{request.Kind}' routeId='{routeId}' targetPhase='{DescribePhase(targetPhaseRef)}' reason='{explicitReason}'.",
                DebugUtility.Colors.Success);

            string completionLabel = request.Kind == RunContinuationKind.ResetRun
                ? "ResetRunCompleted"
                : "RetryCompleted";

            DebugUtility.Log<GameplaySessionRunResetService>(
                $"[OBS][GameplaySessionFlow][RunReset] {completionLabel} routeId='{routeId}' targetPhase='{DescribePhase(targetPhaseRef)}' reason='{explicitReason}'.",
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

        private SceneRouteId ResolveGameplayRouteId(GameplayStartSnapshot snapshot)
        {
            return snapshot.MacroRouteId.IsValid
                ? snapshot.MacroRouteId
                : _navigationService.ResolveGameplayRouteIdOrFail();
        }

        private void ClearRestartContext(string reason)
        {
            _restartContextService.Clear(reason);
        }

        private void ApplyExplicitTargetPhaseState(PhaseDefinitionAsset targetPhaseRef, GameplayStartSnapshot baseSnapshot, string reason)
        {
            _phaseCatalogRuntimeStateService.SetPendingTarget(targetPhaseRef, reason);
            _phaseCatalogRuntimeStateService.CommitCurrentTarget(targetPhaseRef, reason);

            GameplayStartSnapshot targetSnapshot = new GameplayStartSnapshot(
                targetPhaseRef,
                baseSnapshot.MacroRouteId,
                baseSnapshot.MacroRouteRef,
                PhaseDefinitionId.BuildCanonicalIntroContentId(targetPhaseRef.PhaseId),
                reason,
                0,
                string.Empty);

            _restartContextService.UpdateGameplayStartSnapshot(targetSnapshot);
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
