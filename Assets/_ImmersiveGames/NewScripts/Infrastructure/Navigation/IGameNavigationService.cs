using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Infrastructure.Navigation
{
    /// <summary>
    /// Serviço de navegação de alto nível para transições do jogo (produção).
    /// </summary>
    public interface IGameNavigationService
    {
        Task RequestToGameplay(string reason = null);
        Task RequestToMenu(string reason = null);
    }
}
