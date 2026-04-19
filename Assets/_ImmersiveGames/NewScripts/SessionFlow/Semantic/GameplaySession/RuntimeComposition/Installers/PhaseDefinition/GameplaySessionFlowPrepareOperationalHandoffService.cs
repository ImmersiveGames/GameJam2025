using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionFlowPrepareOperationalHandoffService : IGameplaySessionFlowPrepareOperationalHandoffService
    {
        public async System.Threading.Tasks.Task ExecuteAsync(SceneTransitionContext context)
        {
            if (!context.RouteId.IsValid || context.RouteRef == null || context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionSelectionService>(out var phaseSelectionService) || phaseSelectionService == null)
            {
                FailFastOperational(context, "IPhaseDefinitionSelectionService missing.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<GameplayPhaseFlowService>(out var gameplayPhaseFlowService) || gameplayPhaseFlowService == null)
            {
                FailFastOperational(context, "GameplayPhaseFlowService missing.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneCompositionExecutor>(out var sceneCompositionExecutor) || sceneCompositionExecutor == null)
            {
                FailFastOperational(context, "ISceneCompositionExecutor missing.");
            }

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "SceneFlow/GameplaySessionPrepare"
                : context.Reason.Trim();
            string signature = SceneTransitionSignature.Compute(context);

            DebugUtility.Log<GameplaySessionFlowPrepareOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_accepted rail='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            PhaseDefinitionAsset selectedPhaseDefinitionRef = phaseSelectionService.ResolveOrFail();
            PhaseDefinitionSelectedEvent phaseSelectedEvent = gameplayPhaseFlowService.PublishPhaseDefinitionSelected(
                selectedPhaseDefinitionRef,
                context.RouteId,
                context.RouteRef,
                reason);

            SceneCompositionRequest phaseCompositionRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                selectedPhaseDefinitionRef,
                reason,
                phaseSelectedEvent.SelectionSignature,
                forceFullReload: false);

            SceneCompositionResult compositionResult = await sceneCompositionExecutor.ApplyAsync(phaseCompositionRequest);

            const string canonicalPhaseContentAppliedSource = "GameplaySessionFlow";

            DebugUtility.Log<GameplaySessionFlowPrepareOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] handoff_dispatch target='GameplayPhaseFlowService/IntroStageRail' source='{canonicalPhaseContentAppliedSource}' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                selectedPhaseDefinitionRef,
                phaseCompositionRequest.ScenesToLoad,
                phaseCompositionRequest.ActiveScene,
                canonicalPhaseContentAppliedSource);

            DebugUtility.Log<GameplaySessionFlowPrepareOperationalHandoffService>(
                $"[OBS][GameplaySessionFlow][Operational] execution_completed rail='GameplaySessionPrepare' phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' routeId='{context.RouteId}' signature='{signature}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private static void FailFastOperational(SceneTransitionContext context, string detail)
        {
            HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareOperationalHandoffService),
                $"[FATAL][H1][SceneFlow] GameplaySessionFlow prepare operational handoff misconfigured: {detail} routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
        }
    }
}
