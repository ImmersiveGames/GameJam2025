using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
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
        private readonly IGameplaySessionFlowPrepareOperationalHandoffService _handoffService;

        public GameplaySessionFlowPrepareCompletionGate(IGameplaySessionFlowPrepareOperationalHandoffService handoffService)
        {
            _handoffService = handoffService
                ?? throw new System.InvalidOperationException("[FATAL][Config][SceneFlow] GameplaySessionFlowPrepareCompletionGate requer IGameplaySessionFlowPrepareOperationalHandoffService.");
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            if (!ShouldAcceptGameplayPrepareRail(context))
            {
                return;
            }

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "SceneFlow/GameplaySessionPrepare"
                : context.Reason.Trim();
            string signature = SceneTransitionSignature.Compute(context);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] received rail='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] translated rail='GameplaySessionPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            await DispatchOperationalHandoffAsync(context, signature, reason);
        }

        private async Task DispatchOperationalHandoffAsync(SceneTransitionContext context, string signature, string reason)
        {
            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_dispatch target='GameplaySessionPrepareOperational' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            await _handoffService.ExecuteAsync(context);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_accepted target='GameplaySessionPrepareOperational' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private static bool ShouldAcceptGameplayPrepareRail(SceneTransitionContext context)
        {
            if (!context.RouteId.IsValid)
            {
                return false;
            }

            if (context.RouteRef == null)
            {
                DebugUtility.LogVerbose(typeof(GameplaySessionFlowPrepareCompletionGate),
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSkipped routeId='{context.RouteId}' reason='routeRef_missing'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            if (context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                DebugUtility.LogVerbose(typeof(GameplaySessionFlowPrepareCompletionGate),
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSkipped routeId='{context.RouteId}' routeKind='{context.RouteRef.RouteKind}' reason='non_gameplay_route'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            return true;
        }
    }
}

