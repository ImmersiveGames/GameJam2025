using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
namespace _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Regra de produto por rota, separada da policy geral de navegação.
    /// </summary>
    public interface IRouteGuard
    {
        bool CanTransitionRoute(
            SceneTransitionRequest request,
            SceneRouteDefinition routeDefinition,
            out string denialReason);
    }
}

