using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;

namespace _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore
{
    /// <summary>
    /// Read-port canonico do actor set resolvido pelo contexto macro de route.
    /// </summary>
    public interface ISceneFlowRouteActorSetRefContext
    {
        bool TryGetCurrent(out ActorSetRef actorSetRef, out string routeIdentity, out string source);
    }
}
