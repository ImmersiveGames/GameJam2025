using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Runtime.Navigation
{
    /// <summary>
    /// Serviço de navegação de produção (NewScripts).
    /// Mantém rotas/planos centralizados e dispara transições via SceneFlow.
    /// </summary>
    public interface IGameNavigationService
    {
        /// <summary>
        /// Navega por id de rota (permite ampliar combinações sem alterar interface).
        /// </summary>
        Task NavigateAsync(string routeId, string reason = null);

        /// <summary>
        /// Conveniência: navegar para Menu.
        /// </summary>
        Task RequestMenuAsync(string reason = null);

        /// <summary>
        /// Conveniência: navegar para Gameplay.
        /// </summary>
        Task RequestGameplayAsync(string reason = null);
    }
}
