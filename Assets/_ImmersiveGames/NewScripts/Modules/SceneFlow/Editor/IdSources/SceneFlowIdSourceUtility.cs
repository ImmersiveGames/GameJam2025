using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources
{
    internal static class SceneFlowIdSourceUtility
    {
        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        public static SceneFlowIdSourceResult BuildResult(HashSet<string> values, HashSet<string> duplicates)
        {
            var sortedValues = new List<string>(values);
            sortedValues.Sort(StringComparer.Ordinal);

            var sortedDuplicates = new List<string>(duplicates);
            sortedDuplicates.Sort(StringComparer.Ordinal);

            return new SceneFlowIdSourceResult(sortedValues, sortedDuplicates);
        }

        // Comentário: uso quando sobreposição entre fontes é esperada e não deve virar falso positivo.
        public static bool AddValue(HashSet<string> values, string rawValue)
        {
            string normalized = Normalize(rawValue);
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            return values.Add(normalized);
        }

        // Comentário: uso quando duplicidade na MESMA fonte deve virar sinalização de configuração.
        public static bool AddAndTrackDuplicate(HashSet<string> values, HashSet<string> duplicates, string rawValue)
        {
            string normalized = Normalize(rawValue);
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            if (!values.Add(normalized))
            {
                duplicates.Add(normalized);
                return false;
            }

            return true;
        }
    }
}
