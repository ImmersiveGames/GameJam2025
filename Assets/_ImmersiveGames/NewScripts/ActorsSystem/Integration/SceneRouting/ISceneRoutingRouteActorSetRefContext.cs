using _ImmersiveGames.NewScripts.ActorsSystem.Models;
namespace _ImmersiveGames.NewScripts.ActorsSystem.Integration.SceneRouting
{
    /// <summary>
    /// Read-port canonico do actor set resolvido pelo contexto macro de route.
    /// </summary>
    public interface ISceneRoutingRouteActorSetRefContext
    {
        bool TryGetCurrent(out ActorSetRef actorSetRef, out string routeIdentity, out string source);
    }
}
