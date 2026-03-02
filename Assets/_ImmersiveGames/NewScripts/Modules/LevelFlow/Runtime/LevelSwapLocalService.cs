using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelSwapLocalService : ILevelSwapLocalService
    {
        private readonly ILevelFlowService _levelResolver;
        private readonly IRestartContextService _restartContextService;
        private readonly IWorldResetCommands _worldResetCommands;
        private readonly IGameNavigationCatalog _navigationCatalog;
        private readonly ISimulationGateService _simulationGateService;

        public LevelSwapLocalService(
            ILevelFlowService levelResolver,
            IRestartContextService restartContextService,
            IWorldResetCommands worldResetCommands,
            IGameNavigationCatalog navigationCatalog = null,
            ISimulationGateService simulationGateService = null)
        {
            _levelResolver = levelResolver ?? throw new ArgumentNullException(nameof(levelResolver));
            _restartContextService = restartContextService;
            _worldResetCommands = worldResetCommands;
            _navigationCatalog = navigationCatalog;
            _simulationGateService = simulationGateService;
        }

        public async Task SwapLocalAsync(LevelId levelId, string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!levelId.IsValid)
            {
                DebugUtility.LogWarning<LevelSwapLocalService>(
                    $"[OBS][LevelFlow] SwapLocalRejected levelId='{levelId}' reason='invalid_level_id' requestedReason='{reason ?? "<null>"}'.");
                return;
            }

            string normalizedReason = NormalizeSwapReason(reason);
            if (!_levelResolver.TryResolve(levelId, out SceneRouteId macroRouteId, out string contentId, out _) ||
                !macroRouteId.IsValid ||
                string.IsNullOrWhiteSpace(contentId))
            {
                DebugUtility.LogWarning<LevelSwapLocalService>(
                    $"[OBS][LevelFlow] SwapLocalRejected levelId='{levelId}' reason='level_unresolved' requestedReason='{normalizedReason}'.");
                return;
            }

            contentId = contentId.Trim();

            if (_restartContextService == null ||
                !_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot currentSnapshot) ||
                !currentSnapshot.IsValid ||
                !currentSnapshot.HasLevelId ||
                !currentSnapshot.RouteId.IsValid)
            {
                DebugUtility.LogWarning<LevelSwapLocalService>(
                    $"[OBS][LevelFlow] SwapLocalRejected levelId='{levelId}' reason='missing_runtime_snapshot' requestedReason='{normalizedReason}'.");
                return;
            }

            if (currentSnapshot.RouteId != macroRouteId)
            {
                DebugUtility.LogWarning<LevelSwapLocalService>(
                    $"[OBS][LevelFlow] SwapLocalRejected levelId='{levelId}' reason='macro_route_mismatch' currentMacroRouteId='{currentSnapshot.RouteId}' targetMacroRouteId='{macroRouteId}' requestedReason='{normalizedReason}'.");
                return;
            }

            LevelContextSignature levelSignature = LevelContextSignature.Create(levelId, macroRouteId, normalizedReason, contentId);
            int nextSelectionVersion = Math.Max(currentSnapshot.SelectionVersion + 1, 1);
            TransitionStyleId styleId = ResolveStyleIdForSwap(currentSnapshot.StyleId);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] SwapLocalRequested levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' v='{nextSelectionVersion}' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            if (_worldResetCommands == null)
            {
                DebugUtility.LogWarning<LevelSwapLocalService>(
                    $"[OBS][LevelFlow] SwapLocalRejected levelId='{levelId}' reason='missing_world_reset_commands' requestedReason='{normalizedReason}'.");
                return;
            }

            using IDisposable gateHandle = AcquireSwapGate();

            PublishLevelSelected(levelId, macroRouteId, styleId, contentId, normalizedReason, nextSelectionVersion, levelSignature);

            ct.ThrowIfCancellationRequested();
            await _worldResetCommands.ResetLevelAsync(levelId, normalizedReason, levelSignature, ct);

            PublishLevelSwapLocalApplied(levelId, macroRouteId, contentId, normalizedReason, nextSelectionVersion, levelSignature);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] SwapLocalApplied levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' v='{nextSelectionVersion}' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Success);
        }

        private IDisposable AcquireSwapGate()
        {
            if (_simulationGateService == null)
            {
                DebugUtility.LogWarning<LevelSwapLocalService>(
                    $"[WARN][OBS][LevelFlow] SwapLocal gate_not_available token='{SimulationGateTokens.LevelSwapLocal}'.");
                return null;
            }

            return _simulationGateService.Acquire(SimulationGateTokens.LevelSwapLocal);
        }

        private TransitionStyleId ResolveStyleIdForSwap(TransitionStyleId snapshotStyleId)
        {
            if (snapshotStyleId.IsValid)
            {
                return snapshotStyleId;
            }

            if (_navigationCatalog is GameNavigationCatalogAsset assetCatalog)
            {
                GameNavigationEntry gameplayEntry = assetCatalog.ResolveCoreOrFail(GameNavigationIntentKind.Gameplay);
                if (gameplayEntry.StyleId.IsValid)
                {
                    return gameplayEntry.StyleId;
                }
            }

            DebugUtility.LogWarning<LevelSwapLocalService>(
                "[WARN][OBS][LevelFlow] SwapLocal style_unknown -> fallback styleId='none'.");
            return TransitionStyleId.None;
        }

        private static string NormalizeSwapReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "LevelFlow/SwapLevelLocal" : reason.Trim();

        private static void PublishLevelSelected(
            LevelId levelId,
            SceneRouteId macroRouteId,
            TransitionStyleId styleId,
            string contentId,
            string reason,
            int selectionVersion,
            LevelContextSignature levelSignature)
        {
            EventBus<LevelSelectedEvent>.Raise(new LevelSelectedEvent(
                levelId,
                macroRouteId,
                styleId,
                contentId: contentId,
                reason: reason,
                selectionVersion: selectionVersion,
                levelSignature: levelSignature));

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][Level] LevelSelectedEventPublished levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' reason='{reason}' v='{selectionVersion}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);
        }

        private static void PublishLevelSwapLocalApplied(
            LevelId levelId,
            SceneRouteId macroRouteId,
            string contentId,
            string reason,
            int selectionVersion,
            LevelContextSignature levelSignature)
        {
            EventBus<LevelSwapLocalAppliedEvent>.Raise(new LevelSwapLocalAppliedEvent(
                levelId,
                macroRouteId,
                SceneRouteId.None,
                contentId,
                reason,
                selectionVersion,
                levelSignature.Value));
        }
    }
}
