using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Public navigation API for core scene dispatch.
    /// </summary>
    public interface IGameNavigationService
    {
        Task GoToMenuAsync(string reason = null);
        SceneRouteId ResolveGameplayRouteIdOrFail();
        Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload = null, string reason = null);
        Task NavigateAsync(GameNavigationIntentKind intent, string reason = null);
    }
}
