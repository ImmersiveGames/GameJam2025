using System;
using System.Collections.Generic;

namespace ImmersiveGames.GameJam2025.Modules.SceneFlow.Editor.IdSources
{
    internal interface ISceneFlowIdSourceProvider<TId>
    {
        SceneFlowIdSourceResult Collect();
    }

    internal readonly struct SceneFlowIdSourceResult
    {
        public SceneFlowIdSourceResult(IReadOnlyList<string> values, IReadOnlyList<string> duplicateValues)
        {
            Values = values ?? Array.Empty<string>();
            DuplicateValues = duplicateValues ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Values { get; }
        public IReadOnlyList<string> DuplicateValues { get; }
    }

    internal static class SceneFlowIdSourceUtility
    {
        public static string Normalize(string value) => (value ?? string.Empty).Trim();

        public static bool AddAndTrackDuplicate(HashSet<string> values, HashSet<string> duplicates, string value)
        {
            string key = Normalize(value);
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (!values.Add(key))
            {
                duplicates.Add(key);
                return false;
            }

            return true;
        }

        public static SceneFlowIdSourceResult BuildResult(IEnumerable<string> values, IEnumerable<string> duplicates)
        {
            var v = new List<string>();
            foreach (string item in values ?? Array.Empty<string>())
            {
                string key = Normalize(item);
                if (!string.IsNullOrWhiteSpace(key)) v.Add(key);
            }

            var d = new List<string>();
            foreach (string item in duplicates ?? Array.Empty<string>())
            {
                string key = Normalize(item);
                if (!string.IsNullOrWhiteSpace(key)) d.Add(key);
            }

            v.Sort(StringComparer.Ordinal);
            d.Sort(StringComparer.Ordinal);
            return new SceneFlowIdSourceResult(v, d);
        }
    }
}

