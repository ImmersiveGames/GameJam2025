#if UNITY_EDITOR
using _ImmersiveGames.Scripts.Testing;
using UnityEditor;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Editor
{
    [CustomEditor(typeof(ResourceSystemTester))]
    public class ResourceSystemTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var tester = (ResourceSystemTester)target;

            GUILayout.Space(10);
            GUILayout.Label("🧪 Ações de Teste", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("⬆️ Aumentar Recurso"))
                tester.IncreaseTest();

            if (GUILayout.Button("⬇️ Diminuir Recurso"))
                tester.DecreaseTest();

            if (GUILayout.Button("🔄 Resetar Recurso"))
                tester.ResetTest();

            GUI.enabled = true;
        }
    }
}
#endif