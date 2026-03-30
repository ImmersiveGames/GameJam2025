using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime.Phases
{
    internal sealed class SpawnPhase : ISceneResetPhase
    {
        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            await hookRunner.RunWorldHooksAsync(context, "OnBeforeSpawn", hook => hook.OnBeforeSpawnAsync());
            await context.RunSpawnServicesStepAsync("Spawn", service => service.SpawnAsync());
            context.LogActorRegistryCount("After Spawn");
        }
    }
}
