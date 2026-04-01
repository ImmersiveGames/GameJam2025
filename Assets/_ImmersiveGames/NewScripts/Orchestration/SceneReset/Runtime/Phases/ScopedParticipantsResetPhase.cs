using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime.Phases
{
    internal sealed class ScopedParticipantsResetPhase : ISceneResetPhase
    {
        private static readonly SceneResetGameplayResetExecutor Executor = new();

        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            _ = hookRunner;

            System.Diagnostics.Debug.Assert(context != null);
            if (context == null)
            {
                return;
            }

            await Executor.ExecuteAsync(context, ct);
        }
    }
}
