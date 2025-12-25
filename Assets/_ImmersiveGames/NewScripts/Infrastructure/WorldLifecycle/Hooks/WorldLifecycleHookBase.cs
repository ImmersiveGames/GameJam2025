using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Hooks
{
    /// <summary>
    /// Implementação base com no-op para facilitar adoção incremental.
    /// </summary>
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
