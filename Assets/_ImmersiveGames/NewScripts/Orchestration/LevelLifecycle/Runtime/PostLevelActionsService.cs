using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostLevelActionsService : IPostLevelActionsService
    {
        private readonly ILevelFlowRuntimeService _levelFlowRuntimeService;
        private readonly ILevelSwapLocalService _levelSwapLocalService;
        private readonly IRestartContextService _restartContextService;
        private readonly IGameNavigationService _navigationService;
        private readonly ILevelFlowContentService _levelFlowContentService;

        public PostLevelActionsService(
            ILevelFlowRuntimeService levelFlowRuntimeService,
            ILevelSwapLocalService levelSwapLocalService,
            IRestartContextService restartContextService,
            IGameNavigationService navigationService,
            ILevelFlowContentService levelFlowContentService)
        {
            _levelFlowRuntimeService = levelFlowRuntimeService ?? throw new ArgumentNullException(nameof(levelFlowRuntimeService));
            _levelSwapLocalService = levelSwapLocalService ?? throw new ArgumentNullException(nameof(levelSwapLocalService));
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _levelFlowContentService = levelFlowContentService ?? throw new ArgumentNullException(nameof(levelFlowContentService));
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
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='Restart' scope='current_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorStarted action='Restart' scope='current_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            await _levelFlowRuntimeService.RestartLastGameplayAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='Restart' scope='current_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        private async Task RestartFromFirstLevelInternalAsync(string reason, CancellationToken ct)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/RestartFromFirstLevel" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='RestartFromFirstLevel' scope='first_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorStarted action='RestartFromFirstLevel' scope='first_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            await _levelFlowRuntimeService.RestartFromFirstLevelAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='RestartFromFirstLevel' scope='first_level' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        public async Task ResetCurrentLevelAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/ResetCurrentLevel" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='ResetCurrentLevel' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorStarted action='ResetCurrentLevel' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            await _levelFlowRuntimeService.ResetCurrentLevelAsync(normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='ResetCurrentLevel' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        public async Task NextLevelAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "PostLevel/NextLevel" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] IntentReceived action='NextLevel' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef ||
                snapshot.MacroRouteRef == null ||
                snapshot.MacroRouteRef.LevelCollection == null)
            {
                DebugUtility.LogWarning<PostLevelActionsService>(
                    $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='NextLevel' success=False reason='{normalizedReason}' notes='no_valid_snapshot_or_collection'.");
                return;
            }

            var nextLevelRef = _levelFlowContentService.ResolveNextLevelOrFail(snapshot, normalizedReason);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorStarted action='NextLevel' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
            await _levelSwapLocalService.SwapLocalAsync(nextLevelRef, normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][GameplaySessionFlow][Continuity] ExecutorCompleted action='NextLevel' reason='{normalizedReason}' nextLevelRef='{nextLevelRef.name}'.",
                DebugUtility.Colors.Success);
        }

        public async Task ExitToMenuAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "PostLevel/ExitToMenu" : reason.Trim();

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
    }
}

