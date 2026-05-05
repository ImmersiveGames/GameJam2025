using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;

namespace _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Gate explicito de no-content para o profile Base11Sandbox.
    /// Mantem o SceneFlow operacional sem reintroduzir WorldReset ou IntroStage como ownership de completion.
    /// </summary>
    public sealed class Base11SandboxTransitionCompletionGate : ISceneTransitionCompletionGate
    {
        public Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            DebugUtility.Log(typeof(Base11SandboxTransitionCompletionGate),
                $"[OBS][SceneFlow][Gate] Base11Sandbox no-content; transition completion liberado sem espera. routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);

            return Task.CompletedTask;
        }
    }
}
