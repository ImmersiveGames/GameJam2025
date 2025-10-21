using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Editor
{
    [CustomEditor(typeof(SoundData))]
    public class SoundDataEditor : UnityEditor.Editor
    {
        private const string PreviewObjectName = "__SoundDataPreview";

        private static AudioSource _previewSource;
        private static double _previewEndTime;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // Mantém inspector padrão

            var data = target as SoundData;

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(data == null))
            {
                bool isPreviewingCurrent = _previewSource != null && _previewSource.clip == data?.clip;
                string buttonLabel = isPreviewingCurrent ? "Stop Preview" : "Test Play Sound";

                if (GUILayout.Button(buttonLabel))
                {
                    if (isPreviewingCurrent)
                    {
                        StopPreview();
                        return;
                    }

                    StopPreview();

                    if (data?.clip == null)
                    {
                        DebugUtility.LogWarning<SoundDataEditor>("Nenhum clip configurado no SoundData para teste.");
                        return;
                    }

                    PlayPreview(data);
                }
            }
        }

        private static void PlayPreview(SoundData data)
        {
            StopPreview();

            if (data == null || data.clip == null)
            {
                return;
            }

            var previewGo = EditorUtility.CreateGameObjectWithHideFlags(
                PreviewObjectName,
                HideFlags.HideAndDontSave,
                typeof(AudioSource));

            previewGo.transform.position = ResolvePreviewPosition(data);

            var source = previewGo.GetComponent<AudioSource>();
            ConfigureSourceFromData(source, data);

            source.Play();

            _previewSource = source;
            _previewEndTime = EditorApplication.timeSinceStartup + CalculateClipDuration(source, data);

            EditorApplication.update += EditorUpdate;

            DebugUtility.LogVerbose<SoundDataEditor>($"Preview iniciado: {data.clip.name} (Vol: {source.volume:F2}, Pitch: {source.pitch:F2}, SpatialBlend: {source.spatialBlend:F2})");
        }

        private static void StopPreview()
        {
            if (_previewSource == null)
            {
                return;
            }

            EditorApplication.update -= EditorUpdate;

            if (_previewSource.isPlaying)
            {
                _previewSource.Stop();
            }

            Object.DestroyImmediate(_previewSource.gameObject);
            _previewSource = null;
        }

        private static void EditorUpdate()
        {
            if (_previewSource == null)
            {
                EditorApplication.update -= EditorUpdate;
                return;
            }

            if (!_previewSource.loop && EditorApplication.timeSinceStartup >= _previewEndTime)
            {
                StopPreview();
            }
        }

        private static void ConfigureSourceFromData(AudioSource source, SoundData data)
        {
            if (source == null || data == null)
            {
                return;
            }

            // Configurações base do AudioSource respeitando SoundData
            source.hideFlags = HideFlags.HideAndDontSave;
            source.clip = data.clip;
            source.outputAudioMixerGroup = data.mixerGroup;
            source.volume = data.volume;
            source.priority = data.priority;
            source.loop = data.loop;
            source.playOnAwake = false; // Preview sempre inicia manualmente

            // Configurações de espacialização
            source.spatialBlend = data.spatialBlend;
            source.maxDistance = data.maxDistance;

            // Pitch aleatório quando configurado
            source.pitch = 1f;
            if (data.randomPitch)
            {
                source.pitch = Random.Range(1f - data.pitchVariation, 1f + data.pitchVariation);
            }
        }

        private static Vector3 ResolvePreviewPosition(SoundData data)
        {
            if (data == null)
            {
                return Vector3.zero;
            }

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
            {
                return sceneView.camera.transform.position;
            }

            return Vector3.zero;
        }

        private static double CalculateClipDuration(AudioSource source, SoundData data)
        {
            if (source == null || data?.clip == null)
            {
                return 0d;
            }

            float pitch = Mathf.Approximately(source.pitch, 0f) ? 0.0001f : Mathf.Abs(source.pitch);
            return data.clip.length / pitch;
        }

        private void OnDisable()
        {
            if (_previewSource != null)
            {
                StopPreview();
            }
        }
    }
}