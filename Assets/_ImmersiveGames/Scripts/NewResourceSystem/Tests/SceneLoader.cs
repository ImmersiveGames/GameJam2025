using System.Collections;
using _ImmersiveGames.Scripts.NewResourceSystem.Events;
using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.NewResourceSystem.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private string gameplayScene = "Gameplay";
        [SerializeField] private string uiScene = "UI";

        [Header("Inicializar automaticamente os serviços")]
        [SerializeField] private ResourceUIFactory resourceUIFactory;
        private IEnumerator Start()
        {
            DebugUtility.LogVerbose<SceneLoader>("🚀 Starting scene loading sequence...");
            if (resourceUIFactory != null)
            {
                DependencyManager.Instance.RegisterGlobal<IUIFactory<ResourceBindEvent, IResourceUI>>(resourceUIFactory);
                DebugUtility.Log<SceneLoader>("✅ ResourceUIFactory registrado como serviço global", "green", this);
            }
            else
            {
                DebugUtility.LogError<SceneLoader>("❌ ResourceUIFactory não atribuído no Inspector!", this);
            }
        
            // 1. Primeiro carrega a UI (onde está o Handler)
            if (!SceneManager.GetSceneByName(uiScene).isLoaded)
            {
                DebugUtility.LogVerbose<SceneLoader>("📥 Loading UI scene...");
                yield return SceneManager.LoadSceneAsync(uiScene, LoadSceneMode.Additive);
            }
        
            // 2. Espera um frame para o Handler se registrar
            yield return new WaitForEndOfFrame();
            DebugUtility.LogVerbose<SceneLoader>("✅ UI scene loaded, handler should be registered");
        
            // 3. Agora carrega a gameplay (que emite eventos)
            if (!SceneManager.GetSceneByName(gameplayScene).isLoaded)
            {
                DebugUtility.LogVerbose<SceneLoader>("📥 Loading Gameplay scene...");
                yield return SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Additive);
            }
        
            DebugUtility.LogVerbose<SceneLoader>("🎮 All scenes loaded successfully!");
        }
    }
}