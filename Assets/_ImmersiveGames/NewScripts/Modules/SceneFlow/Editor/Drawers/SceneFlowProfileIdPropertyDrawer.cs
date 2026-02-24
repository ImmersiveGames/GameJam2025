using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(SceneFlowProfileId))]
    public sealed class SceneFlowProfileIdPropertyDrawer : PropertyDrawer
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
            SourceSnapshot snapshot = SceneFlowProfileOptionsCache.GetOrRefresh();

            float y = position.y;
            Rect popupRect = new(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            y += EditorGUIUtility.singleLineHeight;

            bool hasSourceError = !string.IsNullOrEmpty(snapshot.ErrorMessage);
            bool hasMissingValue = !string.IsNullOrEmpty(currentNormalized) && !Contains(snapshot.Values, currentNormalized);
            bool hasDuplicates = snapshot.DuplicateValues.Count > 0;

            string missingWarningMessage = hasMissingValue
                ? $"SceneFlowProfileId inexistente: '{currentNormalized}'."
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
            SourceSnapshot snapshot = SceneFlowProfileOptionsCache.GetOrRefresh();

            if (!string.IsNullOrEmpty(snapshot.ErrorMessage))
            {
                float errorHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(snapshot.ErrorMessage), EditorGUIUtility.currentViewWidth);
                height += EditorGUIUtility.standardVerticalSpacing + errorHeight;
            }

            bool hasMissingValue = !string.IsNullOrEmpty(currentNormalized) && !Contains(snapshot.Values, currentNormalized);
            if (hasMissingValue)
            {
                string warningMessage = $"SceneFlowProfileId inexistente: '{currentNormalized}'.";
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

            return $"SceneFlowProfileId duplicado na fonte: {string.Join(", ", visibleIds)}{suffix}.";
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

        private static class SceneFlowProfileOptionsCache
        {
            private static readonly SceneFlowProfileIdSourceProvider Provider = new();
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
                    // Comentário: falha explícita no Editor, sem quebrar o Inspector.
                    _snapshot = new SourceSnapshot(
                        EmptyValues,
                        EmptyValues,
                        $"Failed to load SceneFlowProfileId options. Check SceneTransitionProfileCatalog/Style assets. ({exception.GetType().Name})");
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

        // Smoke test manual (Editor):
        // 1) Abra um asset/componente que tenha campo SceneFlowProfileId no Inspector.
        // 2) Verifique dropdown com (None) + IDs disponíveis do provider.
        // 3) Com valor inválido serializado, confirme "MISSING: <id>" + HelpBox warning.
    }
}
