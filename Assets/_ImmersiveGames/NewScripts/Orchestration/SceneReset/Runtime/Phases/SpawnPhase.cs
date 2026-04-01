using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime.Phases
{
    internal sealed class SpawnPhase : ISceneResetPhase
    {
        private static readonly SceneResetSpawnOwnerExecutor Executor = new();

        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            await hookRunner.RunWorldHooksAsync(context, "OnBeforeSpawn", hook => hook.OnBeforeSpawnAsync());
            DebugUtility.Log(typeof(SceneResetFacade),
                "Spawn phase delegating to Spawn owner services.");
            await Executor.ExecuteAsync(context, "Spawn", service => service.SpawnAsync());
            context.LogActorRegistryCount("After Spawn");
        }
    }
}
