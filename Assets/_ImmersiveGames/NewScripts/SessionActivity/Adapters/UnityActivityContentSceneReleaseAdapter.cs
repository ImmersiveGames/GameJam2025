using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class UnityActivityContentSceneReleaseAdapter : IActivityContentSceneReleaseAdapter
    {
        public async Task<ActivityContentSceneUnloadResult> UnloadAdditiveAsync(ActivityContentSceneUnloadCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityContentSceneUnloadCommand is invalid.");
            }

            Scene loadedScene = SceneManager.GetSceneByName(command.SceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return new ActivityContentSceneUnloadResult(
                    ActivityContentUnloadResultKind.SkippedNoContent,
                    command,
                    command.Source,
                    command.Reason,
                    $"Activity content scene '{command.SceneName}' is not loaded.");
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(command.SceneName);
            if (operation == null)
            {
                return new ActivityContentSceneUnloadResult(
                    ActivityContentUnloadResultKind.Failed,
                    command,
                    command.Source,
                    command.Reason,
                    $"UnloadSceneAsync returned null for activity content scene '{command.SceneName}'.");
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            loadedScene = SceneManager.GetSceneByName(command.SceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                return new ActivityContentSceneUnloadResult(
                    ActivityContentUnloadResultKind.Failed,
                    command,
                    command.Source,
                    command.Reason,
                    $"Activity content scene '{command.SceneName}' is still loaded after unload completion.");
            }

            return new ActivityContentSceneUnloadResult(
                ActivityContentUnloadResultKind.Unloaded,
                command,
                command.Source,
                command.Reason,
                $"Activity content scene '{command.SceneName}' unloaded additive.");
        }
    }
}
