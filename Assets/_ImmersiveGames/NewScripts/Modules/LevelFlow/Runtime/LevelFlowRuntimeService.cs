using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
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

        public LevelFlowRuntimeService(
            ILevelFlowService levelResolver,
            IGameNavigationService navigationService,
            IGameNavigationCatalog navigationCatalog = null,
            ITransitionStyleCatalog styleCatalog = null,
            IRestartContextService restartContextService = null)
        {
            _levelResolver = levelResolver ?? throw new ArgumentNullException(nameof(levelResolver));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _navigationCatalog = navigationCatalog;
            _styleCatalog = styleCatalog;
            _restartContextService = restartContextService;
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

            if (!_levelResolver.TryResolve(typedLevelId, out var resolvedRouteId, out var payload) || !resolvedRouteId.IsValid)
            {
                FailFastConfig($"LevelId não resolvido no LevelFlow. levelId='{typedLevelId}', reason='{reason ?? "<null>"}'.");
                return;
            }

            var (styleId, styleIdTyped, profileId, profileAsset) = ResolveGameplayStyleObservability();
            string normalizedReason = NormalizeReason(reason);
            string selectedContentId = ResolveContentId(typedLevelId);
            LevelContextSignature levelSignature = LevelContextSignature.Create(
                typedLevelId,
                resolvedRouteId,
                normalizedReason,
                selectedContentId);

            int nextSelectionVersion = ResolveNextSelectionVersion();

            EventBus<LevelSelectedEvent>.Raise(new LevelSelectedEvent(
                typedLevelId,
                resolvedRouteId,
                styleIdTyped,
                contentId: selectedContentId,
                reason: normalizedReason,
                selectionVersion: nextSelectionVersion,
                levelSignature: levelSignature));

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][Level] LevelSelectedEventPublished levelId='{typedLevelId}' routeId='{resolvedRouteId}' contentId='{selectedContentId}' reason='{normalizedReason}' v='{nextSelectionVersion}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] StartGameplayRequested levelId='{typedLevelId}' routeId='{resolvedRouteId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _navigationService.StartGameplayRouteAsync(resolvedRouteId, payload, normalizedReason);

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] StartGameplayDispatched routeId='{resolvedRouteId}' styleId='{styleId}' profile='{profileId}' profileAsset='{profileAsset}' reason='{normalizedReason}'.",
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
            if (_levelResolver is ILevelContentResolver contentResolver &&
                contentResolver.TryResolveContentId(levelId, out string contentId))
            {
                return LevelFlowContentDefaults.Normalize(contentId);
            }

            return LevelFlowContentDefaults.DefaultContentId;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "LevelFlow/StartGameplay" : reason.Trim();
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
