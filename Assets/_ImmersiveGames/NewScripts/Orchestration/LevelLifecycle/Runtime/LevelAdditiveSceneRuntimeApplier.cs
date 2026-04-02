using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
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


        public static void RecordAppliedLevel(LevelDefinitionAsset targetLevelRef)
        {
            if (targetLevelRef == null)
            {
                HardFailFastH1.Trigger(typeof(LevelAdditiveSceneRuntimeApplier), "RecordAppliedLevel target levelRef is null.");
            }

            targetLevelRef.ValidateOrFailFast("LevelAdditiveState/RecordApplied");
            HashSet<int> targetBuildIndexes = BuildBuildIndexSetOrFail(targetLevelRef.AdditiveScenes, "RecordApplied");
            UpdateActiveState(targetBuildIndexes, targetLevelRef);
        }

        public static void RecordCleared()
        {
            ClearActiveState();
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
                HardFailFastH1.Trigger(typeof(LevelAdditiveSceneRuntimeApplier), $"{sourceLabel} additive scene list is null.");
            }

            for (int i = 0; i < refs.Count; i++)
            {
                SceneBuildIndexRef sceneRef = refs[i];
                if (sceneRef == null)
                {
                    HardFailFastH1.Trigger(typeof(LevelAdditiveSceneRuntimeApplier), $"{sourceLabel} additive scene reference is null at index={i}.");
                }

                if (sceneRef.BuildIndex < 0)
                {
                    HardFailFastH1.Trigger(typeof(LevelAdditiveSceneRuntimeApplier), $"{sourceLabel} additive scene has invalid buildIndex at index={i}. buildIndex='{sceneRef.BuildIndex}' sceneName='{sceneRef.SceneName}'.");
                }

                set.Add(sceneRef.BuildIndex);
            }

            return set;
        }
    }
}
