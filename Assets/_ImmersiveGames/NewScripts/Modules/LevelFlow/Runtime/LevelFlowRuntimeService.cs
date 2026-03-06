using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
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

            SceneRouteId gameplayRouteId = SceneRouteId.FromName(GameNavigationIntents.ToGameplay);
            if (!gameplayRouteId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(LevelFlowRuntimeService),
                    "[FATAL][H1][LevelFlow] Invalid gameplay route id for StartGameplayDefaultAsync ('to-gameplay').");
            }

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] StartGameplayDefaultRequested routeId='{gameplayRouteId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            await _navigationService.StartGameplayRouteAsync(gameplayRouteId, SceneTransitionPayload.Empty, normalizedReason);
        }

        public async Task SwapLevelLocalAsync(LevelId levelId, string reason = null, CancellationToken ct = default)
        {
            if (_levelSwapLocalService == null)
            {
                DebugUtility.LogWarning<LevelFlowRuntimeService>(
                    $"[OBS][LevelFlow] SwapLocalRejected levelId='{levelId}' reason='missing_level_swap_local_service' requestedReason='{reason ?? "<null>"}'.");
                return;
            }

            await _levelSwapLocalService.SwapLocalAsync(levelId, reason, ct);
        }

        public async Task RestartLastGameplayAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_restartContextService != null &&
                _restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) &&
                snapshot.IsValid &&
                snapshot.RouteId.IsValid)
            {
                string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/RestartLastGameplay" : reason.Trim();
                await _navigationService.StartGameplayRouteAsync(snapshot.RouteId, SceneTransitionPayload.Empty, normalizedReason);
                return;
            }

            await StartGameplayDefaultAsync(reason, ct);
        }
    }
}



