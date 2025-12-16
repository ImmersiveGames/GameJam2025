using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    public interface IWorldLifecycleHook
    {
        Task OnBeforeDespawnAsync();

        Task OnAfterDespawnAsync();

        Task OnBeforeSpawnAsync();

        Task OnAfterSpawnAsync();
    }
}
