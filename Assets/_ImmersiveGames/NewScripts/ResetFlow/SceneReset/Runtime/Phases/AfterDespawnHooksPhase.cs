using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Runtime.Phases
{
    internal sealed class AfterDespawnHooksPhase : ISceneResetPhase
    {
        public Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            return hookRunner.RunWorldHooksAsync(context, "OnAfterDespawn", hook => hook.OnAfterDespawnAsync());
        }
    }
}

