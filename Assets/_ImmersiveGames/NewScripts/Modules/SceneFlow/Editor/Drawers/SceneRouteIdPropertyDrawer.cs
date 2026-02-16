using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(SceneRouteId))]
    public sealed class SceneRouteIdPropertyDrawer : PropertyDrawer
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
            SourceSnapshot snapshot = SceneRouteIdOptionsCache.GetOrRefresh();

            float y = position.y;
            Rect popupRect = new(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            y += EditorGUIUtility.singleLineHeight;

            bool hasSourceError = !string.IsNullOrEmpty(snapshot.ErrorMessage);
            bool hasMissingValue = !string.IsNullOrEmpty(currentNormalized) && !Contains(snapshot.Values, currentNormalized);

            string missingWarningMessage = hasMissingValue
                ? $"SceneRouteId inexistente: '{currentNormalized}'."
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
                EditorGUI.HelpBox(warningRect, missingWarningMessage, MessageType.Warning);
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
            SourceSnapshot snapshot = SceneRouteIdOptionsCache.GetOrRefresh();

            if (!string.IsNullOrEmpty(snapshot.ErrorMessage))
            {
                string errorMessage = snapshot.ErrorMessage;
                float errorHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(errorMessage), EditorGUIUtility.currentViewWidth);
                height += EditorGUIUtility.standardVerticalSpacing + errorHeight;
            }

            bool hasMissingValue = !string.IsNullOrEmpty(currentNormalized) && !Contains(snapshot.Values, currentNormalized);
            if (hasMissingValue)
            {
                string warningMessage = $"SceneRouteId inexistente: '{currentNormalized}'.";
                float warningHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(warningMessage), EditorGUIUtility.currentViewWidth);
                height += EditorGUIUtility.standardVerticalSpacing + warningHeight;
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
            public SourceSnapshot(IReadOnlyList<string> values, string errorMessage)
            {
                Values = values;
                ErrorMessage = errorMessage;
            }

            public IReadOnlyList<string> Values { get; }
            public string ErrorMessage { get; }
        }

        private static class SceneRouteIdOptionsCache
        {
            private static readonly SceneRouteIdSourceProvider Provider = new();
            private static readonly string[] EmptyValues = Array.Empty<string>();

            private static bool _isInitialized;
            private static bool _isDirty = true;
            private static SourceSnapshot _snapshot = new(EmptyValues, "");

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
                    _snapshot = new SourceSnapshot(source.Values, string.Empty);
                }
                catch (Exception exception)
                {
                    // Comentário: falha explícita no Editor, sem quebrar o Inspector.
                    _snapshot = new SourceSnapshot(
                        EmptyValues,
                        $"Failed to load SceneRouteId options. Check SceneRouteCatalog/Route assets. ({exception.GetType().Name})");
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
        // 1) Selecione um MonoBehaviour/ScriptableObject com campo SceneRouteId serializado.
        // 2) Confirme que o campo aparece como dropdown com opção (None) + IDs do catálogo.
        // 3) Force um valor inválido no YAML e reabra o Inspector para validar "MISSING: <id>" + HelpBox warning.
    }
}
