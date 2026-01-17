using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Hooks.QA
{
    /// <summary>
    /// Hook de lifecycle de cena para QA/dev que loga execução com menor prioridade.
    ///
    /// IMPORTANTE: no WorldLifecycleOrchestrator, a ordenação de hooks é ASCENDENTE (Order menor executa primeiro).
    /// Portanto, para este logger rodar por último ("menor prioridade"), usamos um Order alto.
    /// </summary>
    public sealed class SceneLifecycleHookLoggerA : WorldLifecycleHookBase
    {
        [SerializeField]
        private string label = "SceneLifecycleHookLoggerA";

        // Menor prioridade (executa por último): Order alto.
        public override int Order => 10_000;

        public override Task OnBeforeDespawnAsync()
        {
            DebugUtility.Log(typeof(SceneLifecycleHookLoggerA),
                $"[QA] {label} -> OnBeforeDespawnAsync");
            return Task.CompletedTask;
        }

        public override Task OnAfterDespawnAsync()
        {
            DebugUtility.LogVerbose(typeof(SceneLifecycleHookLoggerA),
                $"[QA] {label} -> OnAfterDespawnAsync");
            return Task.CompletedTask;
        }

        public override Task OnBeforeSpawnAsync()
        {
            DebugUtility.LogVerbose(typeof(SceneLifecycleHookLoggerA),
                $"[QA] {label} -> OnBeforeSpawnAsync");
            return Task.CompletedTask;
        }

        public override Task OnAfterSpawnAsync()
        {
            DebugUtility.Log(typeof(SceneLifecycleHookLoggerA),
                $"[QA] {label} -> OnAfterSpawnAsync");
            return Task.CompletedTask;
        }
    }
}
