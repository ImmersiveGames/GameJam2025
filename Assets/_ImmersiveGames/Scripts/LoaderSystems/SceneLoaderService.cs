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

        public SceneLoaderService()
        {
            if (DependencyManager.Provider.TryGetGlobal(out IFadeService fadeService))
            {
                _fade = fadeService;
                Debug.Log("[SceneLoaderService] FadeService injetado.");
            }
            else
            {
                Debug.LogWarning("[SceneLoaderService] FadeService não encontrado.");
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

                    // Garante que cena Single se torne ativa
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
                    // Proteção: só descarrega se não for a cena ativa
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
            Debug.Log("[SceneLoaderService] Iniciando LoadScenesWithFadeAsync");

            if (_fade != null)
            {
                _fade.RequestFadeIn();
                yield return new WaitForSecondsRealtime(0.6f);
            }

            if (scenesToLoad != null)
                yield return LoadScenesAsync(scenesToLoad);

            if (scenesToUnload != null)
                yield return UnloadScenesAsync(scenesToUnload);

            if (_fade != null)
            {
                _fade.RequestFadeOut();
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
