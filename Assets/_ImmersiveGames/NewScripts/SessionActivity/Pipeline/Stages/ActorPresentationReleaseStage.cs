using System;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    internal static class ActorPresentationReleaseStage
    {
        public static bool ShouldReleaseForRail(ActorPresentationReleasePolicy policy, SessionActivityPipeline.ActorPresentationReleaseRail rail)
        {
            return rail switch
            {
                SessionActivityPipeline.ActorPresentationReleaseRail.BeforeRematerialization => true,
                SessionActivityPipeline.ActorPresentationReleaseRail.ActivityExit => policy == ActorPresentationReleasePolicy.ReleaseOnActivityExit,
                SessionActivityPipeline.ActorPresentationReleaseRail.RouteExit => policy == ActorPresentationReleasePolicy.ReleaseOnRouteExit || policy == ActorPresentationReleasePolicy.ReleaseOnActivityExit,
                _ => false
            };
        }

        public static bool IsNonPlayerScopePolicyCompatible(NonPlayerActorScope scope, ActorPresentationReleasePolicy policy)
        {
            return !((scope == NonPlayerActorScope.RouteScoped && policy == ActorPresentationReleasePolicy.ReleaseOnActivityExit) ||
                     (scope == NonPlayerActorScope.ActivityScoped && policy == ActorPresentationReleasePolicy.ReleaseOnRouteExit));
        }

        public static ActorPresentationResult Release(
            IActorPresentationMaterializationAdapter adapter,
            ActorPresentationRuntimeHandle handle,
            string source,
            string reason)
        {
            ActorPresentationReleaseCommand command = new(handle, source, reason);
            return adapter.Release(command);
        }

        public static bool IsKeptBound(ActorPresentationResult releaseResult)
        {
            return string.Equals(releaseResult.ReleasedFact.Reason, UnityActorPresentationMaterializationAdapter.ReasonReleaseKeptBound, StringComparison.Ordinal);
        }
    }
}
