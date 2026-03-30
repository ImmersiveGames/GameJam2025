using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Infrastructure.SceneComposition;
using _ImmersiveGames.NewScripts.Core.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelSwapLocalService : ILevelSwapLocalService
    {
        private readonly IRestartContextService _restartContextService;
        private readonly IWorldResetCommands _worldResetCommands;
        private readonly ISceneCompositionExecutor _sceneCompositionExecutor;
        private readonly ILevelFlowContentService _levelFlowContentService;
        private readonly ISimulationGateService _simulationGateService;

        public LevelSwapLocalService(
            IRestartContextService restartContextService,
            IWorldResetCommands worldResetCommands,
            ISceneCompositionExecutor sceneCompositionExecutor,
            ILevelFlowContentService levelFlowContentService,
            ISimulationGateService simulationGateService = null)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _worldResetCommands = worldResetCommands ?? throw new ArgumentNullException(nameof(worldResetCommands));
            _sceneCompositionExecutor = sceneCompositionExecutor ?? throw new ArgumentNullException(nameof(sceneCompositionExecutor));
            _levelFlowContentService = levelFlowContentService ?? throw new ArgumentNullException(nameof(levelFlowContentService));
            _simulationGateService = simulationGateService;
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
                !currentSnapshot.MacroRouteId.IsValid ||
                !currentSnapshot.HasLevelRef ||
                currentSnapshot.MacroRouteRef == null)
            {
                FailFast(SceneRouteId.None, SceneRouteKind.Unspecified, ComputeSignature(SceneRouteId.None, SceneRouteKind.Unspecified, normalizedReason), normalizedReason, "Missing runtime gameplay snapshot.");
            }

            SceneRouteId macroRouteId = currentSnapshot.MacroRouteId;
            SceneRouteDefinitionAsset routeAsset = currentSnapshot.MacroRouteRef;
            SceneRouteKind routeKind = routeAsset.RouteKind;
            string swapSignature = _levelFlowContentService.BuildLevelSignature(currentSnapshot.LevelRef, macroRouteId, normalizedReason);
            LevelCollectionAsset levelCollection = _levelFlowContentService.ResolveLevelCollectionOrFail(routeAsset, macroRouteId, swapSignature, normalizedReason);

            if (!levelCollection.Contains(targetLevelRef))
            {
                FailFast(macroRouteId, routeKind, swapSignature, normalizedReason, $"Target levelRef='{targetLevelRef.name}' is not part of route LevelCollection.");
            }

            targetLevelRef.ValidateOrFailFast($"LevelSwapLocal routeId='{macroRouteId}' reason='{normalizedReason}' targetLevelRef='{targetLevelRef.name}'");

            LevelDefinitionAsset fromLevelRef = currentSnapshot.LevelRef;
            int nextSelectionVersion = Math.Max(currentSnapshot.SelectionVersion + 1, 1);
            string localContentId = _levelFlowContentService.BuildLocalContentId(targetLevelRef);
            string levelSignature = _levelFlowContentService.BuildLevelSignature(targetLevelRef, macroRouteId, normalizedReason);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelSwapLocalStart fromLevelRef='{(fromLevelRef != null ? fromLevelRef.name : "<none>")}' toLevelRef='{targetLevelRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' contentId='{localContentId}' v='{nextSelectionVersion}' signature='{swapSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            using IDisposable gateHandle = AcquireSwapGate();

            PublishLevelSelected(targetLevelRef, macroRouteId, routeAsset, localContentId, normalizedReason, nextSelectionVersion, levelSignature);

            ct.ThrowIfCancellationRequested();
            await _worldResetCommands.ResetLevelAsync(targetLevelRef, normalizedReason, new LevelContextSignature(levelSignature), ct);
            ct.ThrowIfCancellationRequested();

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(
                LevelSceneCompositionRequestFactory.CreateApplyRequest(
                    fromLevelRef,
                    targetLevelRef,
                    normalizedReason,
                    levelSignature),
                ct);

            LevelAdditiveSceneRuntimeApplier.RecordAppliedLevel(targetLevelRef);

            PublishLevelSwapLocalApplied(targetLevelRef, macroRouteId, localContentId, normalizedReason, nextSelectionVersion, levelSignature);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelSwapLocalApplied fromLevelRef='{(fromLevelRef != null ? fromLevelRef.name : "<none>")}' toLevelRef='{targetLevelRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' contentId='{localContentId}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} v='{nextSelectionVersion}' signature='{swapSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelEntered source='LevelSwapLocal' levelRef='{targetLevelRef.name}' contentId='{localContentId}' v='{nextSelectionVersion}' signature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            EventBus<LevelEnteredEvent>.Raise(new LevelEnteredEvent(
                new LevelIntroStageSession(
                    targetLevelRef,
                    macroRouteId,
                    routeAsset,
                    localContentId,
                    normalizedReason,
                    nextSelectionVersion,
                    levelSignature,
                    targetLevelRef.IntroPresenterPrefab,
                    targetLevelRef.HasIntroStage ? LevelIntroStageDisposition.HasIntro : LevelIntroStageDisposition.NoIntro),
                "LevelSwapLocal"));
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

        private static void PublishLevelSelected(LevelDefinitionAsset levelRef, SceneRouteId macroRouteId, SceneRouteDefinitionAsset routeRef, string localContentId, string reason, int selectionVersion, string levelSignature)
        {
            EventBus<LevelSelectedEvent>.Raise(new LevelSelectedEvent(
                macroRouteId,
                routeRef,
                levelRef,
                selectionVersion,
                localContentId,
                reason,
                levelSignature));

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][Level] LevelSelectedEventPublished levelRef='{(levelRef != null ? levelRef.name : "<none>")}' macroRouteId='{macroRouteId}' contentId='{localContentId}' reason='{reason}' v='{selectionVersion}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);
        }

        private static void PublishLevelSwapLocalApplied(LevelDefinitionAsset levelRef, SceneRouteId macroRouteId, string localContentId, string reason, int selectionVersion, string levelSignature)
        {
            EventBus<LevelSwapLocalAppliedEvent>.Raise(new LevelSwapLocalAppliedEvent(
                levelRef,
                macroRouteId,
                localContentId,
                reason,
                selectionVersion,
                levelSignature));
        }

        private static string ComputeSignature(SceneRouteId routeId, SceneRouteKind routeKind, string reason)
        {
            return $"{routeId}|{routeKind}|{reason}";
        }

        private static void FailFast(SceneRouteId routeId, SceneRouteKind routeKind, string signature, string reason, string detail)
        {
            HardFailFastH1.Trigger(typeof(LevelSwapLocalService),
                $"[FATAL][H1][LevelFlow] LevelSwapLocal configuration error. routeId='{routeId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{detail}'");
        }
    }
}
