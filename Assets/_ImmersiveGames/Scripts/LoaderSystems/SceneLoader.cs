using System.Collections;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;
namespace _ImmersiveGames.Scripts.LoaderSystems
{
    
    public class SceneLoader : Singleton<SceneLoader>
    {
        [SerializeField] private string gameplayScene = "GameplayScene";
        [SerializeField] private string uiScene = "UIScene";
        private IEnumerator Start()
        {
            DebugUtility.LogVerbose<SceneLoader>(
                "🚀 Starting scene loading sequence...",
                DebugUtility.Colors.CrucialInfo);

            // Carregar cenas na ordem correta
            yield return LoadSceneSequence();
            
            DebugUtility.LogVerbose<SceneLoader>(
                "🎮 All scenes loaded!",
                DebugUtility.Colors.Success);
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
            if (SceneManager.GetSceneByName(uiSceneName).isLoaded) yield break;
            DebugUtility.LogVerbose<SceneLoader>($"Carregando cena de UI {uiSceneName} em modo aditivo.");
            yield return SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
        }
        public void ReloadCurrentScene()
        {
            // Wrapper para compatibilidade: inicia a coroutine e não bloqueia o chamador.
            StartCoroutine(ReloadCurrentSceneAsync());
        }

        public IEnumerator ReloadCurrentSceneAsync()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            DebugUtility.LogVerbose<SceneLoader>($"Recarregando cena atual {currentScene}.");
            
            // Obtemos o FadeService por DI
            DependencyManager.Provider.TryGetGlobal(out IFadeService fade);
            
            if (fade != null)
            {
                Debug.Log("[SceneLoader] Executando FadeIn antes da recarga.");
                fade.RequestFadeIn();
                yield return new WaitForSecondsRealtime(0.6f); // Aguarda o fade (ajustável)
            }

            AsyncOperation reloadOperation = SceneManager.LoadSceneAsync(currentScene, LoadSceneMode.Single);
            while (reloadOperation is { isDone: false })
            {
                yield return null;
            }

            // Recarrega a UI (separada) após o gameplay para restabelecer handlers de eventos.
            yield return LoadUISceneAdditive(uiScene);
            
            if (fade != null)
            {
                Debug.Log("[SceneLoader] Executando FadeOut após recarga.");
                fade.RequestFadeOut();
            }
        }
    }
}