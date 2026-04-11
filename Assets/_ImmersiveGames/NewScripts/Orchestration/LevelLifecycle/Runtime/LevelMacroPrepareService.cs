using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
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
        private const string DefaultReason = "GameplaySessionFlow/Prepare";

        private readonly IRestartContextService _restartContextService;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;
        private readonly IPhaseDefinitionSelectionService _phaseDefinitionSelectionService;
        private readonly GameplayPhaseFlowService _gameplayPhaseFlowService;
        private readonly bool _phaseEnabled;

        public LevelMacroPrepareService(
            IRestartContextService restartContextService,
            ISceneCompositionExecutor sceneCompositionExecutor,
            IPhaseDefinitionSelectionService phaseDefinitionSelectionService = null,
            GameplayPhaseFlowService gameplayPhaseFlowService = null,
            bool phaseEnabled = false)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _sceneCompositionExecutor = sceneCompositionExecutor ?? throw new ArgumentNullException(nameof(sceneCompositionExecutor));
            _phaseDefinitionSelectionService = phaseDefinitionSelectionService;
            _gameplayPhaseFlowService = gameplayPhaseFlowService;
            _phaseEnabled = phaseEnabled;
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

            if (routeKind == SceneRouteKind.Gameplay)
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
            if (!_phaseEnabled)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSkipped reason='route_context_phase_disabled' macroRouteId='{macroRouteId}' routeKind='{routeKind}' signature='{prepareSignature}' routeMode='phase-disabled'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (_phaseDefinitionSelectionService == null)
            {
                FailFastPhaseEnabledRoute(macroRouteId, routeKind, prepareSignature, normalizedReason,
                    "IPhaseDefinitionSelectionService missing for phase-enabled route/context.");
            }

            if (_gameplayPhaseFlowService == null)
            {
                FailFastPhaseEnabledRoute(macroRouteId, routeKind, prepareSignature, normalizedReason,
                    "GameplayPhaseFlowService missing for phase-owned handoff publication.");
            }

            PhaseDefinitionAsset selectedPhaseDefinitionRef = _phaseDefinitionSelectionService.ResolveOrFail();

            GameplayStartSnapshot currentSnapshot = GameplayStartSnapshot.Empty;
            bool hasCurrentSnapshot = _restartContextService.TryGetCurrent(out currentSnapshot) && currentSnapshot.IsValid;

            GameplayStartSnapshot lastSnapshot = GameplayStartSnapshot.Empty;
            bool hasLastSnapshot = _restartContextService.TryGetLastGameplayStartSnapshot(out lastSnapshot) &&
                                   lastSnapshot.IsValid;

            bool reusedRestartSnapshot = hasCurrentSnapshot &&
                                         currentSnapshot.MacroRouteId.IsValid &&
                                         currentSnapshot.MacroRouteId == macroRouteId &&
                                         currentSnapshot.HasPhaseDefinitionRef &&
                                         ReferenceEquals(currentSnapshot.PhaseDefinitionRef, selectedPhaseDefinitionRef);

            string source = reusedRestartSnapshot ? "restart_snapshot" : "phase_catalog_index_0";

            if (hasCurrentSnapshot && !reusedRestartSnapshot)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSnapshotIgnored macroRouteId='{macroRouteId}' snapshotPhaseRef='{(currentSnapshot.HasPhaseDefinitionRef ? currentSnapshot.PhaseDefinitionRef.name : "<none>")}' snapshotRouteId='{currentSnapshot.MacroRouteId}' reason='not_selected_for_compat_payload'.",
                    DebugUtility.Colors.Info);
            }

            if (!reusedRestartSnapshot)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPreparePhaseResolved source='phase_catalog_index_0' phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' signature='{prepareSignature}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }

            int selectionVersion = hasLastSnapshot
                ? Math.Max(lastSnapshot.SelectionVersion + 1, 1)
                : 1;

            if (!hasCurrentSnapshot && hasLastSnapshot)
            {
                DebugUtility.Log<LevelMacroPrepareService>(
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSelectionVersionSource source='last_snapshot' prev='{lastSnapshot.SelectionVersion}' next='{selectionVersion}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }

            PhaseDefinitionSelectedEvent phaseSelectedEvent = _gameplayPhaseFlowService.PublishPhaseDefinitionSelected(
                selectedPhaseDefinitionRef,
                macroRouteId,
                routeRef,
                selectionVersion,
                normalizedReason);
            string macroActiveSceneName = SceneManager.GetActiveScene().name;
            SceneCompositionRequest phaseCompositionRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                selectedPhaseDefinitionRef,
                normalizedReason,
                phaseSelectedEvent.SelectionSignature);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSceneCompositionRequestBuilt phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' macroActiveScene='{macroActiveSceneName}' scenesToLoad=[{string.Join(",", phaseCompositionRequest.ScenesToLoad)}] scenesToUnload=[{string.Join(",", phaseCompositionRequest.ScenesToUnload)}] activeScene='{phaseCompositionRequest.ActiveScene}' correlationId='{phaseCompositionRequest.CorrelationId}' reason='{phaseCompositionRequest.Reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(phaseCompositionRequest, ct);

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                selectedPhaseDefinitionRef,
                phaseCompositionRequest.ScenesToLoad,
                phaseCompositionRequest.ActiveScene,
                "GameplaySessionFlow");

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareContentApplied phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} macroRouteId='{macroRouteId}' routeKind='{routeKind}' correlationId='{phaseCompositionRequest.CorrelationId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        private static void FailFastPhaseEnabledRoute(
            SceneRouteId macroRouteId,
            SceneRouteKind routeKind,
            string signature,
            string reason,
            string detail)
        {
            HardFailFastH1.Trigger(typeof(LevelMacroPrepareService),
                $"[FATAL][H1][GameplaySessionFlow] phase-enabled route/context requires Phase support. routeId='{macroRouteId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{detail}'");
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
                !snapshot.IsValid)
            {
                if (hasActivePhaseContent)
                {
                    FailFastConfig(destinationRouteId, destinationRouteKind, signature, reason,
                        $"Phase clear state mismatch: no active snapshot but phase applier reports active content. activeCount='{PhaseContentSceneRuntimeApplier.ActiveAppliedSceneCount}'.");
                }

                DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareClearSkipped reason='no_active_phase' destinationRouteId='{destinationRouteId}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            PhaseDefinitionAsset previousPhaseRef = snapshot.PhaseDefinitionRef;
            if (previousPhaseRef == null)
            {
                FailFastConfig(destinationRouteId, destinationRouteKind, signature, reason,
                    "Phase clear snapshot is missing phaseDefinitionRef.");
            }

            previousPhaseRef.ValidateOrFail($"PhaseClear destinationRouteId='{destinationRouteId}' reason='{reason}'");

            SceneCompositionRequest phaseClearRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateClearRequest(reason, signature);

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSceneCompositionClearRequestBuilt activeScenes=[{string.Join(",", phaseClearRequest.ScenesToUnload)}] correlationId='{phaseClearRequest.CorrelationId}' reason='{phaseClearRequest.Reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(phaseClearRequest, ct);

            PhaseContentSceneRuntimeApplier.RecordCleared();
            _restartContextService.Clear($"GameplaySessionFlow/ClearOnMacroExit/{reason}");

            DebugUtility.Log<LevelMacroPrepareService>(
                $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareContentCleared macroRouteId='{destinationRouteId}' previousContentName='{(previousPhaseRef != null ? previousPhaseRef.name : "<none>")}' scenesRemoved={compositionResult.ScenesRemoved} reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private static string ComputeSignature(SceneRouteId routeId, SceneRouteKind routeKind, string reason)
        {
            return $"{routeId}|{routeKind}|{reason ?? DefaultReason}";
        }

        private static void FailFastConfig(SceneRouteId routeId, SceneRouteKind routeKind, string signature, string reason, string configReason)
        {
            HardFailFastH1.Trigger(typeof(LevelMacroPrepareService),
                $"[FATAL][H1][GameplaySessionFlow] GameplaySessionPrepare configuration error. routeId='{routeId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{configReason}'");
        }
    }
}

