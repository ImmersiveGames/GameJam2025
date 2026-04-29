using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
namespace _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Guard canônico do SceneFlow por rota.
    /// </summary>
    public sealed class SceneFlowRouteGuard : IRouteGuard
    {
        public bool CanTransitionRoute(
            SceneTransitionRequest request,
            SceneRouteDefinition routeDefinition,
            out string denialReason)
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

            if (routeDefinition.RouteKind == SceneRouteKind.Unspecified)
            {
                denialReason = "route_kind_unspecified";
                return false;
            }

            if (string.IsNullOrWhiteSpace(routeDefinition.TargetActiveScene))
            {
                denialReason = "target_active_scene_missing";
                return false;
            }

            if (!routeDefinition.HasSceneData)
            {
                denialReason = "route_scene_data_missing";
                return false;
            }

            denialReason = string.Empty;
            return true;
        }
    }
}

