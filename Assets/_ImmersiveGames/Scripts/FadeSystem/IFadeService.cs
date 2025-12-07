using System.Threading.Tasks;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    /// <summary>
    /// Serviço de fade da tela baseado em Task.
    /// - FadeInAsync / FadeOutAsync: API principal assíncrona.
    /// - RequestFadeIn / RequestFadeOut: wrappers fire-and-forget para código legado.
    /// </summary>
    public interface IFadeService
    {
        /// <summary>
        /// Dispara um FadeIn de forma fire-and-forget (para código legado).
        /// Internamente chama FadeInAsync().
        /// </summary>
        void RequestFadeIn();

        /// <summary>
        /// Dispara um FadeOut de forma fire-and-forget (para código legado).
        /// Internamente chama FadeOutAsync().
        /// </summary>
        void RequestFadeOut();

        /// <summary>
        /// Executa FadeIn e só retorna quando o efeito terminar.
        /// </summary>
        Task FadeInAsync();

        /// <summary>
        /// Executa FadeOut e só retorna quando o efeito terminar.
        /// </summary>
        Task FadeOutAsync();
    }
}