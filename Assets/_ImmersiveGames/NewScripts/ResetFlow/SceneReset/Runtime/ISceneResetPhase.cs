using System.Threading;
using System.Threading.Tasks;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneReset.Runtime
{
    internal interface ISceneResetPhase
    {
        Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct);
    }
}

