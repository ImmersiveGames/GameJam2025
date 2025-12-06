using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.LoaderSystems
{
    public class SceneLoaderService : ISceneLoaderService
    {
        private readonly IFadeService _fade;
        private bool _transitionInProgress;

        public SceneLoaderService()
        {
            if (DependencyManager.Provider.TryGetGlobal(out IFadeService fadeService))
            {
                _fade = fadeService;
                Debug.Log("[SceneLoaderService] FadeService injetado.");
            }
            else
            {
                Debug.LogWarning("[SceneLoaderService] FadeService não encontrado. Transições ocorrerão sem fade.");
            }
        }

        public IEnumerator LoadScenesAsync(IEnumerable<SceneLoadData> scenes)
        {
            foreach (var sceneData in scenes)
            {
                if (!IsSceneLoaded(sceneData.SceneName))
                {
                    Debug.Log($"[SceneLoaderService] Carregando cena: {sceneData.SceneName} (Modo: {sceneData.LoadMode})");
                    var op = SceneManager.LoadSceneAsync(sceneData.SceneName, sceneData.LoadMode);
                    if (op == null)
                    {
                        Debug.LogError($"[SceneLoaderService] Falha ao carregar: {sceneData.SceneName}");
                        continue;
                    }

                    yield return op;

                    if (sceneData.LoadMode == LoadSceneMode.Single)
                    {
                        var loaded = SceneManager.GetSceneByName(sceneData.SceneName);
                        if (loaded.IsValid())
                        {
                            SceneManager.SetActiveScene(loaded);
                            Debug.Log($"[SceneLoaderService] SetActiveScene: {loaded.name}");
                        }
                    }
                }
            }
        }

        public IEnumerator UnloadScenesAsync(IEnumerable<string> sceneNames)
        {
            Scene active = SceneManager.GetActiveScene();

            foreach (var name in sceneNames)
            {
                if (IsSceneLoaded(name))
                {
                    if (name == active.name)
                    {
                        Debug.LogWarning($"[SceneLoaderService] Ignorando descarregamento da cena ativa: {name}");
                        continue;
                    }

                    Debug.Log($"[SceneLoaderService] Descarregando cena: {name}");
                    yield return SceneManager.UnloadSceneAsync(name);
                }
            }
        }

        public IEnumerator ReloadSceneAsync(string sceneName)
        {
            if (!IsSceneLoaded(sceneName))
            {
                Debug.LogWarning($"[SceneLoaderService] Cena {sceneName} não está carregada.");
                yield break;
            }

            yield return UnloadScenesAsync(new[] { sceneName });
            yield return LoadScenesAsync(new[]
            {
                new SceneLoadData(sceneName, LoadSceneMode.Single)
            });
        }

        public IEnumerator LoadScenesWithFadeAsync(
            IEnumerable<SceneLoadData> scenesToLoad,
            IEnumerable<string> scenesToUnload = null)
        {
            if (_transitionInProgress)
            {
                Debug.LogWarning("[SceneLoaderService] Já existe uma transição em andamento. Chamada ignorada.");
                yield break;
            }

            _transitionInProgress = true;
            Debug.Log("[SceneLoaderService] Iniciando LoadScenesWithFadeAsync");

            try
            {
                if (_fade != null)
                {
                    // Escurece antes de qualquer load/unload
                    yield return _fade.FadeInAsync();
                }

                if (scenesToLoad != null)
                    yield return LoadScenesAsync(scenesToLoad);

                if (scenesToUnload != null)
                    yield return UnloadScenesAsync(scenesToUnload);

                if (_fade != null)
                {
                    // Clareia quando tudo terminou
                    yield return _fade.FadeOutAsync();
                }
            }
            finally
            {
                _transitionInProgress = false;
            }
        }

        public bool IsSceneLoaded(string sceneName)
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
