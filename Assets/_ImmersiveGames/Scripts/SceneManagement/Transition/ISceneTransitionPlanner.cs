using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Responsável por calcular um plano de transição (contexto)
    /// a partir do estado atual e de um alvo de cenas.
    /// </summary>
    public interface ISceneTransitionPlanner
    {
        SceneTransitionContext BuildContext(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            string explicitTargetActiveScene,
            bool useFade);
    }
}