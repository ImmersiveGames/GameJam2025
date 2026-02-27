using System;
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
    /// <summary>
    /// Trilho oficial F4 para iniciar gameplay por levelId.
    /// - Resolve LevelId -> RouteId via ILevelFlowService.
    /// - Dispara navegação sem montar scene lists.
    /// - Não decide reset de mundo (responsabilidade de F2 por policy de rota).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelFlowRuntimeService : ILevelFlowRuntimeService
    {
        private readonly ILevelFlowService _levelResolver;
        private readonly IGameNavigationService _navigationService;
        private readonly IGameNavigationCatalog _navigationCatalog;
        private readonly ITransitionStyleCatalog _styleCatalog;
        private readonly IRestartContextService _restartContextService;
        private readonly ILevelContentResolver _contentResolver;
        private readonly IWorldResetCommands _worldResetCommands;

        public LevelFlowRuntimeService(
            ILevelFlowService levelResolver,
            IGameNavigationService navigationService,
            IGameNavigationCatalog navigationCatalog = null,
            ITransitionStyleCatalog styleCatalog = null,
            IRestartContextService restartContextService = null,
            IWorldResetCommands worldResetCommands = null,
            ILevelContentResolver contentResolver = null,
            ILevelMacroRouteCatalog macroRouteCatalog = null)
        {
            _levelResolver = levelResolver ?? throw new ArgumentNullException(nameof(levelResolver));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _navigationCatalog = navigationCatalog;
            _styleCatalog = styleCatalog;
            _restartContextService = restartContextService;
            _worldResetCommands = worldResetCommands;
            _contentResolver = contentResolver ?? (levelResolver as ILevelContentResolver);
        }

        public async Task StartGameplayAsync(string levelId, string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var typedLevelId = LevelId.FromName(levelId);
            if (!typedLevelId.IsValid)
            {
                FailFastConfig($"LevelId inválido para StartGameplayAsync. levelId='{levelId ?? "<null>"}', reason='{reason ?? "<null>"}'.");
                return;
            }

            if (!_levelResolver.TryResolve(typedLevelId, out var macroRouteId, out var payload) || !macroRouteId.IsValid)
            {
                FailFastConfig($"LevelId não resolvido no LevelFlow. levelId='{typedLevelId}', reason='{reason ?? "<null>"}'.");
                return;
            }

            SceneRouteId navRouteId = macroRouteId;

            var (styleId, styleIdTyped, profileId, profileAsset) = ResolveGameplayStyleObservability();
            string normalizedReason = NormalizeReason(reason);
            string selectedContentId = ResolveContentId(typedLevelId);
            LevelContextSignature levelSignature = LevelContextSignature.Create(
                typedLevelId,
                macroRouteId,
                normalizedReason,
                selectedContentId);

            int nextSelectionVersion = ResolveNextSelectionVersion();
            PublishLevelSelected(
                typedLevelId,
                macroRouteId,
                styleIdTyped,
                selectedContentId,
                normalizedReason,
                nextSelectionVersion,
                levelSignature);

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] StartGameplayRequested levelId='{typedLevelId}' macroRouteId='{macroRouteId}' navRouteId='{navRouteId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _navigationService.StartGameplayRouteAsync(navRouteId, payload, normalizedReason);

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] StartGameplayDispatched macroRouteId='{macroRouteId}' navRouteId='{navRouteId}' styleId='{styleId}' profile='{profileId}' profileAsset='{profileAsset}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
        }

        public async Task SwapLevelLocalAsync(LevelId levelId, string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!levelId.IsValid)
            {
                DebugUtility.LogError<LevelFlowRuntimeService>(
                    $"[LevelFlow] LevelSwapLocal ignorado: levelId inválido. levelId='{levelId}', reason='{reason ?? "<null>"}'.");
                return;
            }

            string normalizedReason = NormalizeSwapReason(reason);

            if (!_levelResolver.TryResolve(levelId, out var macroRouteId, out _) || !macroRouteId.IsValid)
            {
                DebugUtility.LogError<LevelFlowRuntimeService>(
                    $"[LevelFlow] LevelSwapLocal ignorado: levelId não resolvido. levelId='{levelId}', reason='{normalizedReason}'.");
                return;
            }

            string contentId = ResolveContentId(levelId);
            if (string.IsNullOrWhiteSpace(contentId))
            {
                DebugUtility.LogError<LevelFlowRuntimeService>(
                    $"[LevelFlow] LevelSwapLocal ignorado: contentId não resolvido. levelId='{levelId}', macroRouteId='{macroRouteId}', reason='{normalizedReason}'.");
                return;
            }

            LevelContextSignature levelSignature = LevelContextSignature.Create(levelId, macroRouteId, normalizedReason, contentId);
            int nextSelectionVersion = ResolveNextSelectionVersion();

            TransitionStyleId swapStyleId = ResolveStyleIdForSwap();

            PublishLevelSelected(
                levelId,
                macroRouteId,
                swapStyleId,
                contentId,
                normalizedReason,
                nextSelectionVersion,
                levelSignature);

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] LevelSwapLocalRequested levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' levelSignature='{levelSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            if (_worldResetCommands == null)
            {
                DebugUtility.LogError<LevelFlowRuntimeService>(
                    $"[OBS][LevelFlow] LevelSwapLocalCompleted levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' levelSignature='{levelSignature}' reason='{normalizedReason}' success=False notes='MissingIWorldResetCommands'.");
                return;
            }

            try
            {
                ct.ThrowIfCancellationRequested();
                await _worldResetCommands.ResetLevelAsync(levelId, normalizedReason, levelSignature, ct);

                SceneRouteId localRouteId = macroRouteId;

                PublishLevelSwapLocalApplied(
                    levelId,
                    macroRouteId,
                    localRouteId,
                    contentId,
                    normalizedReason,
                    nextSelectionVersion,
                    levelSignature);

                DebugUtility.Log<LevelFlowRuntimeService>(
                    $"[OBS][LevelFlow] LevelSwapLocalApplied levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' v='{nextSelectionVersion}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                DebugUtility.Log<LevelFlowRuntimeService>(
                    $"[OBS][LevelFlow] LevelSwapLocalCompleted levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' levelSignature='{levelSignature}' reason='{normalizedReason}' success=True notes=''.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<LevelFlowRuntimeService>(
                    $"[OBS][LevelFlow] LevelSwapLocalCompleted levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' levelSignature='{levelSignature}' reason='{normalizedReason}' success=False notes='{ex.GetType().Name}'.");
                throw;
            }
        }

        public async Task RestartLastGameplayAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_restartContextService == null ||
                !_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelId)
            {
                DebugUtility.LogWarning<LevelFlowRuntimeService>(
                    $"[WARN][OBS][LevelFlow] RestartLastGameplay skipped reason='no_valid_snapshot' requestedReason='{reason ?? "<null>"}'.");
                return;
            }

            LevelId levelId = snapshot.LevelId;
            if (!_levelResolver.TryResolve(levelId, out SceneRouteId macroRouteId, out SceneTransitionPayload payload) ||
                !macroRouteId.IsValid)
            {
                DebugUtility.LogWarning<LevelFlowRuntimeService>(
                    $"[WARN][OBS][LevelFlow] RestartLastGameplay skipped reason='level_not_resolved' levelId='{levelId}' requestedReason='{reason ?? "<null>"}'.");
                return;
            }

            if (snapshot.RouteId.IsValid && snapshot.RouteId != macroRouteId)
            {
                DebugUtility.LogWarning<LevelFlowRuntimeService>(
                    $"[WARN][OBS][LevelFlow] SnapshotRouteMismatch snapshotRouteId='{snapshot.RouteId}' resolvedMacroRouteId='{macroRouteId}' levelId='{levelId}'.");
            }

            SceneRouteId navRouteId = macroRouteId;

            string normalizedReason = NormalizeRestartReason(reason);
            string contentId = snapshot.HasContentId ? LevelFlowContentDefaults.Normalize(snapshot.ContentId) : ResolveContentId(levelId);
            TransitionStyleId styleId = ResolveRestartStyleId(snapshot.StyleId);
            int nextSelectionVersion = Math.Max(snapshot.SelectionVersion + 1, 1);
            LevelContextSignature levelSignature = LevelContextSignature.Create(levelId, macroRouteId, normalizedReason, contentId);

            PublishLevelSelected(
                levelId,
                macroRouteId,
                styleId,
                contentId,
                normalizedReason,
                nextSelectionVersion,
                levelSignature);

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] RestartLastGameplayRequested levelId='{levelId}' macroRouteId='{macroRouteId}' navRouteId='{navRouteId}' reason='{normalizedReason}' v='{nextSelectionVersion}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _navigationService.StartGameplayRouteAsync(navRouteId, payload, normalizedReason);

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] RestartLastGameplayDispatched macroRouteId='{macroRouteId}' navRouteId='{navRouteId}' styleId='{styleId}' reason='{normalizedReason}' v='{nextSelectionVersion}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
        }


        private (string styleId, TransitionStyleId styleIdTyped, string profileId, string profileAsset) ResolveGameplayStyleObservability()
        {
            if (_navigationCatalog == null || _styleCatalog == null)
            {
                return ("<unknown>", TransitionStyleId.None, "<unknown>", "<unknown>");
            }

            GameNavigationEntry gameplayEntry;
            if (_navigationCatalog is GameNavigationCatalogAsset assetCatalog)
            {
                gameplayEntry = assetCatalog.ResolveCoreOrFail(GameNavigationIntentKind.Gameplay);
            }
            else
            {
                return ("<unknown>", TransitionStyleId.None, "<unknown>", "<unknown>");
            }

            string styleId = gameplayEntry.StyleId.ToString();
            if (!_styleCatalog.TryGet(gameplayEntry.StyleId, out var style))
            {
                return (styleId, gameplayEntry.StyleId, "<unknown>", "<unknown>");
            }

            string profileId = style.ProfileId.ToString();
            string profileAsset = style.Profile != null ? style.Profile.name : "<null>";
            return (styleId, gameplayEntry.StyleId, profileId, profileAsset);
        }


        private string ResolveContentId(LevelId levelId)
        {
            if (_contentResolver != null &&
                _contentResolver.TryResolveContentId(levelId, out string contentId))
            {
                return LevelFlowContentDefaults.Normalize(contentId);
            }

            return LevelFlowContentDefaults.DefaultContentId;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "LevelFlow/StartGameplay" : reason.Trim();
        }

        private static string NormalizeSwapReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "LevelFlow/SwapLevelLocal" : reason.Trim();
        }

        private static string NormalizeRestartReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "LevelFlow/RestartLastGameplay" : reason.Trim();
        }

        private TransitionStyleId ResolveRestartStyleId(TransitionStyleId snapshotStyleId)
        {
            if (snapshotStyleId.IsValid)
            {
                return snapshotStyleId;
            }

            var (_, fallbackStyleId, _, _) = ResolveGameplayStyleObservability();
            if (fallbackStyleId.IsValid)
            {
                return fallbackStyleId;
            }

            DebugUtility.LogWarning<LevelFlowRuntimeService>(
                "[WARN][OBS][LevelFlow] RestartLastGameplay style_unknown -> fallback='none'.");
            return TransitionStyleId.None;
        }
        private TransitionStyleId ResolveStyleIdForSwap()
        {
            if (_restartContextService != null
                && _restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot)
                && snapshot.IsValid
                && snapshot.StyleId.IsValid)
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

            DebugUtility.LogWarning<LevelFlowRuntimeService>(
                "[WARN][OBS][LevelFlow] style_unknown while publishing LevelSelectedEvent during SwapLevelLocalAsync; fallback styleId='none'.");
            return TransitionStyleId.None;
        }

        private int ResolveNextSelectionVersion()
        {
            if (_restartContextService != null &&
                _restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot currentSnapshot) &&
                currentSnapshot.IsValid)
            {
                return currentSnapshot.SelectionVersion + 1;
            }

            return 1;
        }

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

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][Level] LevelSelectedEventPublished levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' reason='{reason}' v='{selectionVersion}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);
        }

        private static void PublishLevelSwapLocalApplied(
            LevelId levelId,
            SceneRouteId macroRouteId,
            SceneRouteId localRouteId,
            string contentId,
            string reason,
            int selectionVersion,
            LevelContextSignature levelSignature)
        {
            EventBus<LevelSwapLocalAppliedEvent>.Raise(new LevelSwapLocalAppliedEvent(
                levelId,
                macroRouteId,
                localRouteId,
                contentId,
                reason,
                selectionVersion,
                levelSignature.Value));
        }

        private static void FailFastConfig(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError<LevelFlowRuntimeService>(message);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }
    }
}
