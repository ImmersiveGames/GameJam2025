using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public class LevelLifecycleRuntimeService : ILevelFlowRuntimeService
    {
        private readonly IGameNavigationService _navigationService;
        private readonly IRestartContextService _restartContextService;
        private readonly ILevelSwapLocalService _levelSwapLocalService;

        public LevelLifecycleRuntimeService(
            IGameNavigationService navigationService,
            IRestartContextService restartContextService = null,
            ILevelSwapLocalService levelSwapLocalService = null)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _restartContextService = restartContextService;
            _levelSwapLocalService = levelSwapLocalService;
        }

        public async Task StartGameplayDefaultAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = string.IsNullOrWhiteSpace(reason)
                ? "LevelLifecycle/StartGameplayDefault"
                : reason.Trim();

            SceneRouteId gameplayRouteId = _navigationService.ResolveGameplayRouteIdOrFail();
            if (!gameplayRouteId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleRuntimeService),
                    "[FATAL][H1][LevelLifecycle] Canonical gameplay route resolution returned an invalid routeId.");
            }

            DebugUtility.Log<LevelLifecycleRuntimeService>(
                $"[OBS][LevelLifecycle][Operational] LevelEntryRequested source='GameplaySessionFlow' routeId='{gameplayRouteId}' rail='GameplaySessionFlow -> Level -> EnterStage -> Playing' dispatch='Navigation' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            await _navigationService.StartGameplayRouteAsync(gameplayRouteId, SceneTransitionPayload.Empty, normalizedReason);
        }

        public async Task SwapLevelLocalAsync(PhaseDefinitionSelectedEvent phaseSelection, string reason = null, CancellationToken ct = default)
        {
            if (_levelSwapLocalService == null)
            {
                DebugUtility.LogWarning<LevelLifecycleRuntimeService>(
                    $"[OBS][LevelLifecycle][Operational] SwapLocalRejected phaseRef='{(phaseSelection.PhaseDefinitionRef != null ? phaseSelection.PhaseDefinitionRef.name : "<none>")}' reason='missing_level_swap_local_service' requestedReason='{reason ?? "<null>"}'.");
                return;
            }

            await _levelSwapLocalService.SwapLocalAsync(phaseSelection, reason, ct);
        }

        public async Task ResetCurrentLevelAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = string.IsNullOrWhiteSpace(reason)
                ? "LevelLifecycle/ResetCurrentLevel"
                : reason.Trim();

            if (_restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleRuntimeService),
                    $"[FATAL][H1][LevelLifecycle] ResetCurrentLevelAsync requires a valid current gameplay snapshot. reason='{normalizedReason}'.");
            }

            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasPhaseDefinitionRef ||
                snapshot.PhaseDefinitionRef == null ||
                snapshot.MacroRouteRef == null ||
                !snapshot.MacroRouteId.IsValid ||
                string.IsNullOrWhiteSpace(snapshot.LevelSignature))
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleRuntimeService),
                    $"[FATAL][H1][LevelLifecycle] ResetCurrentLevelAsync requires a valid current gameplay snapshot. reason='{normalizedReason}'.");
            }

            DebugUtility.Log<LevelLifecycleRuntimeService>(
                $"[OBS][LevelLifecycle][Operational] ResetCurrentLevelRequested rail='phase' phaseRef='{snapshot.PhaseDefinitionRef.name}' routeId='{snapshot.MacroRouteId}' v='{snapshot.SelectionVersion}' reason='{normalizedReason}' levelSignature='{snapshot.LevelSignature}'.",
                DebugUtility.Colors.Info);

            PhaseResetContext resetContext = new PhaseResetContext(
                snapshot.PhaseDefinitionRef,
                snapshot.MacroRouteId,
                new LevelContextSignature(snapshot.LevelSignature),
                snapshot.LevelSignature);

            IWorldResetCommands worldResetCommands = ResolveGlobalOrFail<IWorldResetCommands>("IWorldResetCommands");
            await worldResetCommands.ResetLevelAsync(resetContext, normalizedReason, ct);
        }

        public async Task RestartLastGameplayAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_restartContextService != null &&
                _restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) &&
                snapshot.IsValid &&
                snapshot.MacroRouteId.IsValid)
            {
                string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelLifecycle/RestartLastGameplay" : reason.Trim();
                await _navigationService.StartGameplayRouteAsync(snapshot.MacroRouteId, SceneTransitionPayload.Empty, normalizedReason);
                return;
            }

            await StartGameplayDefaultAsync(reason, ct);
        }

        public async Task RestartFromFirstLevelAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = string.IsNullOrWhiteSpace(reason)
                ? "LevelLifecycle/RestartFromFirstLevel"
                : reason.Trim();

            if (_restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleRuntimeService),
                    $"[FATAL][H1][LevelLifecycle] RestartFromFirstLevelAsync requires IRestartContextService. reason='{normalizedReason}'.");
            }

            _restartContextService.Clear(normalizedReason);

            DebugUtility.Log<LevelLifecycleRuntimeService>(
                $"[OBS][LevelLifecycle][Operational] RestartFromFirstLevelRequested reason='{normalizedReason}' dispatch='Navigation.StartGameplayDefaultAsync'.",
                DebugUtility.Colors.Info);

            await StartGameplayDefaultAsync(normalizedReason, ct);
        }

        private static T ResolveGlobalOrFail<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleRuntimeService),
                    $"[FATAL][H1][LevelLifecycle] DependencyManager.Provider is null while resolving {label}.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleRuntimeService),
                    $"[FATAL][H1][LevelLifecycle] Missing required global service: {label}.");
            }

            return service;
        }
    }
}
