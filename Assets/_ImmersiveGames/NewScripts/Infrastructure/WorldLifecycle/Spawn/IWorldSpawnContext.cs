using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn
{
    /// <summary>
    /// Contexto de spawn para agrupar o WorldRoot e o nome da cena corrente.
    /// </summary>
    public interface IWorldSpawnContext
    {
        Transform WorldRoot { get; }

        string SceneName { get; }
    }

    /// <summary>
    /// Implementação concreta do contexto de spawn para uma cena.
    /// </summary>
    public sealed class WorldSpawnContext : IWorldSpawnContext
    {
        public WorldSpawnContext(string sceneName, Transform worldRoot)
        {
            SceneName = sceneName;
            WorldRoot = worldRoot;

            if (string.IsNullOrWhiteSpace(SceneName))
            {
                DebugUtility.LogError(typeof(WorldSpawnContext),
                    "SceneName inválido ao construir WorldSpawnContext.");
            }

            if (WorldRoot == null)
            {
                DebugUtility.LogError(typeof(WorldSpawnContext),
                    "WorldRoot nulo ao construir WorldSpawnContext.");
            }
        }

        public Transform WorldRoot { get; }

        public string SceneName { get; }
    }
}
