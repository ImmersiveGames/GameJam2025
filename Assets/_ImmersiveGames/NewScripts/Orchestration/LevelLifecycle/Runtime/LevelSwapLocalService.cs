using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
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

        public async Task SwapLocalAsync(PhaseDefinitionSelectedEvent phaseSelection, string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = NormalizeSwapReason(reason);
            if (!phaseSelection.IsValid ||
                phaseSelection.PhaseDefinitionRef == null ||
                phaseSelection.MacroRouteRef == null ||
                !phaseSelection.MacroRouteId.IsValid)
            {
                FailFast(SceneRouteId.None, SceneRouteKind.Unspecified, ComputeSignature(SceneRouteId.None, SceneRouteKind.Unspecified, normalizedReason), normalizedReason, "Target phase selection is invalid.");
            }

            if (phaseSelection.SelectionVersion <= 0)
            {
                FailFast(phaseSelection.MacroRouteId, phaseSelection.MacroRouteRef.RouteKind, phaseSelection.SelectionSignature, normalizedReason, "Target phase selection version is invalid.");
            }

            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot currentSnapshot) ||
                !currentSnapshot.IsValid ||
                !currentSnapshot.MacroRouteId.IsValid ||
                !currentSnapshot.HasPhaseDefinitionRef ||
                currentSnapshot.PhaseDefinitionRef == null ||
                currentSnapshot.MacroRouteRef == null ||
                currentSnapshot.MacroRouteId != phaseSelection.MacroRouteId ||
                !ReferenceEquals(currentSnapshot.PhaseDefinitionRef, phaseSelection.PhaseDefinitionRef))
            {
                FailFast(SceneRouteId.None, SceneRouteKind.Unspecified, ComputeSignature(SceneRouteId.None, SceneRouteKind.Unspecified, normalizedReason), normalizedReason, "Missing runtime gameplay snapshot.");
            }

            SceneRouteId macroRouteId = phaseSelection.MacroRouteId;
            SceneRouteDefinitionAsset routeAsset = phaseSelection.MacroRouteRef;
            SceneRouteKind routeKind = routeAsset.RouteKind;
            PhaseDefinitionAsset currentPhaseDefinitionRef = phaseSelection.PhaseDefinitionRef;
            PhaseDefinitionAsset previousPhaseDefinitionRef = LevelAdditiveSceneRuntimeApplier.ActiveAppliedPhaseDefinitionRef ?? currentPhaseDefinitionRef;
            string phaseSwapSignature = _levelFlowContentService.BuildPhaseSwapSignature(currentPhaseDefinitionRef, macroRouteId, normalizedReason);
            string localContentId = _levelFlowContentService.BuildPhaseSwapContentId(currentPhaseDefinitionRef);
            _levelFlowContentService.ResolvePhaseSwapOrFail(currentPhaseDefinitionRef, macroRouteId, phaseSwapSignature, normalizedReason);
            int nextSelectionVersion = phaseSelection.SelectionVersion;
            string levelSignature = phaseSwapSignature;

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelSwapLocalStart previousPhaseRef='{(previousPhaseDefinitionRef != null ? previousPhaseDefinitionRef.name : "<none>")}' currentPhaseRef='{currentPhaseDefinitionRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' contentId='{localContentId}' v='{nextSelectionVersion}' signature='{phaseSwapSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelSwapLocalMembership rail='phase-first' routeId='{macroRouteId}' validation='phase-rule+manifest-only'.",
                DebugUtility.Colors.Info);

            using IDisposable gateHandle = AcquireSwapGate();

            PhaseDefinitionSelectedEvent phaseSelectedEvent = phaseSelection;

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow][PhaseDefinition] PhaseDefinitionSelected rail='swap-local-phase' phaseId='{currentPhaseDefinitionRef.PhaseId}' phaseRef='{currentPhaseDefinitionRef.name}' macroRouteId='{macroRouteId}' contentId='{localContentId}' v='{nextSelectionVersion}' signature='{phaseSelectedEvent.SelectionSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            EventBus<PhaseDefinitionSelectedEvent>.Raise(phaseSelectedEvent);

            ct.ThrowIfCancellationRequested();
            await _worldResetCommands.ResetLevelAsync(
                new PhaseResetContext(
                    currentPhaseDefinitionRef,
                    macroRouteId,
                    new LevelContextSignature(levelSignature),
                    phaseSwapSignature),
                normalizedReason,
                ct);
            ct.ThrowIfCancellationRequested();

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(
                LevelSceneCompositionRequestFactory.CreateApplyRequest(
                    previousPhaseDefinitionRef,
                    currentPhaseDefinitionRef,
                    normalizedReason,
                    levelSignature),
                ct);

            LevelAdditiveSceneRuntimeApplier.RecordAppliedPhaseDefinition(currentPhaseDefinitionRef);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelSwapLocalApplied previousPhaseRef='{(previousPhaseDefinitionRef != null ? previousPhaseDefinitionRef.name : "<none>")}' currentPhaseRef='{currentPhaseDefinitionRef.name}' macroRouteId='{macroRouteId}' routeKind='{routeKind}' contentId='{localContentId}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} v='{nextSelectionVersion}' signature='{phaseSwapSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);

            LevelIntroStageSession swapIntroSession = new LevelIntroStageSession(
                currentPhaseDefinitionRef,
                localContentId,
                normalizedReason,
                nextSelectionVersion,
                levelSignature,
                currentPhaseDefinitionRef.Intro != null ? currentPhaseDefinitionRef.Intro.introPresenterPrefab : null,
                currentPhaseDefinitionRef.Intro != null && currentPhaseDefinitionRef.Intro.hasIntroStage
                    ? LevelIntroStageDisposition.HasIntro
                    : LevelIntroStageDisposition.NoIntro);

            DebugUtility.Log<LevelSwapLocalService>(
                $"[OBS][LevelFlow] LevelEntered source='LevelSwapLocal' rail='phase-first' phaseRef='{currentPhaseDefinitionRef.name}' contentId='{localContentId}' v='{nextSelectionVersion}' signature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            EventBus<PhaseIntroStageEntryEvent>.Raise(new PhaseIntroStageEntryEvent(
                swapIntroSession,
                "LevelSwapLocal",
                routeKind));
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
