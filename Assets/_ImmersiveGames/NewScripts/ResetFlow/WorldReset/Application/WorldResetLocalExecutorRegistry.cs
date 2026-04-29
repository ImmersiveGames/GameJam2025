using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Application
{
    public sealed class WorldResetLocalExecutorRegistry : IWorldResetLocalExecutorRegistry
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, List<IWorldResetLocalExecutor>> _executorsByScene = new(StringComparer.Ordinal);

        public void Register(string sceneName, IWorldResetLocalExecutor executor)
        {
            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            string scene = Normalize(sceneName);
            lock (_lock)
            {
                if (!_executorsByScene.TryGetValue(scene, out List<IWorldResetLocalExecutor> executors))
                {
                    executors = new List<IWorldResetLocalExecutor>(1);
                    _executorsByScene.Add(scene, executors);
                }

                if (!executors.Contains(executor))
                {
                    executors.Add(executor);
                }
            }
        }

        public void Unregister(string sceneName, IWorldResetLocalExecutor executor)
        {
            if (executor == null)
            {
                return;
            }

            string scene = Normalize(sceneName);
            lock (_lock)
            {
                if (!_executorsByScene.TryGetValue(scene, out List<IWorldResetLocalExecutor> executors))
                {
                    return;
                }

                executors.Remove(executor);
                if (executors.Count == 0)
                {
                    _executorsByScene.Remove(scene);
                }
            }
        }

        public IReadOnlyList<IWorldResetLocalExecutor> GetExecutorsForScene(string sceneName)
        {
            string scene = Normalize(sceneName);

            lock (_lock)
            {
                if (_executorsByScene.Count == 0)
                {
                    return Array.Empty<IWorldResetLocalExecutor>();
                }

                if (_executorsByScene.TryGetValue(scene, out List<IWorldResetLocalExecutor> executors))
                {
                    return executors.Count == 0
                        ? Array.Empty<IWorldResetLocalExecutor>()
                        : executors.ToArray();
                }

                return Array.Empty<IWorldResetLocalExecutor>();
            }
        }

        private static string Normalize(string sceneName)
        {
            return string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
        }
    }
}

