using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SceneComposition;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelMacroPrepareService : ILevelMacroPrepareService
    {
        private const string DefaultReason = "SceneFlow/LevelPrepare";

        private readonly IRestartContextService _restartContextService;
        private readonly IWorldResetCommands _worldResetCommands;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;
        private readonly SceneRouteCatalogAsset _sceneRouteCatalog;

        public LevelMacroPrepareService(
            IRestartContextService restartContextService,
            IWorldResetCommands worldResetCommands,
            ISceneCompositionExecutor sceneCompositionExecutor,
            SceneRouteCatalogAsset sceneRouteCatalog = null)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _worldResetCommands = worldResetCommands ?? throw new ArgumentNullException(nameof(worldResetCommands));
            _sceneCompositionExecutor = sceneCompositionExecutor ?? throw new ArgumentNullException(nameof(sceneCompositionExecutor));
            _sceneRouteCatalog = sceneRouteCatalog ?? throw new ArgumentNullException(nameof(sceneRouteCatalog));
        }

        public async Task PrepareOrClearAsync(SceneRouteId macroRouteId, string reason, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? DefaultReason : reason.Trim();
            if (!macroRouteId.IsValid)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Unspecified, ComputeSignature(macroRouteId, SceneRouteKind.Unspecified, normalizedReason), normalizedReason, "Macro route id is invalid.");
            }

            SceneRouteDefinitionAsset routeAsset = ResolveRouteAssetOrFail(macroRouteId, normalizedReason);
            SceneRouteKind routeKind = routeAsset.RouteKind;
            string prepareSignature = ComputeSignature(macroRouteId, routeKind, normalizedReason);

            if (routeAsset.LevelCollection != null && routeKind == SceneRouteKind.Gameplay)
            {
                await PrepareGameplayAsync(routeAsset, macroRouteId, routeKind, prepareSignature, normalizedReason, ct);
                return;
            }

            await ClearActiveLevelAsync(macroRouteId, routeKind, prepareSignature, normalizedReason, ct);
        }

        private async Task PrepareGameplayAsync(
            SceneRouteDefinitionAsset routeAsset,
            SceneRouteId macroRouteId,
            SceneRouteKind routeKind,
            string prepareSignature,
            string normalizedReason,
            CancellationToken ct)
        {
            LevelCollectionAsset levelCollection = ResolveLevelCollectionOrFail(routeAsset, macroRouteId, prepareSignature, normalizedReason);

            GameplayStartSnapshot currentSnapshot = GameplayStartSnapshot.Empty;
            bool hasCurrentSnapshot = _restartContextService.TryGetCurrent(out currentSnapshot) && currentSnapshot.IsValid && currentSnapshot.HasLevelRef;

            GameplayStartSnapshot lastSnapshot = GameplayStartSnapshot.Empty;
            bool hasLastSnapshot = _restartContextService.TryGetLastGameplayStartSnapshot(out lastSnapshot) &&
                                   lastSnapshot.IsValid &&
                                   lastSnapshot.HasLevelRef;

            bool snapshotBelongsToMacro = hasCurrentSnapshot &&
                                          currentSnapshot.MacroRouteId.IsValid &&
                                          currentSnapshot.MacroRouteId == macroRouteId &&
                                          currentSnapshot.LevelRef != null &&
                                          levelCollection.Contains(currentSnapshot.LevelRef);

            if (hasCurrentSnapshot && !snapshotBelongsToMacro)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][LevelFlow] LevelPreparedSnapshotIgnored macroRouteId='{macroRouteId}' snapshotLevelRef='{(currentSnapshot.LevelRef != null ? currentSnapshot.LevelRef.name : "<none>")}' snapshotRouteId='{currentSnapshot.MacroRouteId}' reason='not_in_collection_or_macro'.",
                    DebugUtility.Colors.Info);
            }

            bool useSnapshot = snapshotBelongsToMacro;
            string source = useSnapshot ? "snapshot" : "catalog_index_0";

            LevelDefinitionAsset selectedLevelRef = ResolveSelectedLevelDefinitionOrFail(levelCollection, useSnapshot, currentSnapshot, macroRouteId, routeKind, prepareSignature, normalizedReason);
            selectedLevelRef.ValidateOrFailFast($"LevelPrepare routeId='{macroRouteId}' reason='{normalizedReason}'");

            if (!useSnapshot)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][LevelFlow] LevelDefaultSelected source='catalog_index_0' index=0 levelRef='{selectedLevelRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' signature='{prepareSignature}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }

            int selectionVersion = hasLastSnapshot
                ? Math.Max(lastSnapshot.SelectionVersion + 1, 1)
                : 1;

            if (!hasCurrentSnapshot && hasLastSnapshot)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][LevelFlow] SelectionVersionSource source='last_snapshot' prev='{lastSnapshot.SelectionVersion}' next='{selectionVersion}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }

            string localContentId = useSnapshot && currentSnapshot.HasLocalContentId
                ? LevelFlowContentDefaults.Normalize(currentSnapshot.LocalContentId, selectedLevelRef)
                : LevelFlowContentDefaults.BuildCanonicalLevelContentId(selectedLevelRef);
            string levelSignature = CreateLevelSignature(selectedLevelRef, macroRouteId, normalizedReason);

            EventBus<LevelSelectedEvent>.Raise(new LevelSelectedEvent(
                macroRouteId,
                selectedLevelRef,
                selectionVersion,
                localContentId,
                normalizedReason,
                levelSignature));

            ct.ThrowIfCancellationRequested();
            await _worldResetCommands.ResetLevelAsync(
                selectedLevelRef,
                normalizedReason,
                new LevelContextSignature(levelSignature),
                ct);
            ct.ThrowIfCancellationRequested();

            LevelDefinitionAsset previousLevelRef = snapshotBelongsToMacro ? currentSnapshot.LevelRef : null;
            string previousSource = previousLevelRef != null
                ? "snapshot"
                : (LevelAdditiveSceneRuntimeApplier.HasActiveAppliedLevelContent ? "applier_state" : "none");
            string previousRefLabel = previousLevelRef != null
                ? previousLevelRef.name
                : (LevelAdditiveSceneRuntimeApplier.ActiveAppliedLevelRef != null ? LevelAdditiveSceneRuntimeApplier.ActiveAppliedLevelRef.name : "<none>");

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelPreparePreviousResolved source='{previousSource}' prevLevelRef='{previousRefLabel}' targetLevelRef='{selectedLevelRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' signature='{prepareSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(
                new SceneCompositionRequest(
                    SceneCompositionScope.Local,
                    normalizedReason,
                    levelSignature,
                    previousLevelRef,
                    selectedLevelRef),
                ct);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelApplied levelRef='{selectedLevelRef.name}' contentId='{localContentId}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} macroRouteId='{macroRouteId}' routeKind='{routeKind}' signature='{prepareSignature}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelPrepared source='{source}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' levelRef='{selectedLevelRef.name}' contentId='{localContentId}' v='{selectionVersion}' signature='{prepareSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        private async Task ClearActiveLevelAsync(
            SceneRouteId destinationRouteId,
            SceneRouteKind destinationRouteKind,
            string signature,
            string reason,
            CancellationToken ct)
        {
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef ||
                snapshot.LevelRef == null)
            {
                if (LevelAdditiveSceneRuntimeApplier.HasActiveAppliedLevelContent)
                {
                    FailFastConfig(destinationRouteId, destinationRouteKind, signature, reason,
                        $"Level clear state mismatch: no active snapshot but applier reports active content. activeCount='{LevelAdditiveSceneRuntimeApplier.ActiveAppliedSceneCount}'.");
                }

                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][LevelFlow] LevelClearSkipped reason='no_active_level' destinationRouteId='{destinationRouteId}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            LevelDefinitionAsset previousLevelRef = snapshot.LevelRef;
            previousLevelRef.ValidateOrFailFast($"LevelClear destinationRouteId='{destinationRouteId}' reason='{reason}'");

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(
                new SceneCompositionRequest(
                    SceneCompositionScope.Local,
                    reason,
                    signature,
                    previousLevelRef,
                    targetLevelRef: null),
                ct);

            _restartContextService.Clear($"LevelFlow/ClearOnMacroExit/{reason}");

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelCleared macroRouteId='{destinationRouteId}' previousLevelRef='{previousLevelRef.name}' scenesRemoved={compositionResult.ScenesRemoved} reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private static LevelDefinitionAsset ResolveSelectedLevelDefinitionOrFail(
            LevelCollectionAsset levelCollection,
            bool useSnapshot,
            GameplayStartSnapshot snapshot,
            SceneRouteId macroRouteId,
            SceneRouteKind routeKind,
            string signature,
            string reason)
        {
            if (useSnapshot && snapshot.LevelRef != null)
            {
                return snapshot.LevelRef;
            }

            LevelDefinitionAsset defaultLevel = levelCollection.GetDefaultOrNull();
            if (defaultLevel == null)
            {
                FailFastConfig(macroRouteId, routeKind, signature, reason, "Gameplay route LevelCollection default (index 0) is missing.");
            }

            return defaultLevel;
        }

        private SceneRouteDefinitionAsset ResolveRouteAssetOrFail(SceneRouteId macroRouteId, string reason)
        {
            string signature = ComputeSignature(macroRouteId, SceneRouteKind.Unspecified, reason);

            if (_sceneRouteCatalog == null)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Unspecified, signature, reason, "BootstrapConfig.SceneRouteCatalog is null.");
            }

            if (!_sceneRouteCatalog.TryGetAsset(macroRouteId, out SceneRouteDefinitionAsset routeAsset) || routeAsset == null)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Unspecified, signature, reason, "RouteAsset missing from SceneRouteCatalogAsset.");
            }

            return routeAsset;
        }

        private LevelCollectionAsset ResolveLevelCollectionOrFail(
            SceneRouteDefinitionAsset routeAsset,
            SceneRouteId macroRouteId,
            string signature,
            string reason)
        {
            if (routeAsset.RouteKind != SceneRouteKind.Gameplay)
            {
                FailFastConfig(macroRouteId, routeAsset.RouteKind, signature, reason, "LevelPrepare invoked for non-gameplay route.");
            }

            if (routeAsset.LevelCollection == null)
            {
                FailFastConfig(macroRouteId, routeAsset.RouteKind, signature, reason, "Gameplay route without LevelCollection.");
            }

            if (!routeAsset.LevelCollection.TryValidateRuntime(out string collectionError))
            {
                FailFastConfig(macroRouteId, routeAsset.RouteKind, signature, reason, $"Gameplay route LevelCollection invalid. detail='{collectionError}'.");
            }

            return routeAsset.LevelCollection;
        }

        private static string ComputeSignature(SceneRouteId routeId, SceneRouteKind routeKind, string reason)
        {
            return $"{routeId}|{routeKind}|{reason ?? DefaultReason}";
        }

        private static string CreateLevelSignature(LevelDefinitionAsset levelRef, SceneRouteId routeId, string reason)
        {
            string levelName = levelRef != null ? levelRef.name : "<null>";
            return $"level:{levelName}|route:{routeId}|reason:{reason}";
        }

        private static void FailFastConfig(SceneRouteId routeId, SceneRouteKind routeKind, string signature, string reason, string configReason)
        {
            HardFailFastH1.Trigger(typeof(LevelMacroPrepareService),
                $"[FATAL][H1][LevelFlow] LevelPrepare configuration error. routeId='{routeId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{configReason}'");
        }
    }
}
