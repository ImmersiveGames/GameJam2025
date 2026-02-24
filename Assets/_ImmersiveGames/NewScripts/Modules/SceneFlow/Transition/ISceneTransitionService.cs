#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition
{
    public interface ISceneTransitionService
    {
        /// <summary>
        /// Executa a transição completa de acordo com o pedido.
        /// </summary>
        Task TransitionAsync(SceneTransitionRequest request);
    }
}
