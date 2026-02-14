using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Infrastructure.Actors.Bindings.Hooks
{
    /// <summary>
    /// Base helper para hooks em MonoBehaviour (no-op).
    /// </summary>
    public abstract class ActorLifecycleHookBase : MonoBehaviour, IActorLifecycleHook
    {
        public virtual Task OnBeforeActorDespawnAsync() => Task.CompletedTask;

        public virtual Task OnAfterActorDespawnAsync() => Task.CompletedTask;

        public virtual Task OnBeforeActorSpawnAsync() => Task.CompletedTask;

        public virtual Task OnAfterActorSpawnAsync() => Task.CompletedTask;
    }
}
