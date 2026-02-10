using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Implementação permissiva default de IRouteGuard.
    /// </summary>
    public sealed class AllowAllRouteGuard : IRouteGuard
    {
        public bool CanTransitionRoute(
            SceneTransitionRequest request,
            SceneRouteDefinition routeDefinition,
            out string denialReason)
        {
            denialReason = string.Empty;
            return true;
        }
    }
}
