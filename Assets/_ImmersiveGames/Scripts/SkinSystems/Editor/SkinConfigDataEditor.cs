using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEditor;
namespace _ImmersiveGames.Scripts.SkinSystems.Editor
{
    [CustomEditor(typeof(SkinConfigData))]
    public class SkinConfigDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var skinConfig = (SkinConfigData)target;

            if (skinConfig.GetSelectedPrefabs().Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhum prefab configurado para este Skin!", MessageType.Warning);
            }
        }
    }
}