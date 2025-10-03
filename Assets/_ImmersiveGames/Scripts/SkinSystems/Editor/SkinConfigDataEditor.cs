using UnityEditor;
using UnityEngine;
using _ImmersiveGames.Scripts.SkinSystems.Data;

[CustomEditor(typeof(SkinConfigData))]
public class SkinConfigDataEditor : Editor
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