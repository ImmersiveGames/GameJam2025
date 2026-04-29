using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    public interface IWorldSpawnServiceRegistryReadPortProvider
    {
        bool TryGetForScene(string sceneName, out IWorldSpawnServiceRegistryReadPort readPort);

        bool TryGetCurrentReadPort(out IWorldSpawnServiceRegistryReadPort readPort);
    }

    /// <summary>
    /// Provider técnico que resolve o read-port scene-local apenas quando o execute path pede.
    /// </summary>
    public sealed class WorldSpawnServiceRegistryReadPortProvider : IWorldSpawnServiceRegistryReadPortProvider
    {
        private readonly IDependencyProvider _dependencyProvider;

        public WorldSpawnServiceRegistryReadPortProvider(IDependencyProvider dependencyProvider)
        {
            _dependencyProvider = dependencyProvider ?? throw new ArgumentNullException(nameof(dependencyProvider));
        }

        public bool TryGetCurrentReadPort(out IWorldSpawnServiceRegistryReadPort readPort)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return TryGetForScene(sceneName, out readPort);
        }

        public bool TryGetForScene(string sceneName, out IWorldSpawnServiceRegistryReadPort readPort)
        {
            readPort = null;
            if (_dependencyProvider == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            return _dependencyProvider.TryGetForScene(sceneName.Trim(), out readPort) && readPort != null;
        }
    }
}
