#nullable enable
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition
{
    public sealed class NoOpSceneTransitionCompletionGate : ISceneTransitionCompletionGate
    {
        public Task AwaitBeforeFadeOutAsync(SceneTransitionContext context) => Task.CompletedTask;
    }
}
