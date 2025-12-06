using System.Collections;
using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.LoaderSystems
{
    /// <summary>
    /// Serviço responsável por orquestrar carregamento, descarregamento e recarga de cenas.
    /// Implementações típicas devem ser stateless e acessadas via DependencyManager.
    /// </summary>
    public interface ISceneLoaderService
    {
        IEnumerator LoadScenesAsync(IEnumerable<SceneLoadData> scenes);
        IEnumerator UnloadScenesAsync(IEnumerable<string> sceneNames);
        IEnumerator ReloadSceneAsync(string sceneName);

        /// <summary>
        /// Carrega cenas com suporte a fade (se IFadeService estiver disponível).
        /// Opcionalmente descarrega outras cenas após o carregamento.
        /// </summary>
        IEnumerator LoadScenesWithFadeAsync(
            IEnumerable<SceneLoadData> scenesToLoad,
            IEnumerable<string> scenesToUnload = null
        );

        bool IsSceneLoaded(string sceneName);
    }
}