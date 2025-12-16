using System.Threading.Tasks;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Actors
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
