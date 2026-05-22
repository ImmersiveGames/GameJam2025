using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.SceneComposition
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneCompositionExecutor : ISceneCompositionExecutor
    {
        public async Task<SceneCompositionResult> ApplyAsync(SceneCompositionRequest request, CancellationToken ct = default)
        {
            if (!request.HasOperations)
            {
                DebugUtility.Log<SceneCompositionExecutor>(
                    $"[OBS][SceneComposition] {GetScopePrefix(request.Scope)}CompositionNoOp correlationId='{request.CorrelationId}' reason='{request.Reason}'.",
                    DebugUtility.Colors.Info);

                return new SceneCompositionResult(
                    success: true,
                    scope: request.Scope,
                    reason: request.Reason,
                    correlationId: request.CorrelationId,
                    scenesAdded: 0,
                    scenesRemoved: 0,
                    activeScene: request.ActiveScene);
            }

            bool activeSceneAlreadyLoaded = IsSceneLoaded(request.ActiveScene);
            bool activeSceneScheduledToLoad = IsSceneScheduledToLoad(request.ActiveScene, request.ScenesToLoad);

            List<string> addedScenes = await LoadScenesAsync(request.ScenesToLoad, ct);
            EnsureActiveSceneAvailableOrFail(request.ActiveScene, activeSceneAlreadyLoaded, activeSceneScheduledToLoad, request.CorrelationId, request.Reason);
            ApplyActiveSceneIfRequested(request.ActiveScene, request.CorrelationId, request.Reason);
            List<string> removedScenes = await UnloadScenesAsync(request.ScenesToUnload, request.ActiveScene, ct);

            string addedList = string.Join(",", addedScenes);
            string removedList = string.Join(",", removedScenes);
            string activeSceneLabel = string.IsNullOrWhiteSpace(request.ActiveScene) ? "<none>" : request.ActiveScene;

            DebugUtility.Log<SceneCompositionExecutor>(
                $"[OBS][SceneComposition] LocalCompositionApplied correlationId='{request.CorrelationId}' scenesToLoad=[{string.Join(",", request.ScenesToLoad)}] scenesToUnload=[{string.Join(",", request.ScenesToUnload)}] addedScenes=[{addedList}] removedScenes=[{removedList}] activeScene='{activeSceneLabel}' addedCount={addedScenes.Count} removedCount={removedScenes.Count} reason='{request.Reason}'.",
                DebugUtility.Colors.Info);

            return new SceneCompositionResult(
                success: true,
                scope: request.Scope,
                reason: request.Reason,
                correlationId: request.CorrelationId,
                scenesAdded: addedScenes.Count,
                scenesRemoved: removedScenes.Count,
                activeScene: request.ActiveScene);
        }

        private static async Task<List<string>> UnloadScenesAsync(IReadOnlyList<string> scenesToUnload, string activeSceneName, CancellationToken ct)
        {
            List<string> removedScenes = new List<string>();
            HashSet<string> dedupe = new HashSet<string>(StringComparer.Ordinal);
            string normalizedActiveScene = NormalizeSceneName(activeSceneName);

            if (scenesToUnload == null)
            {
                return removedScenes;
            }

            for (int i = 0; i < scenesToUnload.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                string sceneName = NormalizeSceneNameOrFail(scenesToUnload[i], "Unload");
                if (!dedupe.Add(sceneName))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(normalizedActiveScene) &&
                    string.Equals(sceneName, normalizedActiveScene, StringComparison.Ordinal))
                {
                    continue;
                }

                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                DebugUtility.Log<SceneCompositionExecutor>(
                    $"[OBS][SceneComposition] UnloadSceneStarted scene='{sceneName}'.",
                    DebugUtility.Colors.Info);

                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
                if (unloadOperation == null)
                {
                    HardFailFastH1.Trigger(typeof(SceneCompositionExecutor),
                        $"[FATAL][H1][SceneComposition] UnloadSceneAsync returned null for scene='{sceneName}'.");
                }

                while (!unloadOperation.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                removedScenes.Add(sceneName);
                DebugUtility.Log<SceneCompositionExecutor>(
                    $"[OBS][SceneComposition] UnloadSceneCompleted scene='{sceneName}'.",
                    DebugUtility.Colors.Info);
            }

            return removedScenes;
        }

        private static async Task<List<string>> LoadScenesAsync(IReadOnlyList<string> scenesToLoad, CancellationToken ct)
        {
            List<string> addedScenes = new List<string>();
            HashSet<string> dedupe = new HashSet<string>(StringComparer.Ordinal);

            if (scenesToLoad == null)
            {
                return addedScenes;
            }

            for (int i = 0; i < scenesToLoad.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                string sceneName = NormalizeSceneNameOrFail(scenesToLoad[i], "Load");
                if (!dedupe.Add(sceneName))
                {
                    continue;
                }

                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    DebugUtility.Log<SceneCompositionExecutor>(
                        $"[OBS][SceneComposition] LoadSceneStarted scene='{sceneName}'.",
                        DebugUtility.Colors.Info);

                    AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    if (loadOperation == null)
                    {
                        HardFailFastH1.Trigger(typeof(SceneCompositionExecutor),
                            $"[FATAL][H1][SceneComposition] LoadSceneAsync returned null for scene='{sceneName}'.");
                    }

                    while (!loadOperation.isDone)
                    {
                        ct.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }

                    DebugUtility.Log<SceneCompositionExecutor>(
                        $"[OBS][SceneComposition] LoadSceneCompleted scene='{sceneName}'.",
                        DebugUtility.Colors.Info);
                }
                else
                {
                    DebugUtility.Log<SceneCompositionExecutor>(
                        $"[OBS][SceneComposition] LoadSceneCompleted scene='{sceneName}' alreadyLoaded='true'.",
                        DebugUtility.Colors.Info);
                }

                addedScenes.Add(sceneName);
            }

            return addedScenes;
        }

        private static void ApplyActiveSceneIfRequested(string activeSceneName, string correlationId, string reason)
        {
            if (string.IsNullOrWhiteSpace(activeSceneName))
            {
                return;
            }

            Scene scene = SceneManager.GetSceneByName(activeSceneName.Trim());
            if (!scene.IsValid() || !scene.isLoaded)
            {
                HardFailFastH1.Trigger(typeof(SceneCompositionExecutor),
                    $"[FATAL][H1][SceneComposition] Active scene '{activeSceneName}' is not loaded. correlationId='{correlationId}' reason='{reason}'.");
            }

            if (!SceneManager.SetActiveScene(scene))
            {
                HardFailFastH1.Trigger(typeof(SceneCompositionExecutor),
                    $"[FATAL][H1][SceneComposition] Failed to set active scene='{activeSceneName}'. correlationId='{correlationId}' reason='{reason}'.");
            }

            DebugUtility.Log<SceneCompositionExecutor>(
                $"[OBS][SceneComposition] SetActiveScene scene='{activeSceneName}' correlationId='{correlationId}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }


        private static string GetScopePrefix(SceneCompositionScope scope)
        {
            return scope switch
            {
                SceneCompositionScope.Local => "Local",
                SceneCompositionScope.Macro => "Macro",
                _ => "Unknown"
            };
        }

        private static string NormalizeSceneNameOrFail(string sceneName, string phase)
        {
            string normalized = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            if (!string.IsNullOrEmpty(normalized))
            {
                return normalized;
            }

            HardFailFastH1.Trigger(typeof(SceneCompositionExecutor),
                $"[FATAL][H1][SceneComposition] Empty scene name detected during phase='{phase}'.");
            return string.Empty;
        }

        private static string NormalizeSceneName(string sceneName)
        {
            return string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
        }

        private static bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName.Trim());
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool IsSceneScheduledToLoad(string activeSceneName, IReadOnlyList<string> scenesToLoad)
        {
            if (string.IsNullOrWhiteSpace(activeSceneName) || scenesToLoad == null)
            {
                return false;
            }

            string normalizedActiveScene = activeSceneName.Trim();
            for (int i = 0; i < scenesToLoad.Count; i++)
            {
                string sceneName = scenesToLoad[i];
                if (!string.IsNullOrWhiteSpace(sceneName) &&
                    string.Equals(sceneName.Trim(), normalizedActiveScene, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureActiveSceneAvailableOrFail(
            string activeSceneName,
            bool activeSceneAlreadyLoaded,
            bool activeSceneScheduledToLoad,
            string correlationId,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(activeSceneName))
            {
                return;
            }

            if (!activeSceneAlreadyLoaded && !activeSceneScheduledToLoad)
            {
                HardFailFastH1.Trigger(typeof(SceneCompositionExecutor),
                    $"[FATAL][H1][SceneComposition] Active scene '{activeSceneName}' is neither loaded nor scheduled to load. correlationId='{correlationId}' reason='{reason}'.");
            }
        }
    }
}


