using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Runtime
{
    internal interface ISceneResetPhase
    {
        Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct);
    }
}

