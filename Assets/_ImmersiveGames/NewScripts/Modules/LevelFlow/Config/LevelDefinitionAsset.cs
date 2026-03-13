using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Config
{
    [CreateAssetMenu(
        fileName = "LevelDefinitionAsset",
        menuName = "ImmersiveGames/NewScripts/LevelFlow/Level Definition",
        order = 30)]
    public sealed class LevelDefinitionAsset : ScriptableObject
    {
        [SerializeField] private List<SceneBuildIndexRef> additiveScenes = new();

        [Header("Level Flags")]
        [SerializeField] private bool hasIntroStage = true;
        [FormerlySerializedAs("hasPostLevelStage")]
        [SerializeField] private bool hasPostGameReactionHook = true;
        [SerializeField] private bool allowLocalCurtainIn = true;
        [SerializeField] private bool allowLocalCurtainOut = true;

        public IReadOnlyList<SceneBuildIndexRef> AdditiveScenes => additiveScenes;
        public bool HasIntroStage => hasIntroStage;
        public bool HasPostGameReactionHook => hasPostGameReactionHook;
        public bool HasPostLevelStage => hasPostGameReactionHook;
        public bool AllowLocalCurtainIn => allowLocalCurtainIn;
        public bool AllowLocalCurtainOut => allowLocalCurtainOut;

        public bool TryValidateRuntime(out string error)
        {
            error = string.Empty;

            if (additiveScenes == null)
            {
                error = "Additive scenes list is null.";
                return false;
            }

            if (additiveScenes.Count == 0)
            {
                error = "Additive scenes list is empty.";
                return false;
            }

            HashSet<int> seenBuildIndexes = new HashSet<int>();
            for (int i = 0; i < additiveScenes.Count; i++)
            {
                SceneBuildIndexRef sceneRef = additiveScenes[i];
                if (sceneRef == null)
                {
                    error = $"Additive scene ref is null at index={i}.";
                    return false;
                }

                if (sceneRef.BuildIndex < 0)
                {
                    error = $"Additive scene buildIndex invalid at index={i}. buildIndex='{sceneRef.BuildIndex}'.";
                    return false;
                }

                if (!seenBuildIndexes.Add(sceneRef.BuildIndex))
                {
                    error = $"Duplicate additive scene buildIndex='{sceneRef.BuildIndex}'.";
                    return false;
                }
            }

            return true;
        }

        public void ValidateOrFailFast(string context)
        {
            if (TryValidateRuntime(out string error))
            {
                return;
            }

            HardFailFastH1.Trigger(typeof(LevelDefinitionAsset),
                $"[FATAL][H1][LevelFlow] Invalid LevelDefinitionAsset '{name}'. context='{context}' detail='{error}'");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            HashSet<int> seenBuildIndexes = new HashSet<int>();

            if (additiveScenes == null)
            {
                additiveScenes = new List<SceneBuildIndexRef>();
                return;
            }

            for (int i = additiveScenes.Count - 1; i >= 0; i--)
            {
                SceneBuildIndexRef sceneRef = additiveScenes[i];
                if (sceneRef == null)
                {
                    DebugUtility.LogWarning<LevelDefinitionAsset>(
                        $"[WARN][LevelFlow][Config] LevelDefinitionAsset '{name}' has null scene ref at index={i}. Removing entry.");
                    additiveScenes.RemoveAt(i);
                    continue;
                }

                sceneRef.SyncFromEditorAsset();
                if (sceneRef.BuildIndex < 0)
                {
                    DebugUtility.LogWarning<LevelDefinitionAsset>(
                        $"[WARN][LevelFlow][Config] LevelDefinitionAsset '{name}' has invalid buildIndex at index={i}.");
                    continue;
                }

                if (!seenBuildIndexes.Add(sceneRef.BuildIndex))
                {
                    DebugUtility.LogWarning<LevelDefinitionAsset>(
                        $"[WARN][LevelFlow][Config] LevelDefinitionAsset '{name}' has duplicate additive scene buildIndex='{sceneRef.BuildIndex}'.");
                }
            }
        }
#endif
    }
}
