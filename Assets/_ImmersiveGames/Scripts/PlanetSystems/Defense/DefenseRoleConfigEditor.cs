#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CustomEditor(typeof(DefenseRoleConfig))]
    public class DefenseRoleConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "This config maps identifiers (e.g., ActorName) to defense roles. Use it as a fallback when providers are missing. The fallback role is applied when no mapping matches.",
                MessageType.Info);

            DrawDefaultInspector();
        }
    }
}
#endif
