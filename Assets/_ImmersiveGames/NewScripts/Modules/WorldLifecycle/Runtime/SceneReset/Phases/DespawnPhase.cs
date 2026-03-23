using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime.SceneReset.Phases
{
    internal sealed class DespawnPhase : ISceneResetPhase
    {
        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            await context.RunSpawnServicesStepAsync("Despawn", service => service.DespawnAsync());
            context.LogActorRegistryCount("After Despawn");
        }
    }
}
