using System;
using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdDrawers
{
    /// <summary>
    /// Drawer base para IDs tipados com dropdown + sinalização de inconsistência.
    /// </summary>
    public abstract class SceneFlowTypedIdDrawerBase : PropertyDrawer
    {
        private const string RawValuePropertyName = "_value";

        protected abstract SceneFlowIdSourceResult CollectSource();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty rawValueProperty = property.FindPropertyRelative(RawValuePropertyName);
            if (rawValueProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            SceneFlowIdSourceResult source = CollectSource();
            bool allowEmpty = IsEmptyAllowed();

            string currentRaw = rawValueProperty.stringValue;
            string currentNormalized = SceneFlowIdSourceUtility.Normalize(currentRaw);

            bool isMissing = string.IsNullOrEmpty(currentNormalized)
                ? !allowEmpty
                : !Contains(source.Values, currentNormalized);

            var options = BuildOptions(source.Values, allowEmpty, currentNormalized, isMissing, out int currentIndex);

            Rect lineRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect invalidRect = new(position.x, lineRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight * 2f);
            Rect duplicateRect = new(position.x, invalidRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight * 2f);

            Color previousColor = GUI.color;
            if (isMissing)
            {
                GUI.color = new Color(1f, 0.9f, 0.9f);
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUI.Popup(lineRect, label.text, currentIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                string selectedValue = options[selected];
                if (selectedValue.StartsWith("<missing:", StringComparison.Ordinal))
                {
                    selectedValue = currentNormalized;
                }

                rawValueProperty.stringValue = selectedValue == "<none>" ? string.Empty : selectedValue;
            }
            EditorGUI.EndProperty();

            GUI.color = previousColor;

            if (isMissing)
            {
                string currentLabel = string.IsNullOrEmpty(currentNormalized) ? "<empty>" : currentNormalized;
                string message =
                    $"ID atual '{currentLabel}' não foi encontrado nas fontes conhecidas. " +
                    "Verifique catálogo/asset de origem ou ajuste o valor pelo dropdown.";
                EditorGUI.HelpBox(invalidRect, message, MessageType.Error);
            }

            if (source.DuplicateValues.Count > 0)
            {
                string duplicates = string.Join(", ", source.DuplicateValues);
                string message = $"IDs duplicados detectados na fonte: {duplicates}.";
                EditorGUI.HelpBox(duplicateRect, message, MessageType.Warning);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty rawValueProperty = property.FindPropertyRelative(RawValuePropertyName);
            if (rawValueProperty == null)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            SceneFlowIdSourceResult source = CollectSource();
            bool allowEmpty = IsEmptyAllowed();
            string currentNormalized = SceneFlowIdSourceUtility.Normalize(rawValueProperty.stringValue);
            bool isMissing = string.IsNullOrEmpty(currentNormalized)
                ? !allowEmpty
                : !Contains(source.Values, currentNormalized);

            float height = EditorGUIUtility.singleLineHeight;
            if (isMissing)
            {
                height += EditorGUIUtility.standardVerticalSpacing + (EditorGUIUtility.singleLineHeight * 2f);
            }

            if (source.DuplicateValues.Count > 0)
            {
                height += EditorGUIUtility.standardVerticalSpacing + (EditorGUIUtility.singleLineHeight * 2f);
            }

            return height;
        }

        private bool IsEmptyAllowed()
        {
            if (fieldInfo == null)
            {
                return false;
            }

            return fieldInfo.GetCustomAttribute<SceneFlowAllowEmptyIdAttribute>() != null;
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] BuildOptions(
            IReadOnlyList<string> sourceValues,
            bool allowEmpty,
            string currentNormalized,
            bool isMissing,
            out int currentIndex)
        {
            var options = new List<string>(sourceValues.Count + 2);

            if (allowEmpty)
            {
                options.Add("<none>");
            }

            for (int i = 0; i < sourceValues.Count; i++)
            {
                options.Add(sourceValues[i]);
            }

            if (isMissing)
            {
                string missingLabel = string.IsNullOrEmpty(currentNormalized)
                    ? "<missing:<empty>>"
                    : $"<missing:{currentNormalized}>";
                options.Add(missingLabel);
            }

            currentIndex = 0;
            if (allowEmpty && string.IsNullOrEmpty(currentNormalized))
            {
                currentIndex = 0;
                return options.ToArray();
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i], currentNormalized, StringComparison.Ordinal))
                {
                    currentIndex = i;
                    return options.ToArray();
                }
            }

            if (isMissing)
            {
                currentIndex = options.Count - 1;
            }

            return options.ToArray();
        }
    }
}
