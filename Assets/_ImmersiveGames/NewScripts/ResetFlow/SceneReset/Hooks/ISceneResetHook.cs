using System.Threading.Tasks;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneReset.Hooks
{
    /// <summary>
    /// Hooks opcionais para observar o ciclo de vida do reset do mundo.
    /// Implementação é opt-in e não altera o contrato IWorldSpawnService.
    /// </summary>
    public interface ISceneResetHook
    {
        Task OnBeforeDespawnAsync();

        Task OnAfterDespawnAsync();

        Task OnBeforeSpawnAsync();

        Task OnAfterSpawnAsync();
    }
}


