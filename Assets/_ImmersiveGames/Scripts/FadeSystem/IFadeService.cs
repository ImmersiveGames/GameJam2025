using System.Threading.Tasks;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    public interface IFadeService
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