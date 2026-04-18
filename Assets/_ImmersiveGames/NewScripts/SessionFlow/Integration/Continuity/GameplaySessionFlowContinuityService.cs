using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionFlowContinuityService : IGameplaySessionFlowContinuityService
    {
        private readonly IRestartContextService _restartContextService;
        private readonly ISessionIntegrationNavigationHandoffService _navigationHandoffService;
        private readonly IPhaseResetExecutor _phaseResetExecutor;
        private readonly IPhaseDefinitionCatalog _phaseDefinitionCatalog;

        public GameplaySessionFlowContinuityService(
            ISessionIntegrationNavigationHandoffService navigationHandoffService,
            IRestartContextService restartContextService,
            IPhaseResetExecutor phaseResetExecutor,
            IPhaseDefinitionCatalog phaseDefinitionCatalog = null)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _navigationHandoffService = navigationHandoffService ?? throw new ArgumentNullException(nameof(navigationHandoffService));
            _phaseResetExecutor = phaseResetExecutor ?? throw new ArgumentNullException(nameof(phaseResetExecutor));
            _phaseDefinitionCatalog = phaseDefinitionCatalog;
        }

        public Task RestartGameplayAsync(RunRestart restart, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(restart.Reason) ? "GameplaySessionFlow/RestartGameplay" : restart.Reason.Trim();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='RestartGameplay' scope='run_restart' source='{restart.Source}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            return RestartCurrentGameplayInternalAsync(normalizedReason, ct);
        }

        public Task RestartFromFirstPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            return RestartFromFirstPhaseInternalAsync(reason, ct);
        }

        private async Task RestartCurrentGameplayInternalAsync(string reason, CancellationToken ct)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/RestartGameplay" : reason.Trim();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='RestartGameplay' scope='current_phase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffDispatch action='RestartGameplay' scope='current_phase' reason='{normalizedReason}' target='Navigation'.",
                DebugUtility.Colors.Info);
            await RestartLastGameplayOrDefaultAsync(normalizedReason, ct);

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffCompleted action='RestartGameplay' scope='current_phase' reason='{normalizedReason}' target='Navigation'.",
                DebugUtility.Colors.Success);
        }

        private async Task RestartFromFirstPhaseInternalAsync(string reason, CancellationToken ct)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/RestartFromFirstPhase" : reason.Trim();
            if (_phaseDefinitionCatalog == null)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] RestartFromFirstPhase requires PhaseDefinitionCatalog on a phase-enabled route/context. reason='{normalizedReason}'.");
            }

            PhaseDefinitionAsset initialPhaseDefinitionRef = _phaseDefinitionCatalog.ResolveInitialOrFail();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='RestartFromFirstPhase' scope='first_phase' initialPhaseId='{initialPhaseDefinitionRef.PhaseId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffDispatch action='RestartFromFirstPhase' scope='first_phase' initialPhaseRef='{initialPhaseDefinitionRef.name}' reason='{normalizedReason}' target='Navigation'.",
                DebugUtility.Colors.Info);
            _restartContextService.Clear(normalizedReason);
            await StartGameplayDefaultAsync(normalizedReason, ct);

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffCompleted action='RestartFromFirstPhase' scope='first_phase' reason='{normalizedReason}' target='Navigation'.",
                DebugUtility.Colors.Success);
        }

        public async Task ResetCurrentPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/ResetCurrentPhase" : reason.Trim();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='ResetCurrentPhase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffDispatch action='ResetCurrentPhase' reason='{normalizedReason}' target='PhaseResetOperational'.",
                DebugUtility.Colors.Info);
            await ResetCurrentPhaseInternalAsync(normalizedReason, ct);

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffCompleted action='ResetCurrentPhase' reason='{normalizedReason}' target='PhaseResetOperational'.",
                DebugUtility.Colors.Success);
        }

        public Task<PhaseNavigationResult> NextPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            return NavigatePhaseAsync(PhaseNavigationRequest.Next(reason), ct);
        }

        public Task<PhaseNavigationResult> PreviousPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            return NavigatePhaseAsync(PhaseNavigationRequest.Previous(reason), ct);
        }

        public async Task<PhaseNavigationResult> NavigatePhaseAsync(PhaseNavigationRequest request, CancellationToken ct = default)
        {
            PhaseNavigationRequest normalizedRequest = request.Direction == PhaseNavigationDirection.Previous
                ? PhaseNavigationRequest.Previous(request.Reason)
                : request.Direction == PhaseNavigationDirection.Next
                    ? PhaseNavigationRequest.Next(request.Reason)
                    : PhaseNavigationRequest.Specific(request.TargetPhaseId, request.Reason);
            string normalizedReason = string.IsNullOrWhiteSpace(normalizedRequest.Reason) ? "GameplaySessionFlow/NavigationPhase" : normalizedRequest.Reason;
            string action = PhaseNextPhaseServiceSupport.DescribeDirection(normalizedRequest.Direction);

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='{action}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            ct.ThrowIfCancellationRequested();

            IPhaseNextPhaseService nextPhaseService = ResolveRequiredPhaseNextPhaseService(normalizedReason);

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffDispatch action='{action}' rail='phase-local' reason='{normalizedReason}' target='PhaseNextPhaseService'.",
                DebugUtility.Colors.Info);

            PhaseNavigationResult result = await nextPhaseService.NavigateAsync(normalizedRequest, ct);

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffCompleted outcome='{result.Outcome}' action='{action}' reason='{normalizedReason}' target='PhaseNextPhaseService'.",
                result.Outcome == PhaseNavigationOutcome.Changed ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
            return result;
        }

        public async Task ExitToMenuAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/ExitToMenu" : reason.Trim();

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='ExitToMenu' reason='{normalizedReason}' handoff='Navigation'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await DispatchExitToMenuHandoffAsync(normalizedReason, ct);

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] HandoffCompleted action='ExitToMenu' reason='{normalizedReason}' handoff='Navigation'.",
                DebugUtility.Colors.Success);
        }

        private async Task DispatchExitToMenuHandoffAsync(string reason, CancellationToken ct)
        {
            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Handoff] ExitToMenuDispatch action='ExitToMenu' reason='{reason}' target='Navigation'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _navigationHandoffService.RequestExitToMenuAsync(reason, nameof(GameplaySessionFlowContinuityService), ct);
        }

        private async Task RestartLastGameplayOrDefaultAsync(string reason, CancellationToken ct)
        {
            if (_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) &&
                snapshot.IsValid &&
                snapshot.MacroRouteId.IsValid)
            {
                await _navigationHandoffService.RequestStartGameplayRouteAsync(
                    snapshot.MacroRouteId,
                    reason,
                    nameof(GameplaySessionFlowContinuityService),
                    ct);
                return;
            }

            await StartGameplayDefaultAsync(reason, ct);
        }

        private async Task StartGameplayDefaultAsync(string reason, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            SceneRouteId gameplayRouteId = _navigationHandoffService.ResolveGameplayRouteIdOrFail(reason, nameof(GameplaySessionFlowContinuityService));
            if (!gameplayRouteId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] Canonical gameplay route resolution returned an invalid routeId. reason='{reason}'.");
            }

            await _navigationHandoffService.RequestStartGameplayRouteAsync(
                gameplayRouteId,
                reason,
                nameof(GameplaySessionFlowContinuityService),
                ct);
        }

        private async Task ResetCurrentPhaseInternalAsync(string reason, CancellationToken ct)
        {
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasPhaseDefinitionRef ||
                snapshot.PhaseDefinitionRef == null ||
                snapshot.MacroRouteRef == null ||
                !snapshot.MacroRouteId.IsValid ||
                string.IsNullOrWhiteSpace(snapshot.PhaseSignature))
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] ResetCurrentPhaseAsync requires a valid current gameplay snapshot. reason='{reason}'.");
            }

            DebugUtility.Log<GameplaySessionFlowContinuityService>(
                $"[OBS][GameplaySessionFlow][Continuity] ResetCurrentPhaseRequested rail='phase' phaseRef='{snapshot.PhaseDefinitionRef.name}' routeId='{snapshot.MacroRouteId}' v='{snapshot.SelectionVersion}' reason='{reason}' phaseSignature='{snapshot.PhaseSignature}'.",
                DebugUtility.Colors.Info);

            PhaseResetContext resetContext = new PhaseResetContext(
                snapshot.PhaseDefinitionRef,
                snapshot.MacroRouteId,
                new PhaseContextSignature(snapshot.PhaseSignature),
                snapshot.PhaseSignature);

            await _phaseResetExecutor.ResetPhaseAsync(resetContext, reason, ct);
        }

        private static IPhaseNextPhaseService ResolveRequiredPhaseNextPhaseService(string reason)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] DependencyManager.Provider is null while resolving NextPhase rail. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseService>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowContinuityService),
                    $"[FATAL][H1][GameplaySessionFlow] Missing required phase-local NextPhase rail. reason='{reason}'.");
            }

            return service;
        }
    }
}

