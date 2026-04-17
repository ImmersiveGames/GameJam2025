using System.Threading;
using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Core.Logging;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneReset.Runtime.Phases
{
    internal sealed class DespawnPhase : ISceneResetPhase
    {
        private static readonly SceneResetSpawnOwnerExecutor Executor = new();

        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            DebugUtility.Log(typeof(SceneResetPipeline),
                "Despawn phase delegating to Spawn owner services.");
            await Executor.ExecuteAsync(context, "Despawn", service => service.DespawnAsync());
            context.LogActorRegistryCount("After Despawn");
        }
    }
}

