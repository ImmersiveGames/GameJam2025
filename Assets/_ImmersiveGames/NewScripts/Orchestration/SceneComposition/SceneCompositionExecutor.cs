using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneComposition
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

            List<string> removedScenes = await UnloadScenesAsync(request.ScenesToUnload, ct);
            List<string> addedScenes = await LoadScenesAsync(request.ScenesToLoad, ct);
            ApplyActiveSceneIfRequested(request.ActiveScene, request.CorrelationId, request.Reason);

            string addedList = string.Join(",", addedScenes);
            string removedList = string.Join(",", removedScenes);
            bool isClear = request.ScenesToLoad.Count == 0 && request.ScenesToUnload.Count > 0 && string.IsNullOrWhiteSpace(request.ActiveScene);
            string activeSceneLabel = string.IsNullOrWhiteSpace(request.ActiveScene) ? "<none>" : request.ActiveScene;

            string scopePrefix = GetScopePrefix(request.Scope);

            DebugUtility.Log<SceneCompositionExecutor>(
                isClear
                    ? $"[OBS][SceneComposition] {scopePrefix}CompositionCleared correlationId='{request.CorrelationId}' scenesRemoved=[{removedList}] removedCount={removedScenes.Count} activeScene='{activeSceneLabel}' reason='{request.Reason}'."
                    : $"[OBS][SceneComposition] {scopePrefix}CompositionApplied correlationId='{request.CorrelationId}' scenesToLoad=[{string.Join(",", request.ScenesToLoad)}] scenesToUnload=[{string.Join(",", request.ScenesToUnload)}] addedScenes=[{addedList}] removedScenes=[{removedList}] activeScene='{activeSceneLabel}' addedCount={addedScenes.Count} removedCount={removedScenes.Count} reason='{request.Reason}'.",
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

        private static async Task<List<string>> UnloadScenesAsync(IReadOnlyList<string> scenesToUnload, CancellationToken ct)
        {
            List<string> removedScenes = new List<string>();
            HashSet<string> dedupe = new HashSet<string>(System.StringComparer.Ordinal);

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

                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

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
            }

            return removedScenes;
        }

        private static async Task<List<string>> LoadScenesAsync(IReadOnlyList<string> scenesToLoad, CancellationToken ct)
        {
            List<string> addedScenes = new List<string>();
            HashSet<string> dedupe = new HashSet<string>(System.StringComparer.Ordinal);

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
    }
}
