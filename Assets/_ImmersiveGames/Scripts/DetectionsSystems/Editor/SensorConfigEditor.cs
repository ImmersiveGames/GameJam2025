using _ImmersiveGames.Scripts.DetectionsSystems.Runtime;
using UnityEditor;
namespace _ImmersiveGames.Scripts.DetectionsSystems.Editor
{
    [CustomEditor(typeof(SensorConfig))]
    public class SensorConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var sensorConfig = (SensorConfig)target;

            if (sensorConfig.DetectionType == null)
            {
                EditorGUILayout.HelpBox("DetectionType não atribuído!", MessageType.Error);
            }

            if (sensorConfig.Radius <= 0)
            {
                EditorGUILayout.HelpBox("O raio do sensor deve ser maior que 0!", MessageType.Error);
            }
        }
    }
}