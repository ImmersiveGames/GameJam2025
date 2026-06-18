using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    [CustomPropertyDrawer(typeof(ActorAttributeUiBindingRequestAuthoringEntry))]
    internal sealed class ActorAttributeUiBindingRequestAuthoringEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect foldoutRect = new(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, BuildLabel(property), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel += 1;
                float y = position.y + lineHeight + spacing;

                DrawChild(property.FindPropertyRelative("requestEnabled"), position.x, ref y, position.width);
                DrawChild(property.FindPropertyRelative("selectorKind"), position.x, ref y, position.width);
                DrawChild(property.FindPropertyRelative("explicitActorId"), position.x, ref y, position.width);
                DrawChild(property.FindPropertyRelative("explicitActorInstanceRuntimeId"), position.x, ref y, position.width);
                DrawChild(property.FindPropertyRelative("attributeDefinition"), position.x, ref y, position.width);
                DrawChild(property.FindPropertyRelative("imageFillSink"), position.x, ref y, position.width);

                EditorGUI.indentLevel -= 1;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return lineHeight;
            }

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float height = lineHeight + spacing;
            height += GetChildHeight(property.FindPropertyRelative("requestEnabled"));
            height += GetChildHeight(property.FindPropertyRelative("selectorKind"));
            height += GetChildHeight(property.FindPropertyRelative("explicitActorId"));
            height += GetChildHeight(property.FindPropertyRelative("explicitActorInstanceRuntimeId"));
            height += GetChildHeight(property.FindPropertyRelative("attributeDefinition"));
            height += GetChildHeight(property.FindPropertyRelative("imageFillSink"));
            return height;
        }

        private static void DrawChild(SerializedProperty property, float x, ref float y, float width)
        {
            if (property == null)
            {
                return;
            }

            float height = EditorGUI.GetPropertyHeight(property, true);
            Rect rect = new(x, y, width, height);
            EditorGUI.PropertyField(rect, property, true);
            y += height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float GetChildHeight(SerializedProperty property)
        {
            if (property == null)
            {
                return 0f;
            }

            return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        private static string BuildLabel(SerializedProperty property)
        {
            var selectorKindProperty = property.FindPropertyRelative("selectorKind");
            var attributeDefinitionProperty = property.FindPropertyRelative("attributeDefinition");
            string selectorKind = selectorKindProperty != null && selectorKindProperty.propertyType == SerializedPropertyType.Enum
                ? selectorKindProperty.enumDisplayNames[selectorKindProperty.enumValueIndex]
                : "Attribute UI Binding Request";

            string attributeId = string.Empty;
            if (attributeDefinitionProperty != null && attributeDefinitionProperty.objectReferenceValue is ActorAttributeDefinitionAsset definition)
            {
                attributeId = definition.ToRuntimeId().ToString();
            }

            if (string.IsNullOrWhiteSpace(attributeId))
            {
                return selectorKind;
            }

            return $"{selectorKind} / {attributeId}";
        }
    }
}
