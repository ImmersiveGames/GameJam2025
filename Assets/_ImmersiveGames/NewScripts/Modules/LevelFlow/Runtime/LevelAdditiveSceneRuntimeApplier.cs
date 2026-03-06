using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public static class LevelAdditiveSceneRuntimeApplier
    {
        public static async Task<(int added, int removed)> ApplyAsync(
            LevelDefinitionAsset previousLevelRef,
            LevelDefinitionAsset targetLevelRef,
            CancellationToken ct)
        {
            if (targetLevelRef == null)
            {
                FailFast("Target levelRef is null.");
            }

            targetLevelRef.ValidateOrFailFast("LevelAdditiveApply/Target");
            HashSet<int> targetBuildIndexes = BuildTargetBuildIndexSetOrFail(targetLevelRef.AdditiveScenes);
            HashSet<int> candidateUnloadBuildIndexes = new HashSet<int>();

            if (previousLevelRef != null)
            {
                previousLevelRef.ValidateOrFailFast("LevelAdditiveApply/Previous");
                AddBuildIndexes(candidateUnloadBuildIndexes, previousLevelRef.AdditiveScenes);
            }

            List<int> unloadedIndices = new List<int>();
            foreach (int buildIndex in candidateUnloadBuildIndexes)
            {
                ct.ThrowIfCancellationRequested();

                if (targetBuildIndexes.Contains(buildIndex))
                {
                    continue;
                }

                Scene loadedScene = SceneManager.GetSceneByBuildIndex(buildIndex);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    continue;
                }

                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
                if (unloadOperation == null)
                {
                    FailFast($"UnloadSceneAsync returned null for buildIndex='{buildIndex}'.");
                }

                while (!unloadOperation.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                unloadedIndices.Add(buildIndex);
            }

            List<int> loadedIndices = new List<int>();
            foreach (int buildIndex in targetBuildIndexes)
            {
                ct.ThrowIfCancellationRequested();

                Scene loadedScene = SceneManager.GetSceneByBuildIndex(buildIndex);
                if (loadedScene.IsValid() && loadedScene.isLoaded)
                {
                    continue;
                }

                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
                if (loadOperation == null)
                {
                    FailFast($"LoadSceneAsync returned null for buildIndex='{buildIndex}'.");
                }

                while (!loadOperation.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                loadedIndices.Add(buildIndex);
            }

            DebugUtility.Log(typeof(LevelAdditiveSceneRuntimeApplier),
                $"[OBS][LevelFlow] LevelAdditiveApplySummary targetLevelRef='{targetLevelRef.name}' loadedIndices=[{string.Join(",", loadedIndices)}] unloadedIndices=[{string.Join(",", unloadedIndices)}] loadedCount={loadedIndices.Count} unloadedCount={unloadedIndices.Count}.",
                DebugUtility.Colors.Info);

            return (loadedIndices.Count, unloadedIndices.Count);
        }

        private static HashSet<int> BuildTargetBuildIndexSetOrFail(IReadOnlyList<SceneBuildIndexRef> refs)
        {
            HashSet<int> set = new HashSet<int>();
            if (refs == null)
            {
                FailFast("Target additive scene list is null.");
            }

            for (int i = 0; i < refs.Count; i++)
            {
                SceneBuildIndexRef sceneRef = refs[i];
                if (sceneRef == null)
                {
                    FailFast($"Target additive scene reference is null at index={i}.");
                }

                if (sceneRef.BuildIndex < 0)
                {
                    FailFast($"Target additive scene has invalid buildIndex at index={i}. buildIndex='{sceneRef.BuildIndex}' sceneName='{sceneRef.SceneName}'.");
                }

                set.Add(sceneRef.BuildIndex);
            }

            return set;
        }

        private static void AddBuildIndexes(HashSet<int> set, IReadOnlyList<SceneBuildIndexRef> refs)
        {
            if (refs == null)
            {
                return;
            }

            for (int i = 0; i < refs.Count; i++)
            {
                SceneBuildIndexRef sceneRef = refs[i];
                if (sceneRef == null || !sceneRef.IsValid)
                {
                    continue;
                }

                set.Add(sceneRef.BuildIndex);
            }
        }

        private static void FailFast(string detail)
        {
            HardFailFastH1.Trigger(typeof(LevelAdditiveSceneRuntimeApplier),
                $"[FATAL][H1][LevelFlow] Invalid additive scene configuration. detail='{detail}'");
        }
    }
}
