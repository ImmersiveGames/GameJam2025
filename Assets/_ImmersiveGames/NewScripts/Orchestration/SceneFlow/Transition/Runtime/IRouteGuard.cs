using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime
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
