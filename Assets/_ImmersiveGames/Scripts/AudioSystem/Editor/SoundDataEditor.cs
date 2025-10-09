using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEditor;
using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem.Editor
{
    [CustomEditor(typeof(SoundData))]
    public class SoundDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // Mantém inspector padrão

            var data = target as SoundData;

            if (GUILayout.Button("Test Play Sound"))
            {
                if (data?.clip == null)
                {
                    Debug.LogWarning("Nenhum clip configurado no SoundData para teste.");
                    return;
                }

                // Preview simples no editor: Toca clip com volume do data (sem spatial/mixer para isolar)
                AudioSource.PlayClipAtPoint(data.clip, Vector3.zero, data.volume);
                Debug.Log($"Testando som: {data.clip.name} (Volume: {data.volume})");
            }
        }
    }
}