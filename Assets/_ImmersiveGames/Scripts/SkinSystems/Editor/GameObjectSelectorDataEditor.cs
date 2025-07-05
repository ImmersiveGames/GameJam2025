using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Editor
{
    [CustomEditor(typeof(GameObjectSelectorData))]
    public class GameObjectSelectorDataEditor : UnityEditor.Editor
    {
        private ReorderableList _list;

        private SerializedProperty _prefabsProp;
        private SerializedProperty _activationMaskProp;
        private SerializedProperty _tagsProp;

        private void OnEnable()
        {
            _prefabsProp = serializedObject.FindProperty("prefabs");
            _activationMaskProp = serializedObject.FindProperty("activationMask");
            _tagsProp = serializedObject.FindProperty("prefabTags");

            _list = new ReorderableList(serializedObject, _prefabsProp, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    const float toggleWidth = 20f;
                    const float tagWidth = 100f;
                    const float spacing = 5f;

                    var toggleRect = new Rect(rect.x, rect.y, toggleWidth, EditorGUIUtility.singleLineHeight);
                    var prefabRect = new Rect(rect.x + toggleWidth + spacing, rect.y,
                        rect.width - toggleWidth - tagWidth - spacing * 2, EditorGUIUtility.singleLineHeight);
                    var tagRect = new Rect(prefabRect.xMax + spacing, rect.y, tagWidth, EditorGUIUtility.singleLineHeight);

                    EditorGUI.LabelField(toggleRect, " ");
                    EditorGUI.LabelField(prefabRect, "Prefab");
                    EditorGUI.LabelField(tagRect, "Tag");
                },
                drawElementCallback = (rect, index, _, _) =>
                {
                    rect.y += 2;
                    const float toggleWidth = 20f;
                    const float tagWidth = 100f;
                    const float spacing = 5f;

                    var toggleRect = new Rect(rect.x, rect.y, toggleWidth, EditorGUIUtility.singleLineHeight);
                    var prefabRect = new Rect(rect.x + toggleWidth + spacing, rect.y,
                        rect.width - toggleWidth - tagWidth - spacing * 2, EditorGUIUtility.singleLineHeight);
                    var tagRect = new Rect(prefabRect.xMax + spacing, rect.y, tagWidth, EditorGUIUtility.singleLineHeight);

                    EnsureArraySize(_activationMaskProp, index, true);
                    EnsureArraySize(_tagsProp, index, string.Empty);

                    var prefabProp = _prefabsProp.GetArrayElementAtIndex(index);
                    var activeProp = _activationMaskProp.GetArrayElementAtIndex(index);
                    var tagProp = _tagsProp.GetArrayElementAtIndex(index);

                    activeProp.boolValue = EditorGUI.Toggle(toggleRect, activeProp.boolValue);
                    EditorGUI.PropertyField(prefabRect, prefabProp, GUIContent.none);
                    EditorGUI.PropertyField(tagRect, tagProp, GUIContent.none);
                },
                onAddCallback = _ =>
                {
                    _prefabsProp.arraySize++;
                    _activationMaskProp.arraySize++;
                    _tagsProp.arraySize++;

                    _activationMaskProp.GetArrayElementAtIndex(_activationMaskProp.arraySize - 1).boolValue = true;
                    _tagsProp.GetArrayElementAtIndex(_tagsProp.arraySize - 1).stringValue = "";
                },
                onRemoveCallback = list =>
                {
                    int index = list.index;
                    _prefabsProp.DeleteArrayElementAtIndex(index);
                    _activationMaskProp.DeleteArrayElementAtIndex(index);
                    _tagsProp.DeleteArrayElementAtIndex(index);
                }
            };

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Exibir tudo menos as listas customizadas
            DrawPropertiesExcluding(serializedObject, "prefabs", "activationMask", "prefabTags");

            EditorGUILayout.Space(10);
            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void EnsureArraySize(SerializedProperty array, int index, object defaultValue)
        {
            while (array.arraySize <= index)
            {
                array.InsertArrayElementAtIndex(array.arraySize);
            }

            switch (defaultValue)
            {
                case bool b when array.GetArrayElementAtIndex(index).propertyType == SerializedPropertyType.Boolean:
                    array.GetArrayElementAtIndex(index).boolValue = b;
                    break;
                case string s when array.GetArrayElementAtIndex(index).propertyType == SerializedPropertyType.String:
                    array.GetArrayElementAtIndex(index).stringValue = s;
                    break;
            }

        }
    }
}
