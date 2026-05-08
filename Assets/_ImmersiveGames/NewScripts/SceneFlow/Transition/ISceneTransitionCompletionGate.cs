using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SceneRouting.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.SceneRouting.Transition
{
    public interface ISceneTransitionCompletionGate
    {
        Task AwaitBeforeFadeOutAsync(SceneTransitionContext context);
    }
}
