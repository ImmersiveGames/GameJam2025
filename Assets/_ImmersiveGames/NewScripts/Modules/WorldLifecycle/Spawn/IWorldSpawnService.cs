using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn
{
    /// <summary>
    /// Serviço responsável por spawn/despawn dos elementos que compõem o mundo atual.
    /// Implementações devem ser puras (sem dependência em MonoBehaviours) e idempotentes
    /// em chamadas repetidas durante o ciclo de vida da cena.
    /// </summary>
    public interface IWorldSpawnService
    {
        string Name { get; }

        Task SpawnAsync();

        Task DespawnAsync();
    }
}

