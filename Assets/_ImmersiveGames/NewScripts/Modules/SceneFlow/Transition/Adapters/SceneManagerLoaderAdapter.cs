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
            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op == null)
            {
                DebugUtility.LogWarning<SceneManagerLoaderAdapter>(
                    $"[SceneFlow] UnloadSceneAsync retornou null para '{sceneName}'.");
                return;
            }

            while (!op.isDone)
            {
                await Task.Yield();
            }
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
