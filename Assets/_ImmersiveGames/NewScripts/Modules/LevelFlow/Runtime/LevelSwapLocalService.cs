using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelSwapLocalService : ILevelSwapLocalService
    {
        private readonly IRestartContextService _restartContextService;
        private readonly IWorldResetCommands _worldResetCommands;
        private readonly ISimulationGateService _simulationGateService;
        private readonly SceneRouteCatalogAsset _sceneRouteCatalog;

        public LevelSwapLocalService(
            IRestartContextService restartContextService,
            IWorldResetCommands worldResetCommands,
            object _navigationCatalog = null,
            ISimulationGateService simulationGateService = null,
            SceneRouteCatalogAsset sceneRouteCatalog = null)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _worldResetCommands = worldResetCommands ?? throw new ArgumentNullException(nameof(worldResetCommands));
            _simulationGateService = simulationGateService;
            _sceneRouteCatalog = sceneRouteCatalog ?? throw new ArgumentNullException(nameof(sceneRouteCatalog));
        }

        public async Task SwapLocalAsync(LevelDefinitionAsset targetLevelRef, string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = NormalizeSwapReason(reason);
            if (targetLevelRef == null)
            {
                FailFast(SceneRouteId.None, SceneRouteKind.Unspecified, ComputeSignature(SceneRouteId.None, SceneRouteKind.Unspecified, normalizedReason), normalizedReason, "Target levelRef is null.");
            }

            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot currentSnapshot) ||
                !currentSnapshot.IsValid ||
                !currentSnapshot.RouteId.IsValid ||
                !currentSnapshot.HasLevelRef)
            {
                FailFast(SceneRouteId.None, SceneRouteKind.Unspecified, ComputeSignature(SceneRouteId.None, SceneRouteKind.Unspecified, normalizedReason), normalizedReason, "Missing runtime gameplay snapshot.");
            }

            SceneRouteId macroRouteId = currentSnapshot.RouteId;
            SceneRouteDefinitionAsset routeAsset = ResolveRouteAssetOrFail(macroRouteId, normalizedReason);
            SceneRouteKind routeKind = routeAsset.RouteKind;
            string swapSignature = ComputeSignature(macroRouteId, routeKind, normalizedReason);
            LevelCollectionAsset levelCollection = ResolveLevelCollectionOrFail(routeAsset, macroRouteId, swapSignature, normalizedReason);

            if (!levelCollection.Contains(targetLevelRef))
            {
                FailFast(macroRouteId, routeKind, swapSignature, normalizedReason, $"Target levelRef='{targetLevelRef.name}' is not part of route LevelCollection.");
            }

            targetLevelRef.ValidateOrFailFast($"LevelSwapLocal routeId='{macroRouteId}' reason='{normalizedReason}' targetLevelRef='{targetLevelRef.name}'");

            LevelDefinitionAsset fromLevelRef = currentSnapshot.LevelRef;
            int nextSelectionVersion = Math.Max(currentSnapshot.SelectionVersion + 1, 1);
            string levelSignature = CreateLevelSignature(targetLevelRef, macroRouteId, normalizedReason);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelSwapLocalStart fromLevelRef='{(fromLevelRef != null ? fromLevelRef.name : "<none>")}' toLevelRef='{targetLevelRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' v='{nextSelectionVersion}' signature='{swapSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            using IDisposable gateHandle = AcquireSwapGate();

            PublishLevelSelected(targetLevelRef, macroRouteId, normalizedReason, nextSelectionVersion, levelSignature);

            ct.ThrowIfCancellationRequested();
            await _worldResetCommands.ResetLevelAsync(targetLevelRef, normalizedReason, new LevelContextSignature(levelSignature), ct);
            ct.ThrowIfCancellationRequested();

            (int scenesAdded, int scenesRemoved) = await LevelAdditiveSceneRuntimeApplier.ApplyAsync(
                fromLevelRef,
                targetLevelRef,
                ct);

            PublishLevelSwapLocalApplied(targetLevelRef, macroRouteId, normalizedReason, nextSelectionVersion, levelSignature);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelSwapLocalApplied fromLevelRef='{(fromLevelRef != null ? fromLevelRef.name : "<none>")}' toLevelRef='{targetLevelRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' scenesAdded={scenesAdded} scenesRemoved={scenesRemoved} v='{nextSelectionVersion}' signature='{swapSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        [Obsolete("Legacy LevelId swap path is disabled in canonical LevelFlow.")]
        public Task SwapLocalAsync(LevelId levelId, string reason = null, CancellationToken ct = default)
        {
            HardFailFastH1.Trigger(typeof(LevelSwapLocalService),
                $"[FATAL][H1][LevelFlow] Legacy SwapLocalAsync(LevelId) is disabled. levelId='{levelId}' reason='{reason}'.");
            return Task.CompletedTask;
        }

        private SceneRouteDefinitionAsset ResolveRouteAssetOrFail(SceneRouteId macroRouteId, string reason)
        {
            string signature = ComputeSignature(macroRouteId, SceneRouteKind.Unspecified, reason);

            if (_sceneRouteCatalog == null)
            {
                FailFast(macroRouteId, SceneRouteKind.Unspecified, signature, reason, "BootstrapConfig.SceneRouteCatalog is null.");
            }

            if (!_sceneRouteCatalog.TryGetAsset(macroRouteId, out SceneRouteDefinitionAsset routeAsset) || routeAsset == null)
            {
                FailFast(macroRouteId, SceneRouteKind.Unspecified, signature, reason, "RouteAsset missing from SceneRouteCatalogAsset.");
            }

            return routeAsset;
        }

        private LevelCollectionAsset ResolveLevelCollectionOrFail(SceneRouteDefinitionAsset routeAsset, SceneRouteId macroRouteId, string signature, string reason)
        {
            if (routeAsset.RouteKind != SceneRouteKind.Gameplay)
            {
                FailFast(macroRouteId, routeAsset.RouteKind, signature, reason, "LevelSwapLocal invoked for non-gameplay route.");
            }

            if (routeAsset.LevelCollection == null)
            {
                FailFast(macroRouteId, routeAsset.RouteKind, signature, reason, "Gameplay route without LevelCollection.");
            }

            if (!routeAsset.LevelCollection.TryValidateRuntime(out string collectionError))
            {
                FailFast(macroRouteId, routeAsset.RouteKind, signature, reason, $"Gameplay route LevelCollection invalid. detail='{collectionError}'.");
            }

            return routeAsset.LevelCollection;
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

        private static string NormalizeSwapReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "LevelFlow/SwapLevelLocal" : reason.Trim();

        private static void PublishLevelSelected(LevelDefinitionAsset levelRef, SceneRouteId macroRouteId, string reason, int selectionVersion, string levelSignature)
        {
            EventBus<LevelSelectedEvent>.Raise(new LevelSelectedEvent(
                macroRouteId,
                levelRef,
                selectionVersion,
                reason,
                levelSignature));

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][Level] LevelSelectedEventPublished levelRef='{(levelRef != null ? levelRef.name : "<none>")}' macroRouteId='{macroRouteId}' reason='{reason}' v='{selectionVersion}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);
        }

        private static void PublishLevelSwapLocalApplied(LevelDefinitionAsset levelRef, SceneRouteId macroRouteId, string reason, int selectionVersion, string levelSignature)
        {
            EventBus<LevelSwapLocalAppliedEvent>.Raise(new LevelSwapLocalAppliedEvent(
                levelRef,
                macroRouteId,
                reason,
                selectionVersion,
                levelSignature));
        }

        private static string ComputeSignature(SceneRouteId routeId, SceneRouteKind routeKind, string reason)
        {
            return $"{routeId}|{routeKind}|{reason}";
        }

        private static string CreateLevelSignature(LevelDefinitionAsset levelRef, SceneRouteId routeId, string reason)
        {
            string levelName = levelRef != null ? levelRef.name : "<null>";
            return $"level:{levelName}|route:{routeId}|reason:{reason}";
        }

        private static void FailFast(SceneRouteId routeId, SceneRouteKind routeKind, string signature, string reason, string detail)
        {
            HardFailFastH1.Trigger(typeof(LevelSwapLocalService),
                $"[FATAL][H1][LevelFlow] LevelSwapLocal configuration error. routeId='{routeId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{detail}'");
        }
    }
}
