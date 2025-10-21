using System.Collections;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;
namespace _ImmersiveGames.Scripts.LoaderSystems
{
    
    public class SceneLoader : Singleton<SceneLoader>
    {
        [SerializeField] private string gameplayScene = "Gameplay";
        [SerializeField] private string uiScene = "UI";
        private IEnumerator Start()
        {
            DebugUtility.LogVerbose<SceneLoader>("🚀 Starting scene loading sequence...");

            // Carregar cenas na ordem correta
            yield return LoadSceneSequence();
            
            DebugUtility.LogVerbose<SceneLoader>("🎮 All scenes loaded!");
        }

        private IEnumerator LoadSceneSequence()
        {
            // 2. Gameplay depois (emissores de eventos)
            if (!IsSceneLoaded(gameplayScene))
            {
                yield return SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Additive);
            }
            // 1. UI primeiro (handlers)
            if (!IsSceneLoaded(uiScene))
            {
                yield return LoadUISceneAdditive(uiScene);
                yield return new WaitForEndOfFrame(); // Espera registros
            }

            
        }

        private bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                    return true;
            }
            return false;
        }
        public IEnumerator LoadUISceneAdditive(string uiSceneName)
        {
            if (!SceneManager.GetSceneByName(uiSceneName).isLoaded)
            {
                DebugUtility.LogVerbose<SceneLoader>($"Carregando cena de UI {uiSceneName} em modo aditivo.");
                yield return SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
            }
        }
        public void ReloadCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            DebugUtility.LogVerbose<SceneLoader>($"Recarregando cena atual {currentScene}.");
            SceneManager.LoadSceneAsync(currentScene, LoadSceneMode.Single);
        }
    }
}