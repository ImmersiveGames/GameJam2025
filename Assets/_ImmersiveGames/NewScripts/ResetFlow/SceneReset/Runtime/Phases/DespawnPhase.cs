using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Runtime.Phases
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

