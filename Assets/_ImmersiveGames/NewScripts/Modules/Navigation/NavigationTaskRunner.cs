using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Utilitário pequeno para evitar Task "fire-and-forget" silenciosa.
    /// </summary>
    public static class NavigationTaskRunner
    {
        public static void FireAndForget(Task task, Type contextType, string contextLabel)
        {
            if (task == null)
            {
                return;
            }

            // Se já completou com falha, loga imediatamente.
            if (task.IsFaulted)
            {
                DebugUtility.LogError(contextType,
                    $"[Navigation] Falha em task (imediata). context='{contextLabel}', ex={task.Exception}");
                return;
            }

            // Caso contrário, aguarda em background e loga exceção.
            _ = Observe(task, contextType, contextLabel);
        }

        private static async Task Observe(Task task, Type contextType, string contextLabel)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(contextType,
                    $"[Navigation] Falha em task. context='{contextLabel}', ex={ex}");
            }
        }
    }
}
