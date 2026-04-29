#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.SceneFlow.Transition
{
    public interface ISceneTransitionService
    {
        /// <summary>
        /// Executa a transição completa conforme o pedido.
        /// </summary>
        Task TransitionAsync(SceneTransitionRequest request);
    }
}

