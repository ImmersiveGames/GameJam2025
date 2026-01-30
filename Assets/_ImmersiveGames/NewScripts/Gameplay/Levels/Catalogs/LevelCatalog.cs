using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Definitions;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs
{
    /// <summary>
    /// Catálogo configurável de níveis: ordem, inicial e overrides explícitos.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelCatalog",
        menuName = "ImmersiveGames/Levels/Level Catalog",
        order = 1)]
    public class LevelCatalog : ScriptableObject
    {
        [SerializeField] private string initialLevelId = string.Empty;
        [SerializeField] private List<string> orderedLevels = new();
        [SerializeField] private List<LevelDefinition> definitions = new();
        [SerializeField] private List<NextLevelOverride> nextById = new();

        public string InitialLevelId => Normalize(initialLevelId);

        public IReadOnlyList<string> OrderedLevels => orderedLevels;
        public IReadOnlyList<LevelDefinition> Definitions => definitions;
        public IReadOnlyList<NextLevelOverride> NextById => nextById;

        public bool TryGetDefinition(string levelId, out LevelDefinition definition)
        {
            definition = null;
            var normalized = Normalize(levelId);
            if (normalized.Length == 0)
            {
                return false;
            }

            foreach (var def in definitions)
            {
                if (def == null)
                {
                    continue;
                }

                if (string.Equals(def.LevelId, normalized, StringComparison.Ordinal))
                {
                    definition = def;
                    return true;
                }
            }

            return false;
        }

        public bool TryResolveNextLevelId(string levelId, out string nextLevelId)
        {
            nextLevelId = string.Empty;
            var normalized = Normalize(levelId);
            if (normalized.Length == 0)
            {
                return false;
            }

            if (TryResolveOverride(normalized, out nextLevelId))
            {
                return nextLevelId.Length > 0;
            }

            if (TryResolveOrdered(orderedLevels, normalized, out nextLevelId))
            {
                return true;
            }

            var fallbackOrdered = ExtractDefinitionOrder();
            if (TryResolveOrdered(fallbackOrdered, normalized, out nextLevelId))
            {
                return true;
            }

            nextLevelId = string.Empty;
            return false;
        }

        public bool TryResolveInitialLevelId(out string resolvedLevelId)
        {
            resolvedLevelId = Normalize(initialLevelId);
            if (resolvedLevelId.Length > 0)
            {
                return true;
            }

            if (orderedLevels.Count > 0)
            {
                resolvedLevelId = Normalize(orderedLevels[0]);
                return resolvedLevelId.Length > 0;
            }

            foreach (var def in definitions)
            {
                if (def == null)
                {
                    continue;
                }

                var candidate = Normalize(def.LevelId);
                if (candidate.Length > 0)
                {
                    resolvedLevelId = candidate;
                    return true;
                }
            }

            resolvedLevelId = string.Empty;
            return false;
        }

        private bool TryResolveOverride(string normalizedLevelId, out string nextLevelId)
        {
            nextLevelId = string.Empty;
            foreach (var entry in nextById)
            {
                if (entry == null)
                {
                    continue;
                }

                if (!string.Equals(Normalize(entry.CurrentLevelId), normalizedLevelId, StringComparison.Ordinal))
                {
                    continue;
                }

                nextLevelId = Normalize(entry.NextLevelId);
                return true;
            }

            return false;
        }

        private static bool TryResolveOrdered(IReadOnlyList<string> list, string normalizedLevelId, out string nextLevelId)
        {
            nextLevelId = string.Empty;
            if (list == null || list.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < list.Count; index++)
            {
                var entry = Normalize(list[index]);
                if (!string.Equals(entry, normalizedLevelId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (index + 1 >= list.Count)
                {
                    return false;
                }

                nextLevelId = Normalize(list[index + 1]);
                return nextLevelId.Length > 0;
            }

            return false;
        }

        private List<string> ExtractDefinitionOrder()
        {
            var list = new List<string>();
            foreach (var def in definitions)
            {
                if (def == null)
                {
                    continue;
                }

                var id = Normalize(def.LevelId);
                if (id.Length > 0)
                {
                    list.Add(id);
                }
            }

            return list;
        }

        protected static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        [Serializable]
        public sealed class NextLevelOverride
        {
            [SerializeField] private string currentLevelId;
            [SerializeField] private string nextLevelId;

            public string CurrentLevelId => currentLevelId;
            public string NextLevelId => nextLevelId;
        }
    }
}
