using System.Threading;
using System.Threading.Tasks;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneComposition
{
    public interface ISceneCompositionExecutor
    {
        Task<SceneCompositionResult> ApplyAsync(SceneCompositionRequest request, CancellationToken ct = default);
    }
}

