using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Application
{
    /// <summary>
    /// Helper operacional da fase 6 para localizar executores locais neutros do reset sem depender do nome do modulo concreto.
    /// </summary>
    internal static class WorldResetLocalExecutorLocator
    {
        public static IReadOnlyList<IWorldResetLocalExecutor> FindExecutorsForScene(string sceneName)
        {
            string target = (sceneName ?? string.Empty).Trim();
            IWorldResetLocalExecutor[] all = FindAllExecutors(includeInactive: true);

            if (all.Length == 0)
            {
                return Array.Empty<IWorldResetLocalExecutor>();
            }

            if (target.Length == 0)
            {
                return all;
            }

            List<IWorldResetLocalExecutor> result = null;
            foreach (IWorldResetLocalExecutor executor in all)
            {
                if (executor is not Component component)
                {
                    continue;
                }

                var scene = component.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                if (!string.Equals(scene.name, target, StringComparison.Ordinal))
                {
                    continue;
                }

                result ??= new List<IWorldResetLocalExecutor>(1);
                result.Add(executor);
            }

            return result ?? (IReadOnlyList<IWorldResetLocalExecutor>)Array.Empty<IWorldResetLocalExecutor>();
        }

        private static IWorldResetLocalExecutor[] FindAllExecutors(bool includeInactive)
        {
            var inactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            MonoBehaviour[] all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(inactive, FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                return Array.Empty<IWorldResetLocalExecutor>();
            }

            var valid = new List<IWorldResetLocalExecutor>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour behaviour = all[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is not IWorldResetLocalExecutor executor || executor == null)
                {
                    continue;
                }

                var scene = behaviour.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                valid.Add(executor);
            }

            return valid.Count == 0 ? Array.Empty<IWorldResetLocalExecutor>() : valid.ToArray();
        }
    }
}

