using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Runtime;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Runtime
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

