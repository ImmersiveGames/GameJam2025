using System.Collections;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private string gameplayScene = "Gameplay";
        [SerializeField] private string uiScene = "UI";
        // 🔥 REMOVIDO: private ResourceUIFactory resourceUIFactory;

        private IEnumerator Start()
        {
            DebugUtility.LogVerbose<SceneLoader>("🚀 Starting scene loading sequence...");
            
            // 🔥 REMOVIDO registro da factory antiga
            // O novo sistema usa UI pré-existente, não factory

            // Carregar cenas na ordem correta
            yield return LoadSceneSequence();
            
            DebugUtility.LogVerbose<SceneLoader>("🎮 All scenes loaded!");
        }

        private IEnumerator LoadSceneSequence()
        {
            // 1. UI primeiro (handlers)
            if (!IsSceneLoaded(uiScene))
            {
                yield return SceneManager.LoadSceneAsync(uiScene, LoadSceneMode.Additive);
                yield return new WaitForEndOfFrame(); // Espera registros
            }

            // 2. Gameplay depois (emissores de eventos)
            if (!IsSceneLoaded(gameplayScene))
            {
                yield return SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Additive);
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
    }
}