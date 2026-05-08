using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.SceneFlow.Transition
{
    public interface ISceneTransitionCompletionGate
    {
        Task AwaitBeforeFadeOutAsync(SceneTransitionContext context);
    }
}
