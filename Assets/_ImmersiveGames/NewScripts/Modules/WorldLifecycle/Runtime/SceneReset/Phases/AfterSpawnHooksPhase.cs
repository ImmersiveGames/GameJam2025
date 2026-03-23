using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime.SceneReset.Phases
{
    internal sealed class AfterSpawnHooksPhase : ISceneResetPhase
    {
        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            await hookRunner.RunActorHooksAfterSpawnAsync(context);
            await hookRunner.RunWorldHooksAsync(context, "OnAfterSpawn", hook => hook.OnAfterSpawnAsync());
        }
    }
}
