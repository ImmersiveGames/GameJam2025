using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems.Editor
{
    [CustomEditor(typeof(SkinCollectionData))]
    public class SkinCollectionEditor : UnityEditor.Editor
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
                    element.objectReferenceValue = EditorGUILayout.ObjectField(
                        "Skin Config",
                        element.objectReferenceValue,
                        typeof(SkinConfigData),
                        false);

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        _configs.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (skinConfig != null)
                    {
                        EditorGUILayout.LabelField("ModelType", skinConfig.ModelType.ToString());

                        // Se for uma config de áudio (SkinAudioConfigData / ISkinAudioConfig),
                        // não faz sentido exigir prefab. Validamos áudio em vez disso.
                        if (skinConfig is ISkinAudioConfig audioConfig)
                        {
                            var entries = audioConfig.AudioEntries;
                            int count = entries != null ? entries.Count : 0;

                            if (count == 0)
                            {
                                EditorGUILayout.HelpBox(
                                    "Nenhuma entrada de áudio configurada para esta SkinAudioConfigData.",
                                    MessageType.Info);
                            }
                        }
                        else
                        {
                            // Para configs visuais normais, mantemos a validação de prefab.
                            if (skinConfig.GetSelectedPrefabs().Count == 0)
                            {
                                EditorGUILayout.HelpBox(
                                    "⚠️ Nenhum prefab configurado!",
                                    MessageType.Warning);
                            }
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
}
