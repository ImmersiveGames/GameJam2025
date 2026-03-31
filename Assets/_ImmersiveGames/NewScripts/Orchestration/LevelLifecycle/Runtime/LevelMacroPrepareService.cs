using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelMacroPrepareService : ILevelMacroPrepareService
    {
        private const string DefaultReason = "SceneFlow/LevelPrepare";

        private readonly IRestartContextService _restartContextService;
        private readonly IWorldResetCommands _worldResetCommands;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;
        private readonly ILevelFlowContentService _levelFlowContentService;

        public LevelMacroPrepareService(
            IRestartContextService restartContextService,
            IWorldResetCommands worldResetCommands,
            ISceneCompositionExecutor sceneCompositionExecutor,
            ILevelFlowContentService levelFlowContentService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _worldResetCommands = worldResetCommands ?? throw new ArgumentNullException(nameof(worldResetCommands));
            _sceneCompositionExecutor = sceneCompositionExecutor ?? throw new ArgumentNullException(nameof(sceneCompositionExecutor));
            _levelFlowContentService = levelFlowContentService ?? throw new ArgumentNullException(nameof(levelFlowContentService));
        }

        public async Task PrepareOrClearAsync(SceneRouteId macroRouteId, SceneRouteDefinitionAsset routeRef, string reason, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? DefaultReason : reason.Trim();
            if (!macroRouteId.IsValid)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Unspecified, ComputeSignature(macroRouteId, SceneRouteKind.Unspecified, normalizedReason), normalizedReason, "Macro route id is invalid.");
            }

            if (routeRef == null)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Unspecified, ComputeSignature(macroRouteId, SceneRouteKind.Unspecified, normalizedReason), normalizedReason, "Macro route ref is null.");
            }

            if (routeRef.RouteId != macroRouteId)
            {
                FailFastConfig(macroRouteId, routeRef.RouteKind, ComputeSignature(macroRouteId, routeRef.RouteKind, normalizedReason), normalizedReason, $"Macro route ref mismatch. routeRefRouteId='{routeRef.RouteId}'.");
            }

            SceneRouteKind routeKind = routeRef.RouteKind;
            string prepareSignature = ComputeSignature(macroRouteId, routeKind, normalizedReason);

            if (routeRef.LevelCollection != null && routeKind == SceneRouteKind.Gameplay)
            {
                await PrepareGameplayAsync(routeRef, macroRouteId, routeKind, prepareSignature, normalizedReason, ct);
                return;
            }

            await ClearActiveLevelAsync(macroRouteId, routeKind, prepareSignature, normalizedReason, ct);
        }

        private async Task PrepareGameplayAsync(
            SceneRouteDefinitionAsset routeRef,
            SceneRouteId macroRouteId,
            SceneRouteKind routeKind,
            string prepareSignature,
            string normalizedReason,
            CancellationToken ct)
        {
            LevelCollectionAsset levelCollection = _levelFlowContentService.ResolveLevelCollectionOrFail(routeRef, macroRouteId, prepareSignature, normalizedReason);

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

            LevelDefinitionAsset selectedLevelRef = _levelFlowContentService.ResolveSelectedLevelDefinitionOrFail(levelCollection, useSnapshot, currentSnapshot, macroRouteId, routeKind, prepareSignature, normalizedReason);
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
                ? _levelFlowContentService.BuildLocalContentId(selectedLevelRef, currentSnapshot.LocalContentId)
                : _levelFlowContentService.BuildLocalContentId(selectedLevelRef);
            string levelSignature = _levelFlowContentService.BuildLevelSignature(selectedLevelRef, macroRouteId, normalizedReason);

            LevelSelectedEvent selectedEvent = new LevelSelectedEvent(
                macroRouteId,
                routeRef,
                selectedLevelRef,
                selectionVersion,
                localContentId,
                normalizedReason,
                levelSignature);

            GameplayStartSnapshot gameplayStartSnapshot = GameplayStartSnapshot.FromLevelSelectedEvent(selectedEvent);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelSelectedCanonical levelRef='{selectedLevelRef.name}' contentId='{localContentId}' v='{selectionVersion}' macroRouteId='{macroRouteId}' signature='{levelSignature}' reason='{normalizedReason}' snapshot='{gameplayStartSnapshot}'.",
                DebugUtility.Colors.Info);

            EventBus<LevelSelectedEvent>.Raise(selectedEvent);

            ct.ThrowIfCancellationRequested();
            await _worldResetCommands.ResetLevelAsync(
                selectedLevelRef,
                normalizedReason,
                new LevelContextSignature(levelSignature),
                ct);
            ct.ThrowIfCancellationRequested();

            LevelDefinitionAsset previousLevelRef = snapshotBelongsToMacro ? currentSnapshot.LevelRef : null;
            LevelDefinitionAsset fallbackAppliedLevelRef = previousLevelRef == null ? LevelAdditiveSceneRuntimeApplier.ActiveAppliedLevelRef : null;
            LevelDefinitionAsset compositionPreviousLevelRef = previousLevelRef ?? fallbackAppliedLevelRef;
            string previousSource = previousLevelRef != null
                ? "snapshot"
                : (LevelAdditiveSceneRuntimeApplier.HasActiveAppliedLevelContent ? "applier_state" : "none");
            string previousRefLabel = previousLevelRef != null
                ? previousLevelRef.name
                : (fallbackAppliedLevelRef != null ? fallbackAppliedLevelRef.name : "<none>");

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelPreparePreviousResolved source='{previousSource}' prevLevelRef='{previousRefLabel}' targetLevelRef='{selectedLevelRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' signature='{prepareSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(
                LevelSceneCompositionRequestFactory.CreateApplyRequest(
                    compositionPreviousLevelRef,
                    selectedLevelRef,
                    normalizedReason,
                    levelSignature),
                ct);

            LevelAdditiveSceneRuntimeApplier.RecordAppliedLevel(selectedLevelRef);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelApplied levelRef='{selectedLevelRef.name}' contentId='{localContentId}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} macroRouteId='{macroRouteId}' routeKind='{routeKind}' signature='{prepareSignature}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelPrepared source='{source}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' levelRef='{selectedLevelRef.name}' contentId='{localContentId}' v='{selectionVersion}' signature='{prepareSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelEntered source='LevelPrepare' levelRef='{selectedLevelRef.name}' contentId='{localContentId}' v='{selectionVersion}' signature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            EventBus<LevelEnteredEvent>.Raise(new LevelEnteredEvent(
                new LevelIntroStageSession(
                    selectedLevelRef,
                    macroRouteId,
                    routeRef,
                    localContentId,
                    normalizedReason,
                    selectionVersion,
                    levelSignature,
                    selectedLevelRef.IntroPresenterPrefab,
                    selectedLevelRef.HasIntroStage ? LevelIntroStageDisposition.HasIntro : LevelIntroStageDisposition.NoIntro),
                "LevelPrepare"));
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
                LevelSceneCompositionRequestFactory.CreateClearRequest(
                    previousLevelRef,
                    reason,
                    signature),
                ct);

            LevelAdditiveSceneRuntimeApplier.RecordCleared();
            _restartContextService.Clear($"LevelFlow/ClearOnMacroExit/{reason}");

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][LevelFlow] LevelCleared macroRouteId='{destinationRouteId}' previousLevelRef='{previousLevelRef.name}' scenesRemoved={compositionResult.ScenesRemoved} reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private static string ComputeSignature(SceneRouteId routeId, SceneRouteKind routeKind, string reason)
        {
            return $"{routeId}|{routeKind}|{reason ?? DefaultReason}";
        }

        private static void FailFastConfig(SceneRouteId routeId, SceneRouteKind routeKind, string signature, string reason, string configReason)
        {
            HardFailFastH1.Trigger(typeof(LevelMacroPrepareService),
                $"[FATAL][H1][LevelFlow] LevelPrepare configuration error. routeId='{routeId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{configReason}'");
        }
    }
}
