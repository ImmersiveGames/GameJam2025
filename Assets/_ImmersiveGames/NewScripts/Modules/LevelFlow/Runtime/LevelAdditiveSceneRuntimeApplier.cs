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
        private static readonly object StateSync = new object();
        private static readonly HashSet<int> _activeAppliedBuildIndexes = new HashSet<int>();
        private static LevelDefinitionAsset _activeAppliedLevelRef;

        public static bool HasActiveAppliedLevelContent
        {
            get
            {
                lock (StateSync)
                {
                    return _activeAppliedBuildIndexes.Count > 0;
                }
            }
        }

        public static int ActiveAppliedSceneCount
        {
            get
            {
                lock (StateSync)
                {
                    return _activeAppliedBuildIndexes.Count;
                }
            }
        }

        public static LevelDefinitionAsset ActiveAppliedLevelRef
        {
            get
            {
                lock (StateSync)
                {
                    return _activeAppliedLevelRef;
                }
            }
        }

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
            HashSet<int> targetBuildIndexes = BuildBuildIndexSetOrFail(targetLevelRef.AdditiveScenes, "Target");

            bool forceReload = previousLevelRef != null && ReferenceEquals(previousLevelRef, targetLevelRef);
            List<int> loadedIndices;
            List<int> unloadedIndices;

            if (forceReload)
            {
                (loadedIndices, unloadedIndices) = await ReloadSameLevelAsync(targetBuildIndexes, ct);
            }
            else
            {
                HashSet<int> candidateUnloadBuildIndexes;
                if (previousLevelRef != null)
                {
                    previousLevelRef.ValidateOrFailFast("LevelAdditiveApply/Previous");
                    candidateUnloadBuildIndexes = BuildBuildIndexSetOrFail(previousLevelRef.AdditiveScenes, "Previous");
                }
                else
                {
                    candidateUnloadBuildIndexes = GetActiveAppliedBuildIndexesSnapshot();
                }

                unloadedIndices = await UnloadIndicesAsync(candidateUnloadBuildIndexes, targetBuildIndexes, ct);
                loadedIndices = await LoadIndicesAsync(targetBuildIndexes, ct);
            }

            UpdateActiveState(targetBuildIndexes, targetLevelRef);

            DebugUtility.Log(typeof(LevelAdditiveSceneRuntimeApplier),
                $"[OBS][LevelFlow] LevelAdditiveApplySummary targetLevelRef='{targetLevelRef.name}' loadedIndices=[{string.Join(",", loadedIndices)}] unloadedIndices=[{string.Join(",", unloadedIndices)}] loadedCount={loadedIndices.Count} unloadedCount={unloadedIndices.Count} activeCount={ActiveAppliedSceneCount}.",
                DebugUtility.Colors.Info);

            return (loadedIndices.Count, unloadedIndices.Count);
        }

        public static async Task<int> ClearAsync(LevelDefinitionAsset previousLevelRef, CancellationToken ct)
        {
            HashSet<int> previousBuildIndexes;
            string previousLabel;
            if (previousLevelRef != null)
            {
                previousLevelRef.ValidateOrFailFast("LevelAdditiveClear/Previous");
                previousBuildIndexes = BuildBuildIndexSetOrFail(previousLevelRef.AdditiveScenes, "Previous");
                previousLabel = previousLevelRef.name;
            }
            else
            {
                previousBuildIndexes = GetActiveAppliedBuildIndexesSnapshot();
                LevelDefinitionAsset activeLevelRef = ActiveAppliedLevelRef;
                previousLabel = activeLevelRef != null ? activeLevelRef.name : "<none>";
            }

            HashSet<int> emptyTarget = new HashSet<int>();
            List<int> unloadedIndices = await UnloadIndicesAsync(previousBuildIndexes, emptyTarget, ct);

            ClearActiveState();

            DebugUtility.Log(typeof(LevelAdditiveSceneRuntimeApplier),
                $"[OBS][LevelFlow] LevelAdditiveClearSummary previousLevelRef='{previousLabel}' unloadedIndices=[{string.Join(",", unloadedIndices)}] unloadedCount={unloadedIndices.Count} activeCount={ActiveAppliedSceneCount}.",
                DebugUtility.Colors.Info);

            return unloadedIndices.Count;
        }

        private static async Task<(List<int> loadedIndices, List<int> unloadedIndices)> ReloadSameLevelAsync(HashSet<int> levelBuildIndexes, CancellationToken ct)
        {
            List<int> unloadedIndices = new List<int>();
            List<int> loadedIndices = new List<int>();

            // Comentario: quando previous==target, forcamos unload->load de todo o set aditivo do level.
            foreach (int buildIndex in levelBuildIndexes)
            {
                ct.ThrowIfCancellationRequested();

                Scene loadedScene = SceneManager.GetSceneByBuildIndex(buildIndex);
                if (loadedScene.IsValid() && loadedScene.isLoaded)
                {
                    AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
                    if (unloadOperation == null)
                    {
                        FailFast($"UnloadSceneAsync returned null for buildIndex='{buildIndex}' during reload.");
                    }

                    while (!unloadOperation.isDone)
                    {
                        ct.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }
                }

                unloadedIndices.Add(buildIndex);
            }

            foreach (int buildIndex in levelBuildIndexes)
            {
                ct.ThrowIfCancellationRequested();

                Scene loadedScene = SceneManager.GetSceneByBuildIndex(buildIndex);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    AsyncOperation loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
                    if (loadOperation == null)
                    {
                        FailFast($"LoadSceneAsync returned null for buildIndex='{buildIndex}' during reload.");
                    }

                    while (!loadOperation.isDone)
                    {
                        ct.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }
                }

                loadedIndices.Add(buildIndex);
            }

            return (loadedIndices, unloadedIndices);
        }

        private static async Task<List<int>> UnloadIndicesAsync(HashSet<int> candidateUnloadBuildIndexes, HashSet<int> keepLoadedSet, CancellationToken ct)
        {
            List<int> unloadedIndices = new List<int>();
            foreach (int buildIndex in candidateUnloadBuildIndexes)
            {
                ct.ThrowIfCancellationRequested();

                if (keepLoadedSet.Contains(buildIndex))
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

            return unloadedIndices;
        }

        private static async Task<List<int>> LoadIndicesAsync(HashSet<int> targetBuildIndexes, CancellationToken ct)
        {
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

            return loadedIndices;
        }

        private static HashSet<int> GetActiveAppliedBuildIndexesSnapshot()
        {
            lock (StateSync)
            {
                return new HashSet<int>(_activeAppliedBuildIndexes);
            }
        }

        private static void UpdateActiveState(HashSet<int> buildIndexes, LevelDefinitionAsset activeLevelRef)
        {
            lock (StateSync)
            {
                _activeAppliedBuildIndexes.Clear();
                foreach (int index in buildIndexes)
                {
                    _activeAppliedBuildIndexes.Add(index);
                }

                _activeAppliedLevelRef = activeLevelRef;
            }
        }

        private static void ClearActiveState()
        {
            lock (StateSync)
            {
                _activeAppliedBuildIndexes.Clear();
                _activeAppliedLevelRef = null;
            }
        }

        private static HashSet<int> BuildBuildIndexSetOrFail(IReadOnlyList<SceneBuildIndexRef> refs, string sourceLabel)
        {
            HashSet<int> set = new HashSet<int>();
            if (refs == null)
            {
                FailFast($"{sourceLabel} additive scene list is null.");
            }

            for (int i = 0; i < refs.Count; i++)
            {
                SceneBuildIndexRef sceneRef = refs[i];
                if (sceneRef == null)
                {
                    FailFast($"{sourceLabel} additive scene reference is null at index={i}.");
                }

                if (sceneRef.BuildIndex < 0)
                {
                    FailFast($"{sourceLabel} additive scene has invalid buildIndex at index={i}. buildIndex='{sceneRef.BuildIndex}' sceneName='{sceneRef.SceneName}'.");
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
