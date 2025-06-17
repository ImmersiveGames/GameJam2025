using UnityEngine;
using UnityEditor;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems.Editor
{
    [CustomEditor(typeof(StrategyData))]
    public class StrategyDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var strategyTypeProperty = serializedObject.FindProperty("strategyType");
            var propertiesProperty = serializedObject.FindProperty("properties");

            EditorGUILayout.PropertyField(strategyTypeProperty);

            if (SpawnPropertyMap.StrategyPropertiesMap.TryGetValue((StrategyType)strategyTypeProperty.enumValueIndex, out var expectedProperties))
            {
                foreach (var (nome, _, _) in expectedProperties)
                {
                    var property = propertiesProperty.FindPropertyRelative(nome);
                    if (property != null)
                        EditorGUILayout.PropertyField(property, new GUIContent(ObjectNames.NicifyVariableName(nome)));
                    else
                        EditorGUILayout.HelpBox($"Propriedade '{nome}' não encontrada em StrategyProperties.", MessageType.Error);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}