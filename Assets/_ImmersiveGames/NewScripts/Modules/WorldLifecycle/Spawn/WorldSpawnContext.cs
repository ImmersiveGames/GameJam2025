using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn
{
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
