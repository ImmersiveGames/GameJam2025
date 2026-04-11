using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostLevelActionsService : IPostLevelActionsService
    {
        private readonly IRestartContextService _restartContextService;
        private readonly IGameNavigationService _navigationService;
        private readonly IPhaseDefinitionCatalog _phaseDefinitionCatalog;

        public PostLevelActionsService(
            IGameNavigationService navigationService,
            IRestartContextService restartContextService,
            IPhaseDefinitionCatalog phaseDefinitionCatalog = null)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _phaseDefinitionCatalog = phaseDefinitionCatalog;
        }

        public Task RestartLevelAsync(string reason = null, CancellationToken ct = default)
        {
            return RestartCurrentLevelInternalAsync(reason, ct);
        }

        public Task RestartFromFirstLevelAsync(string reason = null, CancellationToken ct = default)
        {
            return RestartFromFirstLevelInternalAsync(reason, ct);
        }

        private async Task RestartCurrentLevelInternalAsync(string reason, CancellationToken ct)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/RestartGameplay" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='RestartGameplay' scope='current_phase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorStarted action='RestartGameplay' scope='current_phase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            await RestartLastGameplayOrDefaultAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='RestartGameplay' scope='current_phase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        private async Task RestartFromFirstLevelInternalAsync(string reason, CancellationToken ct)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/RestartFromFirstPhase" : reason.Trim();
            if (_phaseDefinitionCatalog == null)
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][GameplaySessionFlow] RestartFromFirstPhase requires PhaseDefinitionCatalog on a phase-enabled route/context. reason='{normalizedReason}'.");
            }

            PhaseDefinitionAsset initialPhaseDefinitionRef = _phaseDefinitionCatalog.ResolveInitialOrFail();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='RestartFromFirstPhase' scope='first_phase' initialPhaseId='{initialPhaseDefinitionRef.PhaseId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorStarted action='RestartFromFirstPhase' scope='first_phase' initialPhaseRef='{initialPhaseDefinitionRef.name}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            _restartContextService.Clear(normalizedReason);
            await StartGameplayDefaultAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='RestartFromFirstPhase' scope='first_phase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        public async Task ResetCurrentLevelAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/ResetCurrentPhase" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='ResetCurrentPhase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorStarted action='ResetCurrentPhase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            await ResetCurrentLevelInternalAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='ResetCurrentPhase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        public async Task NextLevelAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/NextPhase" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='NextPhase' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            ct.ThrowIfCancellationRequested();

            IPhaseNextPhaseService nextPhaseService = ResolveRequiredPhaseNextPhaseService(normalizedReason);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorStarted action='NextPhase' rail='phase-local' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            await nextPhaseService.NextPhaseAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='NextPhase' rail='phase-local' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        public async Task ExitToMenuAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/ExitToMenu" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='ExitToMenu' reason='{normalizedReason}' handoff='Navigation'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await DispatchExitToMenuHandoffAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='GoToMenuAsync' reason='{normalizedReason}' handoff='Navigation'.",
                DebugUtility.Colors.Success);
        }

        private async Task DispatchExitToMenuHandoffAsync(string reason, CancellationToken ct)
        {
            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Handoff] ExitToMenuDispatch action='GoToMenuAsync' reason='{reason}' target='Navigation'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _navigationService.GoToMenuAsync(reason);
        }

        private async Task RestartLastGameplayOrDefaultAsync(string reason, CancellationToken ct)
        {
            if (_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) &&
                snapshot.IsValid &&
                snapshot.MacroRouteId.IsValid)
            {
                await _navigationService.StartGameplayRouteAsync(snapshot.MacroRouteId, SceneTransitionPayload.Empty, reason);
                return;
            }

            await StartGameplayDefaultAsync(reason, ct);
        }

        private async Task StartGameplayDefaultAsync(string reason, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            SceneRouteId gameplayRouteId = _navigationService.ResolveGameplayRouteIdOrFail();
            if (!gameplayRouteId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][GameplaySessionFlow] Canonical gameplay route resolution returned an invalid routeId. reason='{reason}'.");
            }

            await _navigationService.StartGameplayRouteAsync(gameplayRouteId, SceneTransitionPayload.Empty, reason);
        }

        private async Task ResetCurrentLevelInternalAsync(string reason, CancellationToken ct)
        {
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasPhaseDefinitionRef ||
                snapshot.PhaseDefinitionRef == null ||
                snapshot.MacroRouteRef == null ||
                !snapshot.MacroRouteId.IsValid ||
                string.IsNullOrWhiteSpace(snapshot.LevelSignature))
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][GameplaySessionFlow] ResetCurrentLevelAsync requires a valid current gameplay snapshot. reason='{reason}'.");
            }

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ResetCurrentPhaseRequested rail='phase' phaseRef='{snapshot.PhaseDefinitionRef.name}' routeId='{snapshot.MacroRouteId}' v='{snapshot.SelectionVersion}' reason='{reason}' phaseSignature='{snapshot.LevelSignature}'.",
                DebugUtility.Colors.Info);

            PhaseResetContext resetContext = new PhaseResetContext(
                snapshot.PhaseDefinitionRef,
                snapshot.MacroRouteId,
                new LevelContextSignature(snapshot.LevelSignature),
                snapshot.LevelSignature);

            IWorldResetCommands worldResetCommands = ResolveGlobalOrFail<IWorldResetCommands>("IWorldResetCommands");
            await worldResetCommands.ResetLevelAsync(resetContext, reason, ct);
        }

        private static IPhaseNextPhaseService ResolveRequiredPhaseNextPhaseService(string reason)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][GameplaySessionFlow] DependencyManager.Provider is null while resolving NextPhase rail. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseService>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][GameplaySessionFlow] Missing required phase-local NextPhase rail. reason='{reason}'.");
            }

            return service;
        }

        private static T ResolveGlobalOrFail<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][GameplaySessionFlow] DependencyManager.Provider is null while resolving {label}.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][GameplaySessionFlow] Missing required global service: {label}.");
            }

            return service;
        }
    }
}

