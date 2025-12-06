using System.Threading.Tasks;

namespace _ImmersiveGames.Scripts.SceneManagement.Flow
{
    /// <summary>
    /// Serviço de alto nível que orquestra o fluxo de cenas do jogo
    /// (Menu, Gameplay, Reset, etc.).
    /// </summary>
    public interface IGameSceneFlowService
    {
        /// <summary>
        /// Transita para o conjunto de cenas de Menu.
        /// </summary>
        Task GoToMenuAsync();

        /// <summary>
        /// Transita para o conjunto de cenas de Gameplay (Gameplay + UI).
        /// </summary>
        Task GoToGameplayAsync();

        /// <summary>
        /// Executa um reset de jogo.
        /// Implementação padrão: voltar para o setup de Gameplay “limpo”.
        /// </summary>
        Task ResetGameAsync();
    }
}