#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition
{
    public interface ISceneTransitionService
    {
        /// <summary>
        /// Executa a transição completa conforme o pedido.
        /// </summary>
        Task TransitionAsync(SceneTransitionRequest request);
    }
}
