#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Adapters
{
    /// <summary>
    /// Implementação padrão de loader usando SceneManager (fallback).
    /// </summary>
    public sealed class SceneManagerLoaderAdapter : ISceneFlowLoaderAdapter
    {
        public async Task LoadSceneAsync(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                DebugUtility.LogWarning<SceneManagerLoaderAdapter>(
                    $"[SceneFlow] LoadSceneAsync retornou null para '{sceneName}'.");
                return;
            }

            while (!op.isDone)
            {
                await Task.Yield();
            }
        }

        public async Task UnloadSceneAsync(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            bool exists = scene.IsValid();
            bool isLoaded = exists && scene.isLoaded;
            bool isActiveScene = exists && scene == SceneManager.GetActiveScene();

            DebugUtility.Log<SceneManagerLoaderAdapter>(
                $"[OBS][SceneFlow] UnloadAttempt scene='{sceneName}' exists={exists} isLoaded={isLoaded} isActiveScene={isActiveScene}.",
                DebugUtility.Colors.Info);

            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op == null)
            {
                DebugUtility.LogWarning<SceneManagerLoaderAdapter>(
                    $"[OBS][SceneFlow] UnloadAttempt scene='{sceneName}' result='null_operation' exists={exists} isLoaded={isLoaded} isActiveScene={isActiveScene}.");
                return;
            }

            while (!op.isDone)
            {
                await Task.Yield();
            }

            var sceneAfter = SceneManager.GetSceneByName(sceneName);
            bool existsAfter = sceneAfter.IsValid();
            bool isLoadedAfter = existsAfter && sceneAfter.isLoaded;

            DebugUtility.Log<SceneManagerLoaderAdapter>(
                $"[OBS][SceneFlow] UnloadAttempt scene='{sceneName}' result='completed' existsAfter={existsAfter} isLoadedAfter={isLoadedAfter}.",
                DebugUtility.Colors.Info);
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        public async Task<bool> TrySetActiveSceneAsync(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                DebugUtility.LogWarning<SceneManagerLoaderAdapter>(
                    $"[SceneFlow] Cena '{sceneName}' inválida para SetActiveScene.");
                return false;
            }

            if (!scene.isLoaded)
            {
                DebugUtility.LogWarning<SceneManagerLoaderAdapter>(
                    $"[SceneFlow] Cena '{sceneName}' não está carregada para SetActiveScene.");
                return false;
            }

            SceneManager.SetActiveScene(scene);
            await Task.Yield();
            return true;
        }

        public string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}
