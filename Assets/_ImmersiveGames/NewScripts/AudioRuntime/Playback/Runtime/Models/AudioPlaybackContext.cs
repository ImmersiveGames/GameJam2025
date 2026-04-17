using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models
{
    /// <summary>
    /// Contexto can�nico de playback.
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
}

