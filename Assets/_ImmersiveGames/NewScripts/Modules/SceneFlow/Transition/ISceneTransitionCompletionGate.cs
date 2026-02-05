#nullable enable
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition
{
    /// <summary>
    /// Gate opcional para "segurar" o final da transição (FadeOut/Completed) até que
    /// tarefas externas associadas ao mesmo context (ex: WorldLifecycle reset) concluam.
    /// </summary>
    public interface ISceneTransitionCompletionGate
    {
        Task AwaitBeforeFadeOutAsync(SceneTransitionContext context);
    }
}
