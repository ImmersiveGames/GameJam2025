using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class UnitySessionActivityWindowSceneAdapter : ISessionActivityWindowSceneAdapter
    {
        public async Task LoadAdditiveAsync(
            SceneKeyAsset sceneKey,
            string activityId,
            string windowKind,
            string source,
            string reason)
        {
            string sceneName = ResolveSceneNameOrFail(sceneKey, activityId, windowKind, source, reason, "load");

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                return;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                throw new InvalidOperationException($"Activity '{activityId}' {windowKind} additive scene '{sceneName}' returned null LoadSceneAsync operation.");
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            loadedScene = SceneManager.GetSceneByName(sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                throw new InvalidOperationException($"Activity '{activityId}' failed to load {windowKind} additive scene '{sceneName}'.");
            }
        }

        public async Task UnloadAsync(
            SceneKeyAsset sceneKey,
            string activityId,
            string windowKind,
            string source,
            string reason)
        {
            string sceneName = ResolveSceneNameOrFail(sceneKey, activityId, windowKind, source, reason, "unload");

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                throw new InvalidOperationException($"Activity '{activityId}' expected {windowKind} additive scene '{sceneName}' to be loaded before unload, but it is not loaded.");
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(loadedScene);
            if (operation == null)
            {
                throw new InvalidOperationException($"Activity '{activityId}' failed to unload {windowKind} additive scene '{sceneName}' because UnloadSceneAsync returned null.");
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            Scene unloadedScene = SceneManager.GetSceneByName(sceneName);
            if (unloadedScene.IsValid() && unloadedScene.isLoaded)
            {
                throw new InvalidOperationException($"Activity '{activityId}' {windowKind} additive scene '{sceneName}' remained loaded after unload.");
            }
        }

        private static string ResolveSceneNameOrFail(
            SceneKeyAsset sceneKey,
            string activityId,
            string windowKind,
            string source,
            string reason,
            string operation)
        {
            if (sceneKey == null)
            {
                throw new InvalidOperationException($"Activity '{activityId}' requires SceneKeyAsset for {windowKind} {operation}. source='{source}' reason='{reason}'.");
            }

            string sceneName = string.IsNullOrWhiteSpace(sceneKey.SceneName) ? string.Empty : sceneKey.SceneName.Trim();
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new InvalidOperationException($"Activity '{activityId}' requires non-empty SceneName in sceneKey='{sceneKey.name}' for {windowKind} {operation}. source='{source}' reason='{reason}'.");
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new InvalidOperationException($"Activity '{activityId}' {windowKind} additive scene '{sceneName}' cannot be loaded. sceneKey='{sceneKey.name}' source='{source}' reason='{reason}'.");
            }

            return sceneName;
        }
    }
}
