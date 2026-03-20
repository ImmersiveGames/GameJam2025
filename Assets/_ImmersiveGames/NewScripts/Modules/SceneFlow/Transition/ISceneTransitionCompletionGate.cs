#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition
{
    /// <summary>
    /// Gate opcional para "segurar" o final da transição (FadeOut/Completed) até que
    /// tarefas externas associadas ao mesmo context (ex: WorldLifecycle reset) concluam.
    /// </summary>
        /// <summary>
    /// OWNER: contrato de espera antes do FadeOut no pipeline SceneFlow.
    /// NAO E OWNER: regras de reset/preparo especificas de modulo.
    /// PUBLISH/CONSUME: sem EventBus direto; invocado por SceneTransitionService.
    /// Fases tocadas: entre ScenesReady e BeforeFadeOut.
    /// </summary>
public interface ISceneTransitionCompletionGate
    {
        Task AwaitBeforeFadeOutAsync(SceneTransitionContext context);
    }
}


