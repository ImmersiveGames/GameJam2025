using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelMacroPrepareService : ILevelMacroPrepareService
    {
        private const string DefaultReason = "SceneFlow/LevelPrepare";

        private readonly IRestartContextService _restartContextService;
        private readonly ILevelMacroRouteCatalog _macroRouteCatalog;
        private readonly ILevelFlowService _levelFlowService;
        private readonly ILevelContentResolver _levelContentResolver;
        private readonly IWorldResetCommands _worldResetCommands;
        private readonly IGameNavigationCatalog _navigationCatalog;

        public LevelMacroPrepareService(
            IRestartContextService restartContextService,
            ILevelMacroRouteCatalog macroRouteCatalog,
            ILevelFlowService levelFlowService,
            ILevelContentResolver levelContentResolver,
            IWorldResetCommands worldResetCommands,
            IGameNavigationCatalog navigationCatalog = null)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _macroRouteCatalog = macroRouteCatalog ?? throw new ArgumentNullException(nameof(macroRouteCatalog));
            _levelFlowService = levelFlowService ?? throw new ArgumentNullException(nameof(levelFlowService));
            _levelContentResolver = levelContentResolver ?? throw new ArgumentNullException(nameof(levelContentResolver));
            _worldResetCommands = worldResetCommands ?? throw new ArgumentNullException(nameof(worldResetCommands));
            _navigationCatalog = navigationCatalog;
        }

        public async Task PrepareAsync(SceneRouteId macroRouteId, string reason, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!macroRouteId.IsValid)
            {
                return;
            }

            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? DefaultReason : reason.Trim();

            if (!_macroRouteCatalog.TryGetLevelsForMacroRoute(macroRouteId, out IReadOnlyList<LevelId> levelIds) ||
                levelIds == null ||
                levelIds.Count == 0)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][LevelFlow] LevelPrepared source='no_levels_for_macro' macroRouteId='{macroRouteId}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            GameplayStartSnapshot snapshot = GameplayStartSnapshot.Empty;
            bool hasSnapshot = _restartContextService.TryGetCurrent(out snapshot) && snapshot.IsValid && snapshot.HasLevelId;

            SceneRouteId snapshotMacroRouteId = SceneRouteId.None;
            bool snapshotBelongsToMacro = hasSnapshot &&
                                          snapshot.LevelId.IsValid &&
                                          _macroRouteCatalog.TryResolveMacroRouteId(snapshot.LevelId, out snapshotMacroRouteId) &&
                                          snapshotMacroRouteId.IsValid &&
                                          snapshotMacroRouteId == macroRouteId;

            if (hasSnapshot && !snapshotBelongsToMacro)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][LevelFlow] LevelPreparedSnapshotIgnored macroRouteId='{macroRouteId}' snapshotLevelId='{snapshot.LevelId}' snapshotMacroRouteId='{snapshotMacroRouteId}' reason='not_in_macro'.",
                    DebugUtility.Colors.Info);
            }

            bool useSnapshot = snapshotBelongsToMacro;
            string source = useSnapshot ? "snapshot" : "catalog_first";

            LevelId selectedLevelId = useSnapshot ? snapshot.LevelId : levelIds[0];
            if (!selectedLevelId.IsValid)
            {
                FailFastConfig($"LevelPrepare sem level válido para macroRouteId='{macroRouteId}'.");
            }

            if (!_levelFlowService.TryResolve(selectedLevelId, out SceneRouteId resolvedRouteId, out _, out _) || !resolvedRouteId.IsValid)
            {
                FailFastConfig($"LevelPrepare não conseguiu resolver routeId para levelId='{selectedLevelId}' macroRouteId='{macroRouteId}'.");
            }

            string contentId = useSnapshot && !string.IsNullOrWhiteSpace(snapshot.ContentId)
                ? snapshot.ContentId.Trim()
                : ResolveContentIdOrFail(selectedLevelId, macroRouteId);

            int selectionVersion = useSnapshot
                ? Math.Max(snapshot.SelectionVersion, 1)
                : Math.Max(ResolveSelectionVersionForPublish(hasSnapshot, snapshot), 1);

            string levelSignature = useSnapshot && !string.IsNullOrWhiteSpace(snapshot.LevelSignature)
                ? snapshot.LevelSignature
                : LevelContextSignature.Create(selectedLevelId, resolvedRouteId, normalizedReason, contentId).Value;

            if (!useSnapshot)
            {
                TransitionStyleId styleId = ResolveStyleId(snapshot, hasSnapshot);
                EventBus<LevelSelectedEvent>.Raise(new LevelSelectedEvent(
                    selectedLevelId,
                    resolvedRouteId,
                    styleId,
                    contentId,
                    normalizedReason,
                    selectionVersion,
                    new LevelContextSignature(levelSignature)));
            }

            ct.ThrowIfCancellationRequested();
            await _worldResetCommands.ResetLevelAsync(
                selectedLevelId,
                normalizedReason,
                new LevelContextSignature(levelSignature),
                ct);
            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelPrepared source='{source}' macroRouteId='{macroRouteId}' levelId='{selectedLevelId}' resolvedRouteId='{resolvedRouteId}' contentId='{contentId}' v='{selectionVersion}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        private string ResolveContentIdOrFail(LevelId levelId, SceneRouteId macroRouteId)
        {
            if (_levelContentResolver.TryResolveContentId(levelId, out string contentId) && !string.IsNullOrWhiteSpace(contentId))
            {
                return contentId.Trim();
            }

            FailFastConfig($"LevelPrepare não conseguiu resolver contentId para levelId='{levelId}' macroRouteId='{macroRouteId}'.");
            return string.Empty;
        }

        private int ResolveSelectionVersionForPublish(bool hasSnapshot, GameplayStartSnapshot snapshot)
        {
            if (hasSnapshot)
            {
                return snapshot.SelectionVersion + 1;
            }

            return 1;
        }

        private TransitionStyleId ResolveStyleId(GameplayStartSnapshot snapshot, bool hasSnapshot)
        {
            if (hasSnapshot && snapshot.StyleId.IsValid)
            {
                return snapshot.StyleId;
            }

            if (_navigationCatalog is GameNavigationCatalogAsset assetCatalog)
            {
                GameNavigationEntry gameplayEntry = assetCatalog.ResolveCoreOrFail(GameNavigationIntentKind.Gameplay);
                if (gameplayEntry.StyleId.IsValid)
                {
                    return gameplayEntry.StyleId;
                }
            }

            DebugUtility.LogWarning<LevelMacroPrepareService>(
                "[WARN][OBS][LevelFlow] style_unknown while publishing LevelSelectedEvent during LevelPrepare; fallback styleId='none'.");
            return TransitionStyleId.None;
        }

        private static void FailFastConfig(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError<LevelMacroPrepareService>(message);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif

            throw new InvalidOperationException(message);
        }
    }
}
