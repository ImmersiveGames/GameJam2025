using System.Threading;
using System.Threading.Tasks;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneReset.Runtime.Phases
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

