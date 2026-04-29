using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
namespace _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Policy canônica do SceneFlow.
    /// </summary>
    public sealed class SceneFlowNavigationPolicy : INavigationPolicy
    {
        public bool CanTransition(SceneTransitionRequest request, out string denialReason)
        {
            if (request == null)
            {
                denialReason = "request_null";
                return false;
            }

            if (!request.RouteId.IsValid)
            {
                denialReason = "route_id_invalid";
                return false;
            }

            if (!request.ResolvedRouteDefinition.HasValue)
            {
                denialReason = "route_definition_missing";
                return false;
            }

            SceneRouteDefinition routeDefinition = request.ResolvedRouteDefinition.Value;
            if (routeDefinition.RouteKind == SceneRouteKind.Unspecified)
            {
                denialReason = "route_kind_unspecified";
                return false;
            }

            if (!routeDefinition.HasSceneData)
            {
                denialReason = "route_scene_data_missing";
                return false;
            }

            if (request.UseFade && request.TransitionProfile == null)
            {
                denialReason = "transition_profile_missing_for_fade";
                return false;
            }

            denialReason = string.Empty;
            return true;
        }
    }
}

