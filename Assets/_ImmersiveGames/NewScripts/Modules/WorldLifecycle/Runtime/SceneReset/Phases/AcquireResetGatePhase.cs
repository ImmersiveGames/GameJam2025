using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime.SceneReset.Phases
{
    internal sealed class AcquireResetGatePhase : ISceneResetPhase
    {
        public Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            context.AcquireGateIfNeeded();
            return Task.CompletedTask;
        }
    }
}
