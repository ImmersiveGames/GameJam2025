using UnityEditor;
using UnityEngine;
using _ImmersiveGames.Scripts.SkinSystems.Data;

[CustomEditor(typeof(SkinCollectionData))]
public class SkinCollectionEditor : Editor
{
    private SerializedProperty _configs;

    private void OnEnable()
    {
        _configs = serializedObject.FindProperty("configs");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Skin Collection", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        for (int i = 0; i < _configs.arraySize; i++)
        {
            var element = _configs.GetArrayElementAtIndex(i);
            var skinConfig = element.objectReferenceValue as SkinConfigData;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.BeginHorizontal();
                element.objectReferenceValue = EditorGUILayout.ObjectField("Skin Config", element.objectReferenceValue, typeof(SkinConfigData), false);

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _configs.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (skinConfig != null)
                {
                    EditorGUILayout.LabelField("ModelType", skinConfig.ModelType.ToString());
                    if (skinConfig.GetSelectedPrefabs().Count == 0)
                    {
                        EditorGUILayout.HelpBox("⚠️ Nenhum prefab configurado!", MessageType.Warning);
                    }
                }
            }
        }

        if (GUILayout.Button("Adicionar Novo SkinConfig"))
        {
            _configs.InsertArrayElementAtIndex(_configs.arraySize);
        }

        serializedObject.ApplyModifiedProperties();
    }
}