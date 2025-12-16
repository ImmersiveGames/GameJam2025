using System.Threading.Tasks;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Serviço de spawn placeholder para validar o pipeline sem instanciar conteúdo real.
    /// </summary>
    public sealed class DummySpawnService : IWorldSpawnService
    {
        public string Name => "DummySpawnService";

        public Task SpawnAsync()
        {
            DebugUtility.Log(typeof(DummySpawnService), "Dummy Spawn executed (no content created).");
            return Task.CompletedTask;
        }

        public Task DespawnAsync()
        {
            DebugUtility.Log(typeof(DummySpawnService), "Dummy Despawn executed (no content to remove).");
            return Task.CompletedTask;
        }
    }
}
