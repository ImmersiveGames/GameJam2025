using System.Threading;
using System.Threading.Tasks;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneReset.Runtime.Phases
{
    internal sealed class AfterDespawnHooksPhase : ISceneResetPhase
    {
        public Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            return hookRunner.RunWorldHooksAsync(context, "OnAfterDespawn", hook => hook.OnAfterDespawnAsync());
        }
    }
}

