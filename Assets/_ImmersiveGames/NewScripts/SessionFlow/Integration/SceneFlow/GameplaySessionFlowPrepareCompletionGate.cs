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
            if (!ShouldAcceptGameplayPrepareRail(context, out SceneRouteProfile profile))
            {
                return;
            }

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "SceneFlow/GameplaySessionPrepare"
                : context.Reason.Trim();
            string signature = SceneTransitionSignature.Compute(context);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] received rail='GameplaySessionPrepare' routeId='{context.RouteId}' routeKind='{context.RouteKind}' routeProfileId='{profile.ProfileId}' gameplayParticipation='{profile.GameplayParticipation}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] translated rail='GameplaySessionPrepare' routeId='{context.RouteId}' routeProfileId='{profile.ProfileId}' gameplayParticipation='{profile.GameplayParticipation}' decisionSource='routeProfile.gameplayParticipation' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            await DispatchOperationalHandoffAsync(context, signature, reason);
        }

        private async Task DispatchOperationalHandoffAsync(SceneTransitionContext context, string signature, string reason)
        {
            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_dispatch target='GameplaySessionPrepareOperational' routeId='{context.RouteId}' routeKind='{context.RouteKind}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            await _handoffService.ExecuteAsync(context);

            DebugUtility.Log<GameplaySessionFlowPrepareCompletionGate>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_accepted target='GameplaySessionPrepareOperational' routeId='{context.RouteId}' routeKind='{context.RouteKind}' gameplayEntryKind='{context.GameplayEntryKind}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private static bool ShouldAcceptGameplayPrepareRail(SceneTransitionContext context, out SceneRouteProfile profile)
        {
            profile = default;

            if (!context.RouteId.IsValid)
            {
                return false;
            }

            if (context.RouteRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareCompletionGate),
                    $"[FATAL][Config][GameplaySessionFlow] GameplaySessionPrepare requires routeRef. routeId='{context.RouteId}' routeKind='{context.RouteKind}'.");
            }

            if (context.RouteRef.RouteProfile == null)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowPrepareCompletionGate),
                    $"[FATAL][Config][GameplaySessionFlow] GameplaySessionPrepare requires route profile. routeId='{context.RouteId}' routeKind='{context.RouteKind}'.");
            }

            context.RouteRef.RouteProfile.ValidateProfileOrFailFast();
            profile = context.RouteRef.RouteProfile.ToProfile();

            if (profile.GameplayParticipation != SceneRouteProfileGameplayParticipation.Gameplay)
            {
                DebugUtility.LogVerbose(typeof(GameplaySessionFlowPrepareCompletionGate),
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSkipped routeId='{context.RouteId}' routeKind='{context.RouteKind}' routeProfileId='{profile.ProfileId}' gameplayParticipation='{profile.GameplayParticipation}' decisionSource='routeProfile.gameplayParticipation' reason='non_gameplay_profile'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            if (!context.IsGameplayInitialEntry)
            {
                DebugUtility.LogVerbose(typeof(GameplaySessionFlowPrepareCompletionGate),
                    $"[OBS][GameplaySessionFlow][Operational] GameplaySessionPrepareSkipped routeId='{context.RouteId}' routeKind='{context.RouteKind}' routeProfileId='{profile.ProfileId}' gameplayParticipation='{profile.GameplayParticipation}' decisionSource='routeProfile.gameplayParticipation' gameplayEntryKind='{context.GameplayEntryKind}' reason='reentry_deferred_to_postcontinuation_rail'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            return true;
        }
    }
}

