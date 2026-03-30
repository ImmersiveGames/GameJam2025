using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Contexto canônico de playback.
    /// </summary>
    public struct AudioPlaybackContext
    {
        public bool UseSpatial;
        public Vector3 WorldPosition;
        public Transform FollowTarget;
        public float VolumeScale;
        public string Reason;
        public AudioSfxVoiceProfileAsset VoiceProfile;
        public AudioSfxEmissionProfileAsset EmissionProfile;
        public AudioSfxExecutionProfileAsset ExecutionProfile;

        public static AudioPlaybackContext Global(string reason = null, float volumeScale = 1f)
        {
            return new AudioPlaybackContext
            {
                UseSpatial = false,
                WorldPosition = Vector3.zero,
                FollowTarget = null,
                VolumeScale = Mathf.Max(0f, volumeScale),
                Reason = reason,
                VoiceProfile = null,
                EmissionProfile = null,
                ExecutionProfile = null
            };
        }

        public static AudioPlaybackContext Spatial(
            Vector3 worldPosition,
            Transform followTarget = null,
            string reason = null,
            float volumeScale = 1f,
            AudioSfxVoiceProfileAsset voiceProfile = null)
        {
            return new AudioPlaybackContext
            {
                UseSpatial = true,
                WorldPosition = worldPosition,
                FollowTarget = followTarget,
                VolumeScale = Mathf.Max(0f, volumeScale),
                Reason = reason,
                VoiceProfile = voiceProfile,
                EmissionProfile = null,
                ExecutionProfile = null
            };
        }
    }

    /// <summary>
    /// Canonical minimal global AudioListener host for F3/BGM runtime.
    /// This is not the final listener policy for spatial audio.
    /// </summary>
    public sealed class AudioListenerRuntimeHost : MonoBehaviour
    {
        private const string HostObjectName = "NewScripts_AudioListenerRuntime";

        private AudioListener _listener;

        public static AudioListenerRuntimeHost EnsureCreated()
        {
            AudioListenerRuntimeHost[] existingHosts = Object.FindObjectsByType<AudioListenerRuntimeHost>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (existingHosts != null && existingHosts.Length > 0)
            {
                var host = existingHosts[0];
                host.EnsureListenerComponent();
                Object.DontDestroyOnLoad(host.gameObject);

                if (existingHosts.Length > 1)
                {
                    for (int i = 1; i < existingHosts.Length; i++)
                    {
                        var extraHost = existingHosts[i];
                        if (extraHost == null || extraHost == host)
                        {
                            continue;
                        }

                        Object.Destroy(extraHost.gameObject);
                    }

                    DebugUtility.LogWarning(typeof(AudioListenerRuntimeHost),
                        $"[Audio][Listener] Multiple canonical hosts detected. Keeping '{host.name}' and removing extras ({existingHosts.Length - 1}).");
                }

                host.EnforceSingleListenerPolicy();

                DebugUtility.LogVerbose(typeof(AudioListenerRuntimeHost),
                    $"[Audio][Listener] Runtime host reused. host='{host.name}'.",
                    DebugUtility.Colors.Info);
                return host;
            }

            var runtimeObject = new GameObject(HostObjectName);
            var createdHost = runtimeObject.AddComponent<AudioListenerRuntimeHost>();
            createdHost.EnsureListenerComponent();
            Object.DontDestroyOnLoad(runtimeObject);
            createdHost.EnforceSingleListenerPolicy();

            DebugUtility.LogVerbose(typeof(AudioListenerRuntimeHost),
                $"[Audio][Listener] Runtime host created. host='{runtimeObject.name}'.",
                DebugUtility.Colors.Info);

            return createdHost;
        }

        private void Awake()
        {
            EnsureListenerComponent();
            EnforceSingleListenerPolicy();
        }

        private void OnEnable()
        {
            EnsureListenerComponent();
            EnforceSingleListenerPolicy();
        }

        private void EnsureListenerComponent()
        {
            if (!TryGetComponent(out _listener) || _listener == null)
            {
                _listener = gameObject.AddComponent<AudioListener>();
            }

            if (!_listener.enabled)
            {
                _listener.enabled = true;
            }
        }

        private void EnforceSingleListenerPolicy()
        {
            if (_listener == null)
            {
                return;
            }

            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (listeners == null || listeners.Length == 0)
            {
                return;
            }

            int duplicatesDetected = 0;
            for (int i = 0; i < listeners.Length; i++)
            {
                var candidate = listeners[i];
                if (candidate == null || candidate == _listener)
                {
                    continue;
                }

                if (!candidate.enabled)
                {
                    continue;
                }

                duplicatesDetected++;
                candidate.enabled = false;

                DebugUtility.LogWarning(typeof(AudioListenerRuntimeHost),
                    $"[Audio][Listener] Foreign enabled listener disabled. owner='{candidate.gameObject.name}'.");
            }

            if (duplicatesDetected > 0)
            {
                DebugUtility.LogWarning(typeof(AudioListenerRuntimeHost),
                    $"[Audio][Listener] Duplicate listeners detected. Canonical listener remains active. disabled={duplicatesDetected}.");
            }
        }
    }
}
