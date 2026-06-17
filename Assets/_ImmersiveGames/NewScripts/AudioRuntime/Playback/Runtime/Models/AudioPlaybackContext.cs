using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models
{
    /// <summary>
    /// Contexto can�nico de playback.
    /// </summary>
    public struct AudioPlaybackContext
    {
        public bool useSpatial;
        public Vector3 worldPosition;
        public Transform followTarget;
        public float volumeScale;
        public string reason;
        public AudioSfxVoiceProfileAsset voiceProfile;
        public AudioSfxEmissionProfileAsset emissionProfile;
        public AudioSfxExecutionProfileAsset executionProfile;

        public static AudioPlaybackContext Global(string reason = null, float volumeScale = 1f)
        {
            return new AudioPlaybackContext
            {
                useSpatial = false,
                worldPosition = Vector3.zero,
                followTarget = null,
                volumeScale = Mathf.Max(0f, volumeScale),
                reason = reason,
                voiceProfile = null,
                emissionProfile = null,
                executionProfile = null
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
                useSpatial = true,
                worldPosition = worldPosition,
                followTarget = followTarget,
                volumeScale = Mathf.Max(0f, volumeScale),
                reason = reason,
                voiceProfile = voiceProfile,
                emissionProfile = null,
                executionProfile = null
            };
        }
    }
}

