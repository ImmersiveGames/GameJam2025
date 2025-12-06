using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    /// <summary>
    /// Serviço de fade da tela. Expõe métodos fire-and-forget
    /// e também corrotinas aguardáveis.
    /// </summary>
    public interface IFadeService
    {
        /// <summary>Dispara um FadeIn (tela escurecendo até alpha=1) de forma assíncrona.</summary>
        void RequestFadeIn();

        /// <summary>Dispara um FadeOut (tela clareando até alpha=0) de forma assíncrona.</summary>
        void RequestFadeOut();

        /// <summary>Executa FadeIn e só retorna quando o efeito terminar.</summary>
        IEnumerator FadeInAsync();

        /// <summary>Executa FadeOut e só retorna quando o efeito terminar.</summary>
        IEnumerator FadeOutAsync();
    }

    /// <summary>
    /// Runner global de corrotinas, para desacoplar serviços de MonoBehaviours específicos.
    /// </summary>
    public interface ICoroutineRunner
    {
        Coroutine Run(IEnumerator coroutine);
    }
    /// <summary>
    /// Adaptador para aguardar o término de fades usando async/await,
    /// encapsulando o uso de corrotinas do IFadeService.
    /// </summary>
    public interface IFadeAwaiter
    {
        Task FadeInAsync();
        Task FadeOutAsync();
    }
}