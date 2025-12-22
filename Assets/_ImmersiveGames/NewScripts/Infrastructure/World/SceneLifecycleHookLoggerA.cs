using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Debug;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Hook de lifecycle de cena para QA/dev que loga execução com menor prioridade.
    /// </summary>
    public sealed class SceneLifecycleHookLoggerA : MonoBehaviour, IWorldLifecycleHook, IOrderedLifecycleHook
    {
        [SerializeField]
        private string label = "SceneLifecycleHookLoggerA";

        public int Order => 0;

        public Task OnBeforeDespawnAsync()
        {
            DebugUtility.Log(typeof(SceneLifecycleHookLoggerA),
                $"[QA] {label} -> OnBeforeDespawnAsync");
            return Task.CompletedTask;
        }

        public Task OnAfterDespawnAsync()
        {
            DebugUtility.LogVerbose(typeof(SceneLifecycleHookLoggerA),
                $"[QA] {label} -> OnAfterDespawnAsync");
            return Task.CompletedTask;
        }

        public Task OnBeforeSpawnAsync()
        {
            DebugUtility.LogVerbose(typeof(SceneLifecycleHookLoggerA),
                $"[QA] {label} -> OnBeforeSpawnAsync");
            return Task.CompletedTask;
        }

        public Task OnAfterSpawnAsync()
        {
            DebugUtility.Log(typeof(SceneLifecycleHookLoggerA),
                $"[QA] {label} -> OnAfterSpawnAsync");
            return Task.CompletedTask;
        }
    }
}
