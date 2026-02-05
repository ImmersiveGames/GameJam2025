using System.Collections.Generic;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Respons�vel por calcular um plano de transi��o (contexto)
    /// a partir do estado atual e de um alvo de cenas.
    /// 
    /// Fase 1:
    /// - Mant�m o m�todo legado baseado em lista de nomes de cena.
    /// - Adiciona sobrecarga baseada em SceneGroupProfile.
    /// </summary>
    public interface ISceneTransitionPlanner
    {
        /// <summary>
        /// Vers�o legada:
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
        /// Nova vers�o baseada em SceneGroupProfile.
        /// </summary>
        SceneTransitionContext BuildContext(
            SceneState currentState,
            SceneGroupProfile targetGroup);

        /// <summary>
        /// Vers�o estendida com origem/destino expl�citos.
        /// Ideal para debugs, analytics ou regras especiais 
        /// de unload com base no grupo de origem.
        /// </summary>
        SceneTransitionContext BuildContext(
            SceneState currentState,
            SceneGroupProfile fromGroup,
            SceneGroupProfile toGroup);
    }
}
