using _ImmersiveGames.Scripts.SceneManagement.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.SceneManagement.Tests
{
    public class SceneLoaderLowLevelTester : MonoBehaviour
    {
        [SerializeField] private string sceneToLoad = "GameplayScene";
        [SerializeField] private string sceneToUnload = "GameplayScene";
        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Additive;

        private ISceneLoader _loader;

        private void Awake()
        {
            _loader = new SceneLoaderCore();
        }

        private async void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("[Tester] Carregando...");
                await _loader.LoadSceneAsync(sceneToLoad, loadMode);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("[Tester] Descarregando...");
                await _loader.UnloadSceneAsync(sceneToUnload);
            }
        }
    }
}