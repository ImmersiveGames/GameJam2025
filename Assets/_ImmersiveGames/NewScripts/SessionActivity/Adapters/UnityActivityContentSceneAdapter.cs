using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class UnityActivityContentSceneAdapter : IActivityContentSceneAdapter
    {
        public async Task<ActivityContentSceneLoadResult> LoadAdditiveAsync(ActivityContentSceneLoadCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityContentSceneLoadCommand is invalid.");
            }

            if (!Application.CanStreamedLevelBeLoaded(command.SceneName))
            {
                return new ActivityContentSceneLoadResult(
                    ActivityContentLoadResultKind.Rejected,
                    command,
                    command.Source,
                    command.Reason,
                    $"Activity content scene '{command.SceneName}' cannot be loaded.");
            }

            Scene loadedScene = SceneManager.GetSceneByName(command.SceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                return new ActivityContentSceneLoadResult(
                    ActivityContentLoadResultKind.Loaded,
                    command,
                    command.Source,
                    command.Reason,
                    $"Activity content scene '{command.SceneName}' already loaded.");
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(command.SceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                return new ActivityContentSceneLoadResult(
                    ActivityContentLoadResultKind.Failed,
                    command,
                    command.Source,
                    command.Reason,
                    $"LoadSceneAsync returned null for activity content scene '{command.SceneName}'.");
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            loadedScene = SceneManager.GetSceneByName(command.SceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return new ActivityContentSceneLoadResult(
                    ActivityContentLoadResultKind.Failed,
                    command,
                    command.Source,
                    command.Reason,
                    $"Activity content scene '{command.SceneName}' was not loaded after completion.");
            }

            return new ActivityContentSceneLoadResult(
                ActivityContentLoadResultKind.Loaded,
                command,
                command.Source,
                command.Reason,
                $"Activity content scene '{command.SceneName}' loaded additive.");
        }
    }
}
