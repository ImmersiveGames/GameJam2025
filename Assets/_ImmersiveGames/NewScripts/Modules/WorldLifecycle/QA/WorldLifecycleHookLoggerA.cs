using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.QA
{
    /// <summary>
    /// Hook de lifecycle de cena para QA/dev que loga execução com menor prioridade.
    ///
    /// IMPORTANTE: no WorldLifecycleOrchestrator, a ordenação de hooks é ASCENDENTE (Order menor executa primeiro).
    /// Portanto, para este logger rodar por último ("menor prioridade"), usamos um Order alto.
    /// </summary>
    public sealed class WorldLifecycleHookLoggerA : WorldLifecycleHookBase
    {
        [SerializeField]
        private string label = "WorldLifecycleHookLoggerA";

        // Menor prioridade (executa por último): Order alto.
        public override int Order => 10_000;

        public override Task OnBeforeDespawnAsync()
        {
            DebugUtility.Log(typeof(WorldLifecycleHookLoggerA),
                $"[QA] {label} -> OnBeforeDespawnAsync");
            return Task.CompletedTask;
        }

        public override Task OnAfterDespawnAsync()
        {
            DebugUtility.LogVerbose(typeof(WorldLifecycleHookLoggerA),
                $"[QA] {label} -> OnAfterDespawnAsync");
            return Task.CompletedTask;
        }

        public override Task OnBeforeSpawnAsync()
        {
            DebugUtility.LogVerbose(typeof(WorldLifecycleHookLoggerA),
                $"[QA] {label} -> OnBeforeSpawnAsync");
            return Task.CompletedTask;
        }

        public override Task OnAfterSpawnAsync()
        {
            DebugUtility.Log(typeof(WorldLifecycleHookLoggerA),
                $"[QA] {label} -> OnAfterSpawnAsync");
            return Task.CompletedTask;
        }
    }
}

