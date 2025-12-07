﻿using UnityEngine;
using UnityEngine.SceneManagement;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

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
            if (DependencyManager.Provider.TryGetGlobal(out ISceneLoaderService loader))
            {
                _loader = loader;
                Debug.Log("<color=green>[SceneLoaderTester] Loader injetado com sucesso.</color>");
            }
            else
            {
                Debug.LogError("[SceneLoaderTester] ISceneLoaderService não encontrado no DependencyManager.");
            }
        }

        private void Update()
        {
            if (_loader == null)
                return;

            // 1: Ir para o Menu (Menu = Single, descarrega Gameplay + UI)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("<color=cyan>[SceneLoaderTester] Tecla 1 pressionada - Carregando MenuScene...</color>");
                StartCoroutine(_loader.LoadScenesWithFadeAsync(
                    scenesToLoad: new[]
                    {
                        new SceneLoadData(menuScene, LoadSceneMode.Single)
                    },
                    scenesToUnload: new[]
                    {
                        gameplayScene,
                        uiScene
                    }
                ));
            }

            // 2: Ir para Gameplay + UI (ambas Additive), descarregando Menu
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("<color=cyan>[SceneLoaderTester] Tecla 2 pressionada - Carregando GameplayScene + UIScene...</color>");
                StartCoroutine(_loader.LoadScenesWithFadeAsync(
                    scenesToLoad: new[]
                    {
                        new SceneLoadData(gameplayScene),
                        new SceneLoadData(uiScene)
                    },
                    scenesToUnload: new[]
                    {
                        menuScene
                    }
                ));
            }

            // R: Recarregar Gameplay atual (sem mexer na UI explicitamente)
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("<color=yellow>[SceneLoaderTester] Tecla R pressionada - Recarregando GameplayScene...</color>");
                StartCoroutine(_loader.ReloadSceneAsync(gameplayScene));
            }

            // L: Listar cenas carregadas
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
