using System;
using System.Reflection;
using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using UnityEditor;
using UnityEngine;

namespace ImmersiveGames.GameJam2025.Modules.Audio.Editor
{
    [CustomEditor(typeof(AudioSfxCueAsset))]
    [CanEditMultipleObjects]
    internal sealed class AudioSfxCueAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _clipsProperty;

        private void OnEnable()
        {
            _clipsProperty = serializedObject.FindProperty("clips");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            AudioClip previewClip = ResolvePreviewClip();
            DrawPreviewInfo(previewClip);
            DrawPreviewControls(previewClip);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreviewInfo(AudioClip previewClip)
        {
            int clipCount = _clipsProperty != null ? _clipsProperty.arraySize : 0;

            EditorGUILayout.LabelField("Clips no cue", clipCount.ToString());

            if (previewClip == null)
            {
                EditorGUILayout.HelpBox("Nenhum AudioClip valido encontrado neste cue.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Clip resolvido", previewClip.name);
            EditorGUILayout.LabelField("Duracao", $"{previewClip.length:0.###} s");
            EditorGUILayout.LabelField("Estado de carga", previewClip.loadState.ToString());
        }

        private void DrawPreviewControls(AudioClip previewClip)
        {
            using (new EditorGUI.DisabledScope(previewClip == null))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Play", GUILayout.Height(24f)))
                {
                    AudioSfxCuePreviewUtility.Play(previewClip);
                }

                if (GUILayout.Button("Stop", GUILayout.Height(24f)))
                {
                    AudioSfxCuePreviewUtility.Stop();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private AudioClip ResolvePreviewClip()
        {
            if (_clipsProperty == null || !_clipsProperty.isArray || _clipsProperty.arraySize == 0)
            {
                return null;
            }

            for (int i = 0; i < _clipsProperty.arraySize; i++)
            {
                var element = _clipsProperty.GetArrayElementAtIndex(i);
                if (element == null)
                {
                    continue;
                }

                var clip = element.objectReferenceValue as AudioClip;
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }
    }

    internal static class AudioSfxCuePreviewUtility
    {
        private static readonly Type AudioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        private static readonly MethodInfo PlayPreviewClipMethod = ResolveMethod("PlayPreviewClip", 1, 2, 3);
        private static readonly MethodInfo StopAllPreviewClipsMethod = ResolveMethod("StopAllPreviewClips", 0);
        private static readonly MethodInfo StopPreviewClipMethod = ResolveMethod("StopPreviewClip", 0);

        private static AudioClip _currentClip;

        public static void Play(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[Audio][Editor][SFXCue] Play blocked: clip is null.");
                return;
            }

            Stop();

            if (!InvokePlay(clip))
            {
                Debug.LogWarning($"[Audio][Editor][SFXCue] Play blocked: preview API unavailable for clip='{clip.name}'.");
                return;
            }

            _currentClip = clip;
        }

        public static void Stop()
        {
            if (StopAllPreviewClipsMethod != null)
            {
                StopAllPreviewClipsMethod.Invoke(null, Array.Empty<object>());
            }
            else if (StopPreviewClipMethod != null)
            {
                StopPreviewClipMethod.Invoke(null, Array.Empty<object>());
            }

            _currentClip = null;
        }

        private static bool InvokePlay(AudioClip clip)
        {
            if (AudioUtilType == null || PlayPreviewClipMethod == null)
            {
                return false;
            }

            object[] arguments = PlayPreviewClipMethod.GetParameters().Length switch
            {
                1 => new object[] { clip },
                2 => new object[] { clip, 0 },
                _ => new object[] { clip, 0, false },
            };

            PlayPreviewClipMethod.Invoke(null, arguments);
            return true;
        }

        private static MethodInfo ResolveMethod(string methodName, params int[] parameterCounts)
        {
            if (AudioUtilType == null)
            {
                return null;
            }

            var methods = AudioUtilType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                int parameters = method.GetParameters().Length;
                foreach (int expectedCount in parameterCounts)
                {
                    if (parameters == expectedCount)
                    {
                        return method;
                    }
                }
            }

            return null;
        }
    }
}

