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
        private readonly ILevelSwapLocalService _levelSwapLocalService;
        private readonly IWorldResetCommands _worldResetCommands;
        private readonly ISimulationGateService _simulationGateService;

        public LevelFlowRuntimeService(
            ILevelFlowService levelResolver,
            IGameNavigationService navigationService,
            IGameNavigationCatalog navigationCatalog = null,
            ITransitionStyleCatalog styleCatalog = null,
            IRestartContextService restartContextService = null,
            ILevelSwapLocalService levelSwapLocalService = null,
            ILevelContentResolver contentResolver = null,
            ILevelMacroRouteCatalog macroRouteCatalog = null,
            IWorldResetCommands worldResetCommands = null,
            ISimulationGateService simulationGateService = null)
        {
            _levelResolver = levelResolver ?? throw new ArgumentNullException(nameof(levelResolver));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _navigationCatalog = navigationCatalog;
            _styleCatalog = styleCatalog;
            _restartContextService = restartContextService;
            _levelSwapLocalService = levelSwapLocalService;
            _contentResolver = contentResolver ?? (levelResolver as ILevelContentResolver);
            _worldResetCommands = worldResetCommands;
            _simulationGateService = simulationGateService;
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

            if (!_levelResolver.TryResolve(typedLevelId, out var macroRouteId, out var contentId, out var payload) ||
                !macroRouteId.IsValid ||
                string.IsNullOrWhiteSpace(contentId))
            {
                FailFastConfig($"LevelId não resolvido no LevelFlow ou contentId inválido. levelId='{typedLevelId}', reason='{reason ?? "<null>"}'.");
                return;
            }

            SceneRouteId navRouteId = macroRouteId;

            var (styleId, styleIdTyped, profileId, profileAsset) = ResolveGameplayStyleObservability();
            string normalizedReason = NormalizeReason(reason);
            string selectedContentId = contentId.Trim();
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
            if (_levelSwapLocalService == null)
            {
                DebugUtility.LogWarning<LevelFlowRuntimeService>(
                    $"[OBS][LevelFlow] SwapLocalRejected levelId='{levelId}' reason='missing_level_swap_local_service' requestedReason='{reason ?? "<null>"}'.");
                return;
            }

            await _levelSwapLocalService.SwapLocalAsync(levelId, reason, ct);
        }


        public async Task ResetCurrentLevelAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_restartContextService == null ||
                !_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelId ||
                !snapshot.RouteId.IsValid)
            {
                FailFastConfig($"ResetCurrentLevelAsync exige seleção atual válida. requestedReason='{reason ?? "<null>"}'.");
                return;
            }

            if (_worldResetCommands == null)
            {
                FailFastConfig("ResetCurrentLevelAsync exige IWorldResetCommands registrado.");
                return;
            }

            string normalizedReason = NormalizeLevelResetReason(reason);
            LevelId levelId = snapshot.LevelId;
            SceneRouteId macroRouteId = snapshot.RouteId;
            string contentId = snapshot.HasContentId ? LevelFlowContentDefaults.Normalize(snapshot.ContentId) : ResolveContentId(levelId);
            int selectionVersion = Math.Max(snapshot.SelectionVersion, 1);
            LevelContextSignature levelSignature = string.IsNullOrWhiteSpace(snapshot.LevelSignature)
                ? LevelContextSignature.Create(levelId, macroRouteId, normalizedReason, contentId)
                : new LevelContextSignature(snapshot.LevelSignature);

            using IDisposable gateHandle = AcquireLevelResetGate();

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] LevelResetRequested levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' v='{selectionVersion}' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();
            await _worldResetCommands.ResetLevelAsync(levelId, normalizedReason, levelSignature, ct);

            DebugUtility.Log<LevelFlowRuntimeService>(
                $"[OBS][LevelFlow] LevelResetCompleted levelId='{levelId}' macroRouteId='{macroRouteId}' contentId='{contentId}' v='{selectionVersion}' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Success);
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
            if (!_levelResolver.TryResolve(levelId, out SceneRouteId macroRouteId, out _, out SceneTransitionPayload payload) ||
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

        private static string NormalizeLevelResetReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "LevelFlow/ResetCurrentLevel" : reason.Trim();
        }


        private IDisposable AcquireLevelResetGate()
        {
            if (_simulationGateService == null)
            {
                DebugUtility.LogWarning<LevelFlowRuntimeService>(
                    $"[WARN][OBS][LevelFlow] LevelReset gate_not_available token='{SimulationGateTokens.LevelReset}'.");
                return null;
            }

            return _simulationGateService.Acquire(SimulationGateTokens.LevelReset);
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
