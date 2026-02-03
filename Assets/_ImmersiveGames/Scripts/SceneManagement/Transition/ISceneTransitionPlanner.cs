using System.Collections.Generic;
using _ImmersiveGames.Scripts.SceneManagement.Configs;

namespace _ImmersiveGames.Scripts.SceneManagement.OldTransition
{
    /// <summary>
    /// Responsável por calcular um plano de transição (contexto)
    /// a partir do estado atual e de um alvo de cenas.
    /// 
    /// Fase 1:
    /// - Mantém o método legado baseado em lista de nomes de cena.
    /// - Adiciona sobrecarga baseada em SceneGroupProfile.
    /// </summary>
    public interface ISceneTransitionPlanner
    {
        /// <summary>
        /// Versão legada:
        /// - Recebe a lista de cenas alvo (nomes);
        /// - Calcula quais devem ser carregadas e descarregadas;
        /// - Define a cena ativa e se deve usar fade.
        /// </summary>
        SceneTransitionContext BuildContext(
            SceneState currentState,
            IReadOnlyList<string> targetScenes,
            string explicitTargetActiveScene,
            bool useFade);

        /// <summary>
        /// Nova versão baseada em SceneGroupProfile.
        /// </summary>
        SceneTransitionContext BuildContext(
            SceneState currentState,
            SceneGroupProfile targetGroup);

        /// <summary>
        /// Versão estendida com origem/destino explícitos.
        /// Ideal para debugs, analytics ou regras especiais 
        /// de unload com base no grupo de origem.
        /// </summary>
        SceneTransitionContext BuildContext(
            SceneState currentState,
            SceneGroupProfile fromGroup,
            SceneGroupProfile toGroup);
    }
}
