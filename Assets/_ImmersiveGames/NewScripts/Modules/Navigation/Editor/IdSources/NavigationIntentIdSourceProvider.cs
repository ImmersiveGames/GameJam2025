using System;
using System.Collections.Generic;
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Editor.IdSources
{
    /// <summary>
    /// Coleta NavigationIntentId do catálogo canônico de intents.
    /// </summary>
    public sealed class NavigationIntentIdSourceProvider
    {
        private const string CanonicalCatalogPath = "Assets/Resources/GameNavigationIntentCatalog.asset";

        public NavigationIntentIdSourceResult Collect()
        {
            var values = new HashSet<string>(StringComparer.Ordinal);
            var duplicates = new HashSet<string>(StringComparer.Ordinal);

            var catalog = AssetDatabase.LoadAssetAtPath<GameNavigationIntentCatalogAsset>(CanonicalCatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException($"Canonical catalog not found at '{CanonicalCatalogPath}'.");
            }

            var serializedObject = new SerializedObject(catalog);
            CollectIntentIdsFromBlock(serializedObject, "core", values, duplicates);
            CollectIntentIdsFromBlock(serializedObject, "custom", values, duplicates);

            return BuildResult(values, duplicates);
        }

        private static void CollectIntentIdsFromBlock(
            SerializedObject serializedObject,
            string blockPropertyName,
            HashSet<string> values,
            HashSet<string> duplicates)
        {
            SerializedProperty block = serializedObject.FindProperty(blockPropertyName);
            if (block == null || !block.isArray)
            {
                return;
            }

            for (int i = 0; i < block.arraySize; i++)
            {
                SerializedProperty entry = block.GetArrayElementAtIndex(i);
                SerializedProperty intentId = entry.FindPropertyRelative("intentId");
                SerializedProperty raw = intentId?.FindPropertyRelative("_value");
                if (raw == null)
                {
                    continue;
                }

                string normalized = NavigationIntentId.Normalize(raw.stringValue);
                if (string.IsNullOrEmpty(normalized))
                {
                    continue;
                }

                if (!values.Add(normalized))
                {
                    duplicates.Add(normalized);
                }
            }
        }

        private static NavigationIntentIdSourceResult BuildResult(HashSet<string> values, HashSet<string> duplicates)
        {
            var sortedValues = new List<string>(values);
            sortedValues.Sort(StringComparer.Ordinal);

            var sortedDuplicates = new List<string>(duplicates);
            sortedDuplicates.Sort(StringComparer.Ordinal);

            return new NavigationIntentIdSourceResult(sortedValues, sortedDuplicates);
        }
    }

    public readonly struct NavigationIntentIdSourceResult
    {
        public NavigationIntentIdSourceResult(IReadOnlyList<string> values, IReadOnlyList<string> duplicateValues)
        {
            Values = values;
            DuplicateValues = duplicateValues;
        }

        public IReadOnlyList<string> Values { get; }
        public IReadOnlyList<string> DuplicateValues { get; }
    }
}
