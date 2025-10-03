using _ImmersiveGames.Scripts.DetectionsSystems.Runtime;
using UnityEditor;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DetectionsSystems.Editor
{
    [CustomEditor(typeof(SensorCollection))]
    public class SensorCollectionEditor : UnityEditor.Editor
    {
        private SerializedProperty _sensors;

        private void OnEnable()
        {
            _sensors = serializedObject.FindProperty("sensors");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Sensor Collection", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            for (int i = 0; i < _sensors.arraySize; i++)
            {
                var element = _sensors.GetArrayElementAtIndex(i);
                var sensorConfig = element.objectReferenceValue as SensorConfig;

                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUILayout.BeginHorizontal();
                    element.objectReferenceValue = EditorGUILayout.ObjectField("Sensor Config", element.objectReferenceValue, typeof(SensorConfig), false);

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        _sensors.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (sensorConfig == null) continue;
                    EditorGUILayout.LabelField("DetectionType", sensorConfig.DetectionType != null ? sensorConfig.DetectionType.name : "⚠️ NONE");

                    if (sensorConfig.Radius <= 0)
                        EditorGUILayout.HelpBox("⚠️ Raio do sensor é zero ou negativo!", MessageType.Warning);

                    if (sensorConfig.TargetLayer == 0)
                        EditorGUILayout.HelpBox("⚠️ Nenhuma Layer definida para detecção!", MessageType.Warning);
                }
            }

            if (GUILayout.Button("Adicionar Novo SensorConfig"))
            {
                _sensors.InsertArrayElementAtIndex(_sensors.arraySize);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
