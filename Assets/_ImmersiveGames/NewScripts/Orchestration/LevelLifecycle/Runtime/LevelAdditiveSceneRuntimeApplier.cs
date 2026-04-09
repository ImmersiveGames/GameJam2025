using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public static class LevelAdditiveSceneRuntimeApplier
    {
        private static readonly object StateSync = new object();
        private static readonly HashSet<int> _activeAppliedBuildIndexes = new HashSet<int>();
        private static PhaseDefinitionAsset _activeAppliedPhaseDefinitionRef;

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

        public static PhaseDefinitionAsset ActiveAppliedPhaseDefinitionRef
        {
            get
            {
                lock (StateSync)
                {
                    return _activeAppliedPhaseDefinitionRef;
                }
            }
        }


        public static void RecordAppliedPhaseDefinition(PhaseDefinitionAsset targetPhaseDefinitionRef)
        {
            if (targetPhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(LevelAdditiveSceneRuntimeApplier), "RecordAppliedPhaseDefinition target phaseDefinitionRef is null.");
            }

            targetPhaseDefinitionRef.ValidateOrFail("LevelAdditiveState/RecordApplied");
            PhaseDefinitionAsset.PhaseSwapBlock swap = targetPhaseDefinitionRef.Swap;
            if (swap == null)
            {
                HardFailFastH1.Trigger(typeof(LevelAdditiveSceneRuntimeApplier), $"RecordAppliedPhaseDefinition phase '{targetPhaseDefinitionRef.name}' has no swap block.");
            }

            HashSet<int> targetBuildIndexes = BuildBuildIndexSetOrFail(swap.additiveScenes, "RecordApplied");
            UpdateActiveState(targetBuildIndexes, targetPhaseDefinitionRef);
        }

        public static void RecordCleared()
        {
            ClearActiveState();
        }

        private static void UpdateActiveState(HashSet<int> buildIndexes, PhaseDefinitionAsset activePhaseDefinitionRef)
        {
            lock (StateSync)
            {
                _activeAppliedBuildIndexes.Clear();
                foreach (int index in buildIndexes)
                {
                    _activeAppliedBuildIndexes.Add(index);
                }

                _activeAppliedPhaseDefinitionRef = activePhaseDefinitionRef;
            }
        }

        private static void ClearActiveState()
        {
            lock (StateSync)
            {
                _activeAppliedBuildIndexes.Clear();
                _activeAppliedPhaseDefinitionRef = null;
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
