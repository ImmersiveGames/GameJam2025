using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
namespace _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro
{
    /// <summary>
    /// Public navigation API for core scene dispatch.
    /// </summary>
    public interface IGameNavigationService
    {
        Task GoToMenuAsync(string reason = null);
        SceneRouteId ResolveGameplayRouteIdOrFail();
        Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload, string reason = null);
        Task NavigateAsync(GameNavigationIntentKind intent, string reason = null);
        Task NavigateToRoute(SceneRouteDefinitionAsset routeDefinition, string reason = null);
        Task NavigateToResolvedRoute(_ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline.SessionOperationalResolvedRoute resolvedRoute, string reason = null);
    }
}

