using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneComposition
{
    public interface ISceneCompositionExecutor
    {
        Task<SceneCompositionResult> ApplyAsync(SceneCompositionRequest request, CancellationToken ct = default);
    }
}
