using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    public interface IGameplaySessionFlowPrepareOperationalHandoffService
    {
        Task ExecuteAsync(SceneTransitionContext context);
    }

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

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "SceneFlow/GameplaySessionPrepare"
                : context.Reason.Trim();
            string signature = SceneTransitionSignature.Compute(context);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] received rail='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            IGameplaySessionFlowPrepareOperationalHandoffService handoffService = ResolveRequiredHandoffService(context);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] translated rail='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_dispatch target='GameplaySessionPrepareOperational' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            await handoffService.ExecuteAsync(context);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_accepted target='GameplaySessionPrepareOperational' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private static IGameplaySessionFlowPrepareOperationalHandoffService ResolveRequiredHandoffService(SceneTransitionContext context)
        {
            if (DependencyManager.Provider == null)
            {
                FailFastConfig(context, "DependencyManager.Provider unavailable.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplaySessionFlowPrepareOperationalHandoffService>(out var handoffService) || handoffService == null)
            {
                FailFastConfig(context, "IGameplaySessionFlowPrepareOperationalHandoffService missing.");
            }

            return handoffService;
        }

        private static void FailFastConfig(SceneTransitionContext context, string detail)
        {
            HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareCompletionGate),
                $"[FATAL][H1][SceneFlow] GameplaySessionFlow completion gate misconfigured: {detail} routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
        }
    }
}

