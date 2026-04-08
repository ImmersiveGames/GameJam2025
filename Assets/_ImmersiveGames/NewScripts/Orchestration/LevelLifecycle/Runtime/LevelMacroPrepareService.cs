using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelMacroPrepareService : ILevelMacroPrepareService
    {
        private const string DefaultReason = "SceneFlow/LevelPrepare";

        private readonly IRestartContextService _restartContextService;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;
        private readonly ILevelFlowContentService _levelFlowContentService;
        private readonly IPhaseDefinitionSelectionService _phaseDefinitionSelectionService;

        public LevelMacroPrepareService(
            IRestartContextService restartContextService,
            ISceneCompositionExecutor sceneCompositionExecutor,
            ILevelFlowContentService levelFlowContentService,
            IPhaseDefinitionSelectionService phaseDefinitionSelectionService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _sceneCompositionExecutor = sceneCompositionExecutor ?? throw new ArgumentNullException(nameof(sceneCompositionExecutor));
            _levelFlowContentService = levelFlowContentService ?? throw new ArgumentNullException(nameof(levelFlowContentService));
            _phaseDefinitionSelectionService = phaseDefinitionSelectionService ?? throw new ArgumentNullException(nameof(phaseDefinitionSelectionService));
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
            PhaseDefinitionAsset selectedPhaseDefinitionRef = _phaseDefinitionSelectionService.ResolveOrFail();

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
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionPreparedSnapshotIgnored macroRouteId='{macroRouteId}' snapshotLevelRef='{(currentSnapshot.LevelRef != null ? currentSnapshot.LevelRef.name : "<none>")}' snapshotRouteId='{currentSnapshot.MacroRouteId}' reason='not_in_collection_or_macro'.",
                    DebugUtility.Colors.Info);
            }

            bool useSnapshot = snapshotBelongsToMacro;
            string source = useSnapshot ? "snapshot" : "catalog_index_0";

            LevelDefinitionAsset selectedLevelRef = _levelFlowContentService.ResolveSelectedLevelDefinitionOrFail(levelCollection, useSnapshot, currentSnapshot, macroRouteId, routeKind, prepareSignature, normalizedReason);
            selectedLevelRef.ValidateOrFailFast($"LevelPrepare routeId='{macroRouteId}' reason='{normalizedReason}'");

            if (!useSnapshot)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionDefaultSelected source='catalog_index_0' index=0 phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' compatLevelRef='{selectedLevelRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' signature='{prepareSignature}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }

            int selectionVersion = hasLastSnapshot
                ? Math.Max(lastSnapshot.SelectionVersion + 1, 1)
                : 1;

            if (!hasCurrentSnapshot && hasLastSnapshot)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][GameplaySessionFlow][LevelLifecycle] SelectionVersionSource source='last_snapshot' prev='{lastSnapshot.SelectionVersion}' next='{selectionVersion}' reason='{normalizedReason}'.",
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
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionSelectedCanonical phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' contentId='{localContentId}' v='{selectionVersion}' macroRouteId='{macroRouteId}' signature='{levelSignature}' reason='{normalizedReason}' snapshot='{gameplayStartSnapshot}'.",
                DebugUtility.Colors.Info);

            PhaseDefinitionSelectedEvent phaseSelectedEvent = new PhaseDefinitionSelectedEvent(
                selectedPhaseDefinitionRef,
                macroRouteId,
                routeRef,
                selectionVersion,
                normalizedReason);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionSelected rail='canonical' phaseId='{phaseSelectedEvent.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' routeId='{macroRouteId}' v='{selectionVersion}' reason='{normalizedReason}' selectionSignature='{phaseSelectedEvent.SelectionSignature}'.",
                DebugUtility.Colors.Info);

            EventBus<PhaseDefinitionSelectedEvent>.Raise(phaseSelectedEvent);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] LevelSelectedCompat rail='compat-only' phaseId='{selectedPhaseDefinitionRef.PhaseId}' levelRef='{selectedLevelRef.name}' macroRouteId='{macroRouteId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            EventBus<LevelSelectedEvent>.Raise(selectedEvent);

            string macroActiveSceneName = SceneManager.GetActiveScene().name;
            SceneCompositionRequest phaseCompositionRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                selectedPhaseDefinitionRef,
                normalizedReason,
                phaseSelectedEvent.SelectionSignature);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionSceneCompositionRequestBuilt phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' macroActiveScene='{macroActiveSceneName}' scenesToLoad=[{string.Join(",", phaseCompositionRequest.ScenesToLoad)}] scenesToUnload=[{string.Join(",", phaseCompositionRequest.ScenesToUnload)}] activeScene='{phaseCompositionRequest.ActiveScene}' correlationId='{phaseCompositionRequest.CorrelationId}' reason='{phaseCompositionRequest.Reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(phaseCompositionRequest, ct);

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                selectedPhaseDefinitionRef,
                phaseCompositionRequest.ScenesToLoad,
                phaseCompositionRequest.ActiveScene);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionContentApplied phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} macroRouteId='{macroRouteId}' routeKind='{routeKind}' correlationId='{phaseCompositionRequest.CorrelationId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionPrepared source='{source}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' compatLevelRef='{selectedLevelRef.name}' contentId='{localContentId}' v='{selectionVersion}' signature='{prepareSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionEntered source='GameplaySessionFlow' phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' compatLevelRef='{selectedLevelRef.name}' contentId='{localContentId}' v='{selectionVersion}' signature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            EventBus<LevelEnteredEvent>.Raise(new LevelEnteredEvent(
                selectedLevelRef.CreateIntroStageSession(
                    localContentId,
                    normalizedReason,
                    selectionVersion,
                    levelSignature),
                "GameplaySessionFlow",
                routeKind));
        }

        private async Task ClearActiveLevelAsync(
            SceneRouteId destinationRouteId,
            SceneRouteKind destinationRouteKind,
            string signature,
            string reason,
            CancellationToken ct)
        {
            bool hasActivePhaseContent = PhaseContentSceneRuntimeApplier.HasActiveAppliedPhaseContent;

            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef ||
                snapshot.LevelRef == null)
            {
                if (hasActivePhaseContent)
                {
                    FailFastConfig(destinationRouteId, destinationRouteKind, signature, reason,
                        $"Phase clear state mismatch: no active snapshot but phase applier reports active content. activeCount='{PhaseContentSceneRuntimeApplier.ActiveAppliedSceneCount}'.");
                }

                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionClearSkipped reason='no_active_phase' destinationRouteId='{destinationRouteId}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            LevelDefinitionAsset previousLevelRef = snapshot.LevelRef;
            previousLevelRef.ValidateOrFailFast($"LevelClear destinationRouteId='{destinationRouteId}' reason='{reason}'");

            SceneCompositionRequest phaseClearRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateClearRequest(reason, signature);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionSceneCompositionClearRequestBuilt activeScenes=[{string.Join(",", phaseClearRequest.ScenesToUnload)}] correlationId='{phaseClearRequest.CorrelationId}' reason='{phaseClearRequest.Reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(phaseClearRequest, ct);

            PhaseContentSceneRuntimeApplier.RecordCleared();
            _restartContextService.Clear($"LevelFlow/ClearOnMacroExit/{reason}");

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionContentCleared macroRouteId='{destinationRouteId}' previousLevelRef='{previousLevelRef.name}' scenesRemoved={compositionResult.ScenesRemoved} reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private static string ComputeSignature(SceneRouteId routeId, SceneRouteKind routeKind, string reason)
        {
            return $"{routeId}|{routeKind}|{reason ?? DefaultReason}";
        }

        private static void FailFastConfig(SceneRouteId routeId, SceneRouteKind routeKind, string signature, string reason, string configReason)
        {
            HardFailFastH1.Trigger(typeof(LevelMacroPrepareService),
                $"[FATAL][H1][LevelLifecycle] LevelPrepare configuration error. routeId='{routeId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{configReason}'");
        }
    }
}
