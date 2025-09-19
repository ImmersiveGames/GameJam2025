#if UNITY_EDITOR
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

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("⬆️ Aumentar", "Aumenta o recurso em {amount} unidades")))
                tester.IncreaseResource();
            if (GUILayout.Button(new GUIContent("⬇️ Diminuir", "Diminui o recurso em {amount} unidades")))
                tester.DecreaseResource();
            if (GUILayout.Button(new GUIContent("🔄 Resetar", "Reseta o recurso para o valor inicial")))
                tester.ResetResource();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("🧪 Adicionar Modificador", "Adiciona um modificador ao recurso")))
                tester.AddModifier();
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
        }
    }
}
#endif