using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Hooks;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Reset.QA
{
    /// <summary>
    /// Hook de lifecycle de cena para QA/dev com prioridade maior para validar ordenação determinística.
    /// </summary>
    public sealed class SceneLifecycleHookLoggerB : MonoBehaviour, IWorldLifecycleHook, IOrderedLifecycleHook
    {
        [SerializeField]
        private string label = "SceneLifecycleHookLoggerB";

        public int Order => 10;

        public Task OnBeforeDespawnAsync()
        {
            DebugUtility.Log(typeof(SceneLifecycleHookLoggerB),
                $"[QA] {label} -> OnBeforeDespawnAsync");
            return Task.CompletedTask;
        }

        public Task OnAfterDespawnAsync()
        {
            DebugUtility.LogVerbose(typeof(SceneLifecycleHookLoggerB),
                $"[QA] {label} -> OnAfterDespawnAsync");
            return Task.CompletedTask;
        }

        public Task OnBeforeSpawnAsync()
        {
            DebugUtility.LogVerbose(typeof(SceneLifecycleHookLoggerB),
                $"[QA] {label} -> OnBeforeSpawnAsync");
            return Task.CompletedTask;
        }

        public Task OnAfterSpawnAsync()
        {
            DebugUtility.Log(typeof(SceneLifecycleHookLoggerB),
                $"[QA] {label} -> OnAfterSpawnAsync");
            return Task.CompletedTask;
        }
    }
}
