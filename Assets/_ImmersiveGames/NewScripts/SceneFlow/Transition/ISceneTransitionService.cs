#nullable enable
using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Runtime;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition
{
    public interface ISceneTransitionService
    {
        /// <summary>
        /// Executa a transição completa conforme o pedido.
        /// </summary>
        Task TransitionAsync(SceneTransitionRequest request);
    }
}

