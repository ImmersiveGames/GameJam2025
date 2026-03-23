using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime.SceneReset.Phases
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
