#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Adapters
{
    public sealed class NoOpTransitionCompletionGate : ISceneTransitionCompletionGate
    {
        public Task AwaitBeforeFadeOutAsync(SceneTransitionContext context) => Task.CompletedTask;
    }
}
