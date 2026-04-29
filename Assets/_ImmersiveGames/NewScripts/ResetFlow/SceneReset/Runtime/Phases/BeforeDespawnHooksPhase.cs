using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Runtime.Phases
{
    internal sealed class BeforeDespawnHooksPhase : ISceneResetPhase
    {
        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            await hookRunner.RunWorldHooksAsync(context, "OnBeforeDespawn", hook => hook.OnBeforeDespawnAsync());
            await hookRunner.RunActorHooksBeforeDespawnAsync(context);
        }
    }
}

