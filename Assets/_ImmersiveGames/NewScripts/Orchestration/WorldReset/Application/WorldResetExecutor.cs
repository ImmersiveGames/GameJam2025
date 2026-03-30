using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Application
{
    /// <summary>
    /// Executa o trilho local de reset em boundary neutro resolvido pelo pipeline macro.
    /// Não valida pós-condições nem publica lifecycle.
    /// </summary>
    public sealed class WorldResetExecutor
    {
        public async Task ExecuteAsync(
            IReadOnlyList<IWorldResetLocalExecutor> executors,
            string reason)
        {
            await ExecuteResetOnControllersAsync(executors, reason);
        }

        private static async Task ExecuteResetOnControllersAsync(
            IReadOnlyList<IWorldResetLocalExecutor> executors,
            string reason)
        {
            if (executors == null || executors.Count == 0)
            {
                return;
            }

            var filtered = new List<IWorldResetLocalExecutor>(executors.Count);
            for (int i = 0; i < executors.Count; i++)
            {
                IWorldResetLocalExecutor executor = executors[i];
                if (executor != null)
                {
                    filtered.Add(executor);
                }
            }

            filtered.Sort(static (a, b) => CompareExecutors(a, b));

            var tasks = new List<Task>(filtered.Count);
            for (int i = 0; i < filtered.Count; i++)
            {
                tasks.Add(filtered[i].ResetWorldAsync(reason));
            }

            await Task.WhenAll(tasks);
        }

        private static int CompareExecutors(IWorldResetLocalExecutor left, IWorldResetLocalExecutor right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is not UnityEngine.Object leftObject)
            {
                return 1;
            }

            if (right is not UnityEngine.Object rightObject)
            {
                return -1;
            }

            return leftObject.GetInstanceID().CompareTo(rightObject.GetInstanceID());
        }
    }
}
