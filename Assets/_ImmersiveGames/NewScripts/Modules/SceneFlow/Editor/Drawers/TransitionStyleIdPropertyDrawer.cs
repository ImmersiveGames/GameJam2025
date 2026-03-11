using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources
{
    /// <summary>
    /// Coleta TransitionStyleId a partir de TransitionStyleCatalogAsset.styles.
    /// </summary>
    internal sealed class TransitionStyleIdSourceProvider : ISceneFlowIdSourceProvider<TransitionStyleId>
    {
        public SceneFlowIdSourceResult Collect()
        {
            var values = new HashSet<string>();
            var duplicates = new HashSet<string>();

            string[] guids = AssetDatabase.FindAssets("t:TransitionStyleCatalogAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<TransitionStyleCatalogAsset>(path);
                if (catalog == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(catalog);
                SerializedProperty styles = serializedObject.FindProperty("styles");
                if (styles == null || !styles.isArray)
                {
                    continue;
                }

                for (int j = 0; j < styles.arraySize; j++)
                {
                    SerializedProperty styleEntry = styles.GetArrayElementAtIndex(j);
                    SerializedProperty styleId = styleEntry.FindPropertyRelative("styleId");
                    SerializedProperty raw = styleId?.FindPropertyRelative("_value");
                    if (raw == null)
                    {
                        continue;
                    }

                    SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, raw.stringValue);
                }
            }

            return SceneFlowIdSourceUtility.BuildResult(values, duplicates);
        }
    }
}

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources
{
    // -----------------------------------------------------------------------------
    // Shared IdSource contracts (Editor-only)
    // Comentario (PT-BR): estes tipos foram removidos durante o cleanup e ainda sao
    // referenciados pelos drawers. Mantemos aqui como "single source of truth" para
    // evitar duplicacao e restaurar compilacao.
    // -----------------------------------------------------------------------------

    internal interface ISceneFlowIdSourceProvider<TId>
    {
        SceneFlowIdSourceResult Collect();
    }

    internal readonly struct SceneFlowIdSourceResult
    {
        public IReadOnlyList<string> Values { get; }
        public IReadOnlyList<string> DuplicateValues { get; }

        public SceneFlowIdSourceResult(IReadOnlyList<string> values, IReadOnlyList<string> duplicateValues)
        {
            Values = values ?? Array.Empty<string>();
            DuplicateValues = duplicateValues ?? Array.Empty<string>();
        }
    }

    internal static class SceneFlowIdSourceUtility
    {
        // Comentario (PT-BR): normalizacao minima para IDs (trim).
        public static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        public static bool AddAndTrackDuplicate(HashSet<string> values, HashSet<string> duplicates, string value)
        {
            string key = Normalize(value);
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (!values.Add(key))
            {
                duplicates.Add(key);
                return false;
            }

            return true;
        }

        public static void AddValue(ICollection<string> values, string value)
        {
            string key = Normalize(value);
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            values.Add(key);
        }

        public static SceneFlowIdSourceResult BuildResult(IEnumerable<string> values, IEnumerable<string> duplicates)
        {
            var v = new List<string>();
            if (values != null)
            {
                foreach (string item in values)
                {
                    string key = Normalize(item);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        v.Add(key);
                    }
                }
            }

            var d = new List<string>();
            if (duplicates != null)
            {
                foreach (string item in duplicates)
                {
                    string key = Normalize(item);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        d.Add(key);
                    }
                }
            }

            v.Sort(StringComparer.Ordinal);
            d.Sort(StringComparer.Ordinal);

            return new SceneFlowIdSourceResult(v, d);
        }
    }
}

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.Drawers
{
    using _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources;

    [CustomPropertyDrawer(typeof(TransitionStyleId))]
    public sealed class TransitionStyleIdPropertyDrawer : PropertyDrawer
    {
        private const string RawValuePropertyName = "_value";
        private const string NoneOptionLabel = "(None)";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty rawValueProperty = property.FindPropertyRelative(RawValuePropertyName);
            if (rawValueProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
            SourceSnapshot snapshot = TransitionStyleOptionsCache.GetOrRefresh();

            float y = position.y;
            Rect popupRect = new(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            y += EditorGUIUtility.singleLineHeight;

            bool hasSourceError = !string.IsNullOrEmpty(snapshot.ErrorMessage);
            bool hasMissingValue = !string.IsNullOrEmpty(currentNormalized) && !Contains(snapshot.Values, currentNormalized);
            bool hasDuplicates = snapshot.DuplicateValues.Count > 0;

            string missingWarningMessage = hasMissingValue
                ? $"TransitionStyleId inexistente: '{currentNormalized}'."
                : string.Empty;

            string duplicateWarningMessage = hasDuplicates
                ? BuildDuplicateWarningMessage(snapshot.DuplicateValues)
                : string.Empty;

            if (hasSourceError)
            {
                y += EditorGUIUtility.standardVerticalSpacing;
                float errorHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(snapshot.ErrorMessage), position.width);
                Rect errorRect = new(position.x, y, position.width, errorHeight);
                y += errorHeight;
                EditorGUI.HelpBox(errorRect, snapshot.ErrorMessage, MessageType.Error);
            }

            if (hasMissingValue)
            {
                y += EditorGUIUtility.standardVerticalSpacing;
                float warningHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(missingWarningMessage), position.width);
                Rect warningRect = new(position.x, y, position.width, warningHeight);
                y += warningHeight;
                EditorGUI.HelpBox(warningRect, missingWarningMessage, MessageType.Warning);
            }

            if (hasDuplicates)
            {
                y += EditorGUIUtility.standardVerticalSpacing;
                float duplicateHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(duplicateWarningMessage), position.width);
                Rect duplicateRect = new(position.x, y, position.width, duplicateHeight);
                EditorGUI.HelpBox(duplicateRect, duplicateWarningMessage, MessageType.Warning);
            }

            BuildPopupOptions(snapshot.Values, currentNormalized, hasMissingValue, out string[] popupOptions, out string[] rawValuesByIndex, out int selectedIndex);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUI.Popup(popupRect, label.text, selectedIndex, popupOptions);
            if (EditorGUI.EndChangeCheck() && nextIndex >= 0 && nextIndex < rawValuesByIndex.Length)
            {
                rawValueProperty.stringValue = rawValuesByIndex[nextIndex];
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty rawValueProperty = property.FindPropertyRelative(RawValuePropertyName);
            if (rawValueProperty == null)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float height = EditorGUIUtility.singleLineHeight;
            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
            SourceSnapshot snapshot = TransitionStyleOptionsCache.GetOrRefresh();

            if (!string.IsNullOrEmpty(snapshot.ErrorMessage))
            {
                float errorHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(snapshot.ErrorMessage), EditorGUIUtility.currentViewWidth);
                height += EditorGUIUtility.standardVerticalSpacing + errorHeight;
            }

            bool hasMissingValue = !string.IsNullOrEmpty(currentNormalized) && !Contains(snapshot.Values, currentNormalized);
            if (hasMissingValue)
            {
                string warningMessage = $"TransitionStyleId inexistente: '{currentNormalized}'.";
                float warningHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(warningMessage), EditorGUIUtility.currentViewWidth);
                height += EditorGUIUtility.standardVerticalSpacing + warningHeight;
            }

            if (snapshot.DuplicateValues.Count > 0)
            {
                string duplicateMessage = BuildDuplicateWarningMessage(snapshot.DuplicateValues);
                float duplicateHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(duplicateMessage), EditorGUIUtility.currentViewWidth);
                height += EditorGUIUtility.standardVerticalSpacing + duplicateHeight;
            }

            return height;
        }

        private static void BuildPopupOptions(
            IReadOnlyList<string> sourceValues,
            string currentNormalized,
            bool hasMissingValue,
            out string[] popupOptions,
            out string[] rawValuesByIndex,
            out int selectedIndex)
        {
            var labels = new List<string>(sourceValues.Count + 2) { NoneOptionLabel };
            var values = new List<string>(sourceValues.Count + 2) { string.Empty };

            for (int i = 0; i < sourceValues.Count; i++)
            {
                string id = sourceValues[i];
                labels.Add(id);
                values.Add(id);
            }

            if (hasMissingValue)
            {
                labels.Add($"MISSING: {currentNormalized}");
                values.Add(currentNormalized);
            }

            selectedIndex = 0;
            if (!string.IsNullOrEmpty(currentNormalized))
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (string.Equals(values[i], currentNormalized, StringComparison.Ordinal))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            popupOptions = labels.ToArray();
            rawValuesByIndex = values.ToArray();
        }

        private static string BuildDuplicateWarningMessage(IReadOnlyList<string> duplicateValues)
        {
            const int maxIdsToDisplay = 5;
            int visibleCount = Mathf.Min(maxIdsToDisplay, duplicateValues.Count);

            var visibleIds = new string[visibleCount];
            for (int i = 0; i < visibleCount; i++)
            {
                visibleIds[i] = duplicateValues[i];
            }

            string suffix = duplicateValues.Count > maxIdsToDisplay
                ? $" (+{duplicateValues.Count - maxIdsToDisplay} more)"
                : string.Empty;

            return $"TransitionStyleId duplicado na fonte: {string.Join(", ", visibleIds)}{suffix}.";
        }

        private static bool Contains(IReadOnlyList<string> values, string expected)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct SourceSnapshot
        {
            public SourceSnapshot(IReadOnlyList<string> values, IReadOnlyList<string> duplicateValues, string errorMessage)
            {
                Values = values;
                DuplicateValues = duplicateValues;
                ErrorMessage = errorMessage;
            }

            public IReadOnlyList<string> Values { get; }
            public IReadOnlyList<string> DuplicateValues { get; }
            public string ErrorMessage { get; }
        }

        private static class TransitionStyleOptionsCache
        {
            private static readonly TransitionStyleIdSourceProvider Provider = new();
            private static readonly string[] EmptyValues = Array.Empty<string>();

            private static bool _isInitialized;
            private static bool _isDirty = true;
            private static SourceSnapshot _snapshot = new(EmptyValues, EmptyValues, string.Empty);

            public static SourceSnapshot GetOrRefresh()
            {
                EnsureInitialized();

                if (!_isDirty)
                {
                    return _snapshot;
                }

                try
                {
                    SceneFlowIdSourceResult source = Provider.Collect();
                    _snapshot = new SourceSnapshot(source.Values, source.DuplicateValues, string.Empty);
                }
                catch (Exception exception)
                {
                    _snapshot = new SourceSnapshot(
                        EmptyValues,
                        EmptyValues,
                        $"Failed to load TransitionStyleId options. Check TransitionStyleCatalog/Style assets. ({exception.GetType().Name})");
                }

                _isDirty = false;
                return _snapshot;
            }

            private static void EnsureInitialized()
            {
                if (_isInitialized)
                {
                    return;
                }

                _isInitialized = true;
                EditorApplication.projectChanged += MarkDirty;
            }

            private static void MarkDirty()
            {
                _isDirty = true;
            }
        }
    }
}
