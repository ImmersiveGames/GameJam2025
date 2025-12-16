using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Inicializa serviços de escopo de cena para o NewScripts e garante limpeza determinística.
    /// </summary>
    public sealed class NewSceneBootstrapper : MonoBehaviour
    {
        private string _sceneName = string.Empty;

        private void Awake()
        {
            _sceneName = SceneManager.GetActiveScene().name;

            DependencyManager.Provider.RegisterForScene<INewSceneScopeMarker>(_sceneName, new NewSceneScopeMarker());

            DebugUtility.Log(typeof(NewSceneBootstrapper), $"Scene scope created: {_sceneName}");
        }

        private void OnDestroy()
        {
            if (string.IsNullOrEmpty(_sceneName))
            {
                _sceneName = SceneManager.GetActiveScene().name;
            }

            DependencyManager.Provider.ClearSceneServices(_sceneName);

            DebugUtility.Log(typeof(NewSceneBootstrapper), $"Scene scope cleared: {_sceneName}");
        }
    }
}
