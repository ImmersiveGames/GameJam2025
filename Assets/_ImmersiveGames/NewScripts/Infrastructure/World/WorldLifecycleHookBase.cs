using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    public abstract class WorldLifecycleHookBase : IWorldLifecycleHook
    {
        public virtual Task OnBeforeDespawnAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAfterDespawnAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnBeforeSpawnAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAfterSpawnAsync()
        {
            return Task.CompletedTask;
        }
    }
}
