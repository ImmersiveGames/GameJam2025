using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionFlowPrepareCompletionGate : ISceneTransitionCompletionGate
    {
        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            if (!context.RouteId.IsValid)
            {
                return;
            }

            if (context.RouteRef == null)
            {
                DebugUtility.LogVerbose(typeof(GameplaySessionFlowPrepareCompletionGate),
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSkipped routeId='{context.RouteId}' reason='routeRef_missing'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                DebugUtility.LogVerbose(typeof(GameplaySessionFlowPrepareCompletionGate),
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSkipped routeId='{context.RouteId}' routeKind='{context.RouteRef.RouteKind}' reason='non_gameplay_route'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (DependencyManager.Provider == null)
            {
                FailFastConfig(context, "DependencyManager.Provider unavailable.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionSelectionService>(out var phaseSelectionService) || phaseSelectionService == null)
            {
                FailFastConfig(context, "IPhaseDefinitionSelectionService missing.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<GameplayPhaseFlowService>(out var gameplayPhaseFlowService) || gameplayPhaseFlowService == null)
            {
                FailFastConfig(context, "GameplayPhaseFlowService missing.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneCompositionExecutor>(out var sceneCompositionExecutor) || sceneCompositionExecutor == null)
            {
                FailFastConfig(context, "ISceneCompositionExecutor missing.");
            }

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "SceneFlow/GameplaySessionPrepare"
                : context.Reason.Trim();
            string signature = SceneTransitionSignature.Compute(context);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Operational] MacroLoadingPhase='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
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

            PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                selectedPhaseDefinitionRef,
                phaseCompositionRequest.ScenesToLoad,
                phaseCompositionRequest.ActiveScene,
                "GameplaySessionFlow");

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareCompleted phaseId='{selectedPhaseDefinitionRef.PhaseId}' phaseRef='{selectedPhaseDefinitionRef.name}' routeId='{context.RouteId}' signature='{signature}' scenesAdded={compositionResult.ScenesAdded} scenesRemoved={compositionResult.ScenesRemoved} reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private static void FailFastConfig(SceneTransitionContext context, string detail)
        {
            HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareCompletionGate),
                $"[FATAL][H1][SceneFlow] GameplaySessionFlow completion gate misconfigured: {detail} routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
        }
    }
}

