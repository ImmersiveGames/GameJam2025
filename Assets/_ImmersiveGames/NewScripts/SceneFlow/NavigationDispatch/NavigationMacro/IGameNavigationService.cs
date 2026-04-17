using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Runtime;
namespace ImmersiveGames.GameJam2025.Orchestration.Navigation
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

