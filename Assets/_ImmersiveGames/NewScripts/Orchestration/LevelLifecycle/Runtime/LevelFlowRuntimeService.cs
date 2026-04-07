using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

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

        public async Task SwapLevelLocalAsync(LevelDefinitionAsset levelRef, string reason = null, CancellationToken ct = default)
        {
            if (_levelSwapLocalService == null)
            {
                DebugUtility.LogWarning<LevelLifecycleRuntimeService>(
                    $"[OBS][LevelLifecycle][Operational] SwapLocalRejected levelRef='{(levelRef != null ? levelRef.name : "<none>")}' reason='missing_level_swap_local_service' requestedReason='{reason ?? "<null>"}'.");
                return;
            }

            await _levelSwapLocalService.SwapLocalAsync(levelRef, reason, ct);
        }

        public async Task ResetCurrentLevelAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelLifecycle/ResetCurrentLevel" : reason.Trim();
            GameplayStartSnapshot snapshot = default;

            if (_restartContextService == null ||
                !_restartContextService.TryGetCurrent(out snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef)
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleRuntimeService),
                    $"[FATAL][H1][LevelLifecycle] ResetCurrentLevelAsync requires a valid current gameplay snapshot. reason='{normalizedReason}'.");
            }

            DebugUtility.Log<LevelLifecycleRuntimeService>(
                $"[OBS][LevelLifecycle][Operational] ResetCurrentLevelRequested levelRef='{snapshot.LevelRef.name}' routeId='{snapshot.MacroRouteId}' v='{snapshot.SelectionVersion}' reason='{normalizedReason}' levelSignature='{(string.IsNullOrWhiteSpace(snapshot.LevelSignature) ? "<none>" : snapshot.LevelSignature)}'.",
                DebugUtility.Colors.Info);

            await SwapLevelLocalAsync(snapshot.LevelRef, normalizedReason, ct);
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

            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelLifecycle/RestartFromFirstLevel" : reason.Trim();

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
    }

}
