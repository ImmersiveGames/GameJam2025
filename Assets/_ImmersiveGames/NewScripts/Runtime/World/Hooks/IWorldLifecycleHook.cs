using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Runtime.World.Hooks
{
    /// <summary>
    /// Hooks opcionais para observar o ciclo de vida do reset do mundo.
    /// Implementação é opt-in e não altera o contrato IWorldSpawnService.
    /// </summary>
    public interface IWorldLifecycleHook
    {
        Task OnBeforeDespawnAsync();

        Task OnAfterDespawnAsync();

        Task OnBeforeSpawnAsync();

        Task OnAfterSpawnAsync();
    }
}

