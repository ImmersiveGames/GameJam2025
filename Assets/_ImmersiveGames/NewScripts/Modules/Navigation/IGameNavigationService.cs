using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.Navigation
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

        /// <summary>
        /// Inicia gameplay a partir de um LevelId (LevelFlow -> SceneRouteId + payload).
        /// </summary>
        Task StartGameplayAsync(LevelId levelId, string reason = null);
    }
}
