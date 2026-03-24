using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneReset.Bindings;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Application
{
    /// <summary>
    /// Executa o trilho local de reset nos SceneResetControllers resolvidos pelo pipeline macro.
    /// Não valida pós-condições nem publica lifecycle.
    /// </summary>
    public sealed class WorldResetExecutor
    {
        public async Task ExecuteAsync(
            IReadOnlyList<SceneResetController> controllers,
            string reason)
        {
            await ExecuteResetOnControllersAsync(controllers, reason);
        }

        private static async Task ExecuteResetOnControllersAsync(
            IReadOnlyList<SceneResetController> controllers,
            string reason)
        {
            if (controllers == null || controllers.Count == 0)
            {
                return;
            }

            var filtered = new List<SceneResetController>(controllers.Count);
            for (int i = 0; i < controllers.Count; i++)
            {
                SceneResetController controller = controllers[i];
                if (controller != null)
                {
                    filtered.Add(controller);
                }
            }

            filtered.Sort(static (a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

            var tasks = new List<Task>(filtered.Count);
            for (int i = 0; i < filtered.Count; i++)
            {
                tasks.Add(filtered[i].ResetWorldAsync(reason));
            }

            await Task.WhenAll(tasks);
        }
    }
}
