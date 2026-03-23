using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime.SceneReset
{
    internal interface ISceneResetPhase
    {
        Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct);
    }
}
