using System;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.LoaderSystems
{
    public class SceneLoaderTester : MonoBehaviour
    {
        private ISceneLoaderService _loader;

        [Header("Nomes das cenas")]
        [SerializeField] private string menuScene = "MenuScene";
        [SerializeField] private string gameplayScene = "GameplayScene";
        [SerializeField] private string uiScene = "UIScene";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            if (DependencyManager.Provider.TryGetGlobal(out ISceneLoaderService loader))
            {
                _loader = loader;
                Debug.Log("<color=green>[SceneLoaderTester] Loader injetado com sucesso.</color>");
            }
            else
            {
                Debug.LogError("<color=red>[SceneLoaderTester] Falha ao injetar SceneLoaderService.</color>");
            }
        }

        private void Update()
        {
            if (_loader == null) return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("<color=cyan>[SceneLoaderTester] Tecla 1 pressionada - Carregando MenuScene...</color>");
                StartCoroutine(_loader.LoadScenesWithFadeAsync(
                    new[]
                    {
                        new SceneLoadData(menuScene, LoadSceneMode.Single)
                    },
                    new[] { gameplayScene, uiScene }
                ));
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("<color=cyan>[SceneLoaderTester] Tecla 2 pressionada - Carregando GameplayScene + UIScene...</color>");
                StartCoroutine(_loader.LoadScenesWithFadeAsync(
                    new[]
                    {
                        new SceneLoadData(gameplayScene, LoadSceneMode.Single),  // base
                        new SceneLoadData(uiScene, LoadSceneMode.Additive)       // overlay
                    },
                    new[] { menuScene }
                ));
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("<color=yellow>[SceneLoaderTester] Tecla R pressionada - Recarregando GameplayScene...</color>");
                StartCoroutine(_loader.ReloadSceneAsync(gameplayScene));
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("<color=grey>[SceneLoaderTester] Cenas carregadas atualmente:</color>");
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    Debug.Log($"  ▶️ {scene.name} (isLoaded: {scene.isLoaded})");
                }
            }
        }
    }
}
