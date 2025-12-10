using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEditor;

namespace _ImmersiveGames.Scripts.SkinSystems.Editor
{
    [CustomEditor(typeof(SkinConfigData), true)]
    public class SkinConfigDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var skinConfig = (SkinConfigData)target;

            // Se for uma config de áudio, não faz sentido exigir prefab.
            if (skinConfig is ISkinAudioConfig audioConfig)
            {
                var entries = audioConfig.AudioEntries;
                int count = entries != null ? entries.Count : 0;

                if (count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "Nenhuma entrada de áudio configurada para esta Skin de áudio.",
                        MessageType.Info);
                }

                return;
            }

            // Para configs visuais normais, mantemos o aviso de prefab.
            if (skinConfig.GetSelectedPrefabs().Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Nenhum prefab configurado para este Skin!",
                    MessageType.Warning);
            }
        }
    }
}