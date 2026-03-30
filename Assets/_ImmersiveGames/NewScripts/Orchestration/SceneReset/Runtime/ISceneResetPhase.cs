using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime
{
    internal interface ISceneResetPhase
    {
        Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct);
    }
}
