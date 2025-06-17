using UnityEngine;
using UnityEditor;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems.Editor
{
    [CustomEditor(typeof(TriggerData))]
    public class TriggerDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var triggerTypeProperty = serializedObject.FindProperty("triggerType");
            var propertiesProperty = serializedObject.FindProperty("properties");

            EditorGUILayout.PropertyField(triggerTypeProperty);

            if (SpawnPropertyMap.TriggerPropertiesMap.TryGetValue((TriggerType)triggerTypeProperty.enumValueIndex, out var expectedProperties))
            {
                foreach (var (nome, _, _) in expectedProperties)
                {
                    var property = propertiesProperty.FindPropertyRelative(nome);
                    if (property != null)
                        EditorGUILayout.PropertyField(property, new GUIContent(ObjectNames.NicifyVariableName(nome)));
                    else
                        EditorGUILayout.HelpBox($"Propriedade '{nome}' não encontrada em TriggerProperties.", MessageType.Error);
                }

                if ((TriggerType)triggerTypeProperty.enumValueIndex == TriggerType.PredicateTrigger)
                {
                    var predicateProperty = propertiesProperty.FindPropertyRelative("predicate");
                    if (predicateProperty.objectReferenceValue == null)
                        EditorGUILayout.HelpBox("Predicate é obrigatório para PredicateTrigger.", MessageType.Warning);
                }
                else if ((TriggerType)triggerTypeProperty.enumValueIndex == TriggerType.CompositeTrigger)
                {
                    var compositeTriggersProperty = propertiesProperty.FindPropertyRelative("compositeTriggers");
                    if (compositeTriggersProperty.arraySize == 0)
                        EditorGUILayout.HelpBox("Pelo menos um trigger é necessário para CompositeTrigger.", MessageType.Warning);
                    else
                    {
                        for (int i = 0; i < compositeTriggersProperty.arraySize; i++)
                        {
                            var element = compositeTriggersProperty.GetArrayElementAtIndex(i);
                            EditorGUILayout.PropertyField(element, new GUIContent($"Trigger {i + 1}"));
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}