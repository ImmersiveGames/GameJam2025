using System.Threading.Tasks;
namespace _ImmersiveGames.Scripts.LegadoFadeSystem
{
    public interface ILegadoFadeService
    {
        /// <summary>
        /// Solicita um fade-in (tela escurece).
        /// </summary>
        Task FadeInAsync();

        /// <summary>
        /// Solicita um fade-out (tela volta a ficar visível).
        /// </summary>
        Task FadeOutAsync();
    }
}
