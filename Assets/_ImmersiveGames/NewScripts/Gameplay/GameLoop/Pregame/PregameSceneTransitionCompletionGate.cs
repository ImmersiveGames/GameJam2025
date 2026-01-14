#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PregameSceneTransitionCompletionGate : ISceneTransitionCompletionGate
    {
        private readonly ISceneTransitionCompletionGate _innerGate;

        public PregameSceneTransitionCompletionGate(ISceneTransitionCompletionGate innerGate)
        {
            _innerGate = innerGate ?? new NoOpSceneTransitionCompletionGate();
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            await _innerGate.AwaitBeforeFadeOutAsync(context);
        }
    }
}
