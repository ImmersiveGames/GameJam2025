using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelFlowRuntimeService : ILevelFlowRuntimeService
    {
        private readonly IGameNavigationService _navigationService;
        private readonly IRestartContextService _restartContextService;
        private readonly ILevelSwapLocalService _levelSwapLocalService;

        public LevelFlowRuntimeService(
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
                ? "LevelFlow/StartGameplayDefault"
                : reason.Trim();

            SceneRouteId gameplayRouteId = _navigationService.ResolveGameplayRouteIdOrFail();
            if (!gameplayRouteId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(LevelFlowRuntimeService),
                    "[FATAL][H1][LevelFlow] Canonical gameplay route resolution returned an invalid routeId.");
            }

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] StartGameplayDefaultRequested routeId='{gameplayRouteId}' source='navigation_core_slot' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            await _navigationService.StartGameplayRouteAsync(gameplayRouteId, SceneTransitionPayload.Empty, normalizedReason);
        }

        public async Task SwapLevelLocalAsync(LevelDefinitionAsset levelRef, string reason = null, CancellationToken ct = default)
        {
            if (_levelSwapLocalService == null)
            {
                DebugUtility.LogWarning<LevelFlowRuntimeService>(
                    $"[OBS][LevelFlow] SwapLocalRejected levelRef='{(levelRef != null ? levelRef.name : "<none>")}' reason='missing_level_swap_local_service' requestedReason='{reason ?? "<null>"}'.");
                return;
            }

            await _levelSwapLocalService.SwapLocalAsync(levelRef, reason, ct);
        }

        public async Task RestartLastGameplayAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_restartContextService != null &&
                _restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) &&
                snapshot.IsValid &&
                snapshot.MacroRouteId.IsValid)
            {
                string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/RestartLastGameplay" : reason.Trim();
                await _navigationService.StartGameplayRouteAsync(snapshot.MacroRouteId, SceneTransitionPayload.Empty, normalizedReason);
                return;
            }

            await StartGameplayDefaultAsync(reason, ct);
        }
    }
}
