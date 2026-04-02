using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config
{
    [CreateAssetMenu(
        fileName = "LevelCollectionAsset",
        menuName = "ImmersiveGames/NewScripts/Game/Content/Definitions/Levels/LevelCollectionAsset",
        order = 31)]
    public sealed class LevelCollectionAsset : ScriptableObject
    {
        [SerializeField] private List<LevelDefinitionAsset> levels = new();
        [SerializeField] private bool enforceIndex0AsDefault = true;

        public IReadOnlyList<LevelDefinitionAsset> Levels => levels;
        public bool EnforceIndex0AsDefault => enforceIndex0AsDefault;

        public LevelDefinitionAsset GetDefaultOrNull()
        {
            if (levels == null || levels.Count == 0)
            {
                return null;
            }

            return levels[0];
        }

        public bool Contains(LevelDefinitionAsset levelRef)
        {
            if (levelRef == null || levels == null)
            {
                return false;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                if (ReferenceEquals(levels[i], levelRef))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryValidateRuntime(out string error)
        {
            error = string.Empty;

            if (levels == null)
            {
                error = "Levels list is null.";
                return false;
            }

            if (levels.Count == 0)
            {
                error = "Levels list is empty.";
                return false;
            }

            HashSet<LevelDefinitionAsset> seenRefs = new HashSet<LevelDefinitionAsset>();
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinitionAsset level = levels[i];
                if (level == null)
                {
                    error = $"Null level entry at index={i}.";
                    return false;
                }

                if (!seenRefs.Add(level))
                {
                    error = $"Duplicate level reference '{level.name}' at index={i}.";
                    return false;
                }

                if (!level.TryValidateRuntime(out string levelError))
                {
                    error = $"Level '{level.name}' invalid. detail='{levelError}'";
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

            HardFailFastH1.Trigger(typeof(LevelCollectionAsset),
                $"[FATAL][H1][LevelFlow] Invalid LevelCollectionAsset '{name}'. context='{context}' detail='{error}'");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (levels == null)
            {
                levels = new List<LevelDefinitionAsset>();
                return;
            }

            HashSet<LevelDefinitionAsset> seenRefs = new HashSet<LevelDefinitionAsset>();
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinitionAsset level = levels[i];
                if (level == null)
                {
                    DebugUtility.LogWarning<LevelCollectionAsset>(
                        $"[WARN][LevelFlow][Config] LevelCollectionAsset '{name}' has null entry at index={i}.");
                    continue;
                }

                if (!seenRefs.Add(level))
                {
                    DebugUtility.LogWarning<LevelCollectionAsset>(
                        $"[WARN][LevelFlow][Config] LevelCollectionAsset '{name}' has duplicate reference to level '{level.name}'.");
                }

                if (!level.TryValidateRuntime(out string levelError))
                {
                    DebugUtility.LogWarning<LevelCollectionAsset>(
                        $"[WARN][LevelFlow][Config] LevelCollectionAsset '{name}' has invalid level '{level.name}'. detail='{levelError}'.");
                }
            }
        }
#endif
    }
}
