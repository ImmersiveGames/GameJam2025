using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime.SceneReset.Phases
{
    internal sealed class AfterDespawnHooksPhase : ISceneResetPhase
    {
        public Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            return hookRunner.RunWorldHooksAsync(context, "OnAfterDespawn", hook => hook.OnAfterDespawnAsync());
        }
    }
}
