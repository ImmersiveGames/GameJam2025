using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Navigation;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostLevelActionsService : IPostLevelActionsService
    {
        private readonly ILevelFlowRuntimeService _levelFlowRuntimeService;
        private readonly ILevelSwapLocalService _levelSwapLocalService;
        private readonly IRestartContextService _restartContextService;
        private readonly IGameNavigationService _navigationService;

        public PostLevelActionsService(
            ILevelFlowRuntimeService levelFlowRuntimeService,
            ILevelSwapLocalService levelSwapLocalService,
            IRestartContextService restartContextService,
            IGameNavigationService navigationService)
        {
            _levelFlowRuntimeService = levelFlowRuntimeService ?? throw new ArgumentNullException(nameof(levelFlowRuntimeService));
            _levelSwapLocalService = levelSwapLocalService ?? throw new ArgumentNullException(nameof(levelSwapLocalService));
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
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
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/RestartLevel" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] RestartRequested action='Restart' scope='current_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            await _levelFlowRuntimeService.RestartLastGameplayAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] RestartApplied action='Restart' scope='current_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        private async Task RestartFromFirstLevelInternalAsync(string reason, CancellationToken ct)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/RestartFromFirstLevel" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] RestartRequested action='RestartFromFirstLevel' scope='first_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            await _levelFlowRuntimeService.RestartFromFirstLevelAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] RestartApplied action='RestartFromFirstLevel' scope='first_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        public async Task ResetCurrentLevelAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/ResetCurrentLevel" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionRequested action='ResetCurrentLevel' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            await _levelFlowRuntimeService.ResetCurrentLevelAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionApplied action='ResetCurrentLevel' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        public async Task NextLevelAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "PostLevel/NextLevel" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionRequested action='NextLevel' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef ||
                snapshot.MacroRouteRef == null ||
                snapshot.MacroRouteRef.LevelCollection == null)
            {
                DebugUtility.LogWarning<PostLevelActionsService>(
                    $"[OBS][LevelFlow] PostLevelActionApplied action='NextLevel' success=False reason='{normalizedReason}' notes='no_valid_snapshot_or_collection'.");
                return;
            }

            if (snapshot.MacroRouteRef.RouteId != snapshot.MacroRouteId)
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][LevelFlow] PostLevel NextLevel routeRef mismatch. routeId='{snapshot.MacroRouteId}' routeRefRouteId='{snapshot.MacroRouteRef.RouteId}'.");
            }

            var collection = snapshot.MacroRouteRef.LevelCollection;
            if (!collection.TryValidateRuntime(out string collectionError))
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][LevelFlow] PostLevel NextLevel invalid LevelCollection. routeId='{snapshot.MacroRouteId}' detail='{collectionError}'.");
            }

            int currentIndex = -1;
            for (int i = 0; i < collection.Levels.Count; i++)
            {
                if (ReferenceEquals(collection.Levels[i], snapshot.LevelRef))
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][LevelFlow] PostLevel NextLevel current levelRef not found in route collection. routeId='{snapshot.MacroRouteId}' levelRef='{snapshot.LevelRef.name}'.");
            }

            int nextIndex = (currentIndex + 1) % collection.Levels.Count;
            var nextLevelRef = collection.Levels[nextIndex];
            if (nextLevelRef == null)
            {
                HardFailFastH1.Trigger(typeof(PostLevelActionsService),
                    $"[FATAL][H1][LevelFlow] PostLevel NextLevel resolved null at index='{nextIndex}'. routeId='{snapshot.MacroRouteId}'.");
            }

            await _levelSwapLocalService.SwapLocalAsync(nextLevelRef, normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionApplied action='NextLevel' reason='{normalizedReason}' nextLevelRef='{nextLevelRef.name}'.",
                DebugUtility.Colors.Success);
        }

        public async Task ExitToMenuAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "PostLevel/ExitToMenu" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] ExitToMenuRequested action='ExitToMenu' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][Navigation] ExitToMenuDispatch action='GoToMenuAsync' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            await _navigationService.GoToMenuAsync(normalizedReason);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][Navigation] ExitToMenuApplied action='GoToMenuAsync' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }
    }
}

