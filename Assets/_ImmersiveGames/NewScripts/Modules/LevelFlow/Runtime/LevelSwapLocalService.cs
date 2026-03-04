using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using UnityEngine;

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
                FailFastConfig($"SwapLocal exige levelId válido. levelId='{levelId}' requestedReason='{reason ?? "<null>"}'.");
            }

            string normalizedReason = NormalizeSwapReason(reason);
            if (!_levelResolver.TryResolve(levelId, out SceneRouteId macroRouteId, out string contentId, out _) ||
                !macroRouteId.IsValid ||
                string.IsNullOrWhiteSpace(contentId))
            {
                FailFastConfig($"SwapLocal não conseguiu resolver macroRouteId/contentId. levelId='{levelId}' requestedReason='{normalizedReason}'.");
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

            LevelId fromLevelId = currentSnapshot.LevelId;
            LevelId toLevelId = levelId;

            LevelContextSignature levelSignature = LevelContextSignature.Create(toLevelId, macroRouteId, normalizedReason, contentId);
            int nextSelectionVersion = Math.Max(currentSnapshot.SelectionVersion + 1, 1);
            TransitionStyleId styleId = ResolveStyleIdForSwap(currentSnapshot.StyleId);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelSwapLocalRequested fromLevelId='{fromLevelId}' levelId='{toLevelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' v='{nextSelectionVersion}' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            if (_worldResetCommands == null)
            {
                DebugUtility.LogWarning<LevelSwapLocalService>(
                    $"[OBS][LevelFlow] SwapLocalRejected levelId='{levelId}' reason='missing_world_reset_commands' requestedReason='{normalizedReason}'.");
                return;
            }

            using IDisposable gateHandle = AcquireSwapGate();

            PublishLevelSelected(toLevelId, macroRouteId, styleId, contentId, normalizedReason, nextSelectionVersion, levelSignature);

            ct.ThrowIfCancellationRequested();
            await _worldResetCommands.ResetLevelAsync(toLevelId, normalizedReason, levelSignature, ct);

            PublishLevelSwapLocalApplied(toLevelId, macroRouteId, contentId, normalizedReason, nextSelectionVersion, levelSignature);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelSwapLocalApplied fromLevelId='{fromLevelId}' levelId='{toLevelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' v='{nextSelectionVersion}' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
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
                SceneRouteId.None,
                macroRouteId,
                contentId,
                reason,
                selectionVersion,
                levelSignature.Value));
        }

        private static void FailFastConfig(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError<LevelSwapLocalService>(message);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }
    }
}
