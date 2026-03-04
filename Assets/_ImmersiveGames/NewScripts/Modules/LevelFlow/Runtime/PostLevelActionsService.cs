using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostLevelActionsService : IPostLevelActionsService
    {
        private readonly ILevelFlowRuntimeService _levelFlowRuntimeService;
        private readonly ILevelSwapLocalService _levelSwapLocalService;
        private readonly IRestartContextService _restartContextService;
        private readonly ILevelMacroRouteCatalog _macroRouteCatalog;
        private readonly IGameNavigationService _navigationService;

        public PostLevelActionsService(
            ILevelFlowRuntimeService levelFlowRuntimeService,
            ILevelSwapLocalService levelSwapLocalService,
            IRestartContextService restartContextService,
            ILevelMacroRouteCatalog macroRouteCatalog,
            IGameNavigationService navigationService)
        {
            _levelFlowRuntimeService = levelFlowRuntimeService ?? throw new ArgumentNullException(nameof(levelFlowRuntimeService));
            _levelSwapLocalService = levelSwapLocalService ?? throw new ArgumentNullException(nameof(levelSwapLocalService));
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _macroRouteCatalog = macroRouteCatalog ?? throw new ArgumentNullException(nameof(macroRouteCatalog));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        public async Task RestartLevelAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "PostLevel/RestartLevel" : reason.Trim();
            string levelSignature = ResolveCurrentLevelSignature();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionRequested action='RestartLevel' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] RestartUsesGameResetRequestedEvent reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent(normalizedReason));

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionApplied action='RestartLevel' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Success);
        }

        private string ResolveCurrentLevelSignature()
        {
            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) || !snapshot.IsValid)
            {
                return "<none>";
            }

            if (!string.IsNullOrWhiteSpace(snapshot.LevelSignature))
            {
                return snapshot.LevelSignature;
            }

            if (!snapshot.HasLevelId || !snapshot.RouteId.IsValid)
            {
                return "<none>";
            }

            string contentId = snapshot.HasContentId ? snapshot.ContentId : string.Empty;
            return LevelContextSignature.Create(snapshot.LevelId, snapshot.RouteId, snapshot.Reason, contentId).Value;
        }

        public async Task NextLevelAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "PostLevel/NextLevel" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionRequested action='NextLevel' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelId)
            {
                DebugUtility.LogWarning<PostLevelActionsService>(
                    $"[OBS][LevelFlow] PostLevelActionApplied action='NextLevel' success=False reason='{normalizedReason}' notes='no_valid_snapshot'.");
                return;
            }

            if (!_macroRouteCatalog.TryGetNextLevelInMacro(snapshot.LevelId, out LevelId nextLevelId, wrapToFirst: true) ||
                !nextLevelId.IsValid)
            {
                DebugUtility.LogWarning<PostLevelActionsService>(
                    $"[OBS][LevelFlow] PostLevelActionApplied action='NextLevel' success=False reason='{normalizedReason}' notes='no_next_level' currentLevelId='{snapshot.LevelId}'.");
                return;
            }

            await _levelSwapLocalService.SwapLocalAsync(nextLevelId, normalizedReason, ct);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionApplied action='NextLevel' reason='{normalizedReason}' nextLevelId='{nextLevelId}'.",
                DebugUtility.Colors.Success);
        }

        public async Task ExitToMenuAsync(string reason = null, CancellationToken ct = default)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "PostLevel/ExitToMenu" : reason.Trim();

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionRequested action='ExitToMenu' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _navigationService.ExitToMenuAsync(normalizedReason);

            DebugUtility.Log<PostLevelActionsService>(
                $"[OBS][LevelFlow] PostLevelActionApplied action='ExitToMenu' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }
    }
}
