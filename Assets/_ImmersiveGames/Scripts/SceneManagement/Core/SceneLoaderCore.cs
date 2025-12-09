using System.Threading.Tasks;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.SceneManagement.Core
{
    [DebugLevel(DebugLevel.Verbose)]
    public class SceneLoaderCore : ISceneLoader
    {
        public async Task LoadSceneAsync(string sceneName, LoadSceneMode mode)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                DebugUtility.LogError<SceneLoaderCore>("LoadSceneAsync chamado com sceneName vazio.");
                return;
            }

            if (IsSceneLoaded(sceneName))
            {
                DebugUtility.LogWarning<SceneLoaderCore>("Cena '{sceneName}' já está carregada.");
                return;
            }

            DebugUtility.LogVerbose<SceneLoaderCore>("Carregando cena '{sceneName}' (mode={mode}).");

            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(sceneName, mode);
            }
            catch (System.Exception e)
            {
                DebugUtility.LogError<SceneLoaderCore>("Erro ao iniciar load da cena '{sceneName}': {e}");
                return;
            }

            if (op == null)
            {
                DebugUtility.LogError<SceneLoaderCore>("LoadSceneAsync retornou null para '{sceneName}'.");
                return;
            }

            // Usando um loop async sem coroutines
            while (!op.isDone)
                await Task.Yield();

            DebugUtility.Log<SceneLoaderCore>("Cena '{sceneName}' carregada.");
        }

        public async Task UnloadSceneAsync(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                DebugUtility.LogError<SceneLoaderCore>("UnloadSceneAsync chamado com sceneName vazio.");
                return;
            }

            if (!IsSceneLoaded(sceneName))
            {
                DebugUtility.LogWarning<SceneLoaderCore>("Cena '{sceneName}' não está carregada.");
                return;
            }

            DebugUtility.LogVerbose<SceneLoaderCore>("Descarregando cena '{sceneName}'.");

            AsyncOperation op;
            try
            {
                op = SceneManager.UnloadSceneAsync(sceneName);
            }
            catch (System.Exception e)
            {
                DebugUtility.LogError<SceneLoaderCore>("Erro ao iniciar unload da cena '{sceneName}': {e}");
                return;
            }

            if (op == null)
            {
                DebugUtility.LogError<SceneLoaderCore>("UnloadSceneAsync retornou null para '{sceneName}'.");
                return;
            }

            while (!op.isDone)
                await Task.Yield();

            DebugUtility.Log<SceneLoaderCore>("Cena '{sceneName}' descarregada.");
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName)
                    return scene.isLoaded;
            }
            return false;
        }

        public Scene GetSceneByName(string sceneName)
        {
            return SceneManager.GetSceneByName(sceneName);
        }
    }
}
