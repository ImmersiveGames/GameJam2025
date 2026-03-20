using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Config
{
    /// <summary>
    /// Base abstrata de cue de áudio.
    /// </summary>
    public abstract class AudioCueAsset : ScriptableObject
    {
        [SerializeField] private List<AudioClip> clips = new List<AudioClip>();
        [SerializeField] private AudioMixerGroup mixerGroup;
        [SerializeField] [Range(0f, 1f)] private float baseVolume = 1f;
        [SerializeField] private bool loop;
        [SerializeField] private float pitchMin = 1f;
        [SerializeField] private float pitchMax = 1f;
        [SerializeField] [Range(0f, 1f)] private float randomVolumeJitter;

        public IReadOnlyList<AudioClip> Clips => clips;
        public AudioMixerGroup MixerGroup => mixerGroup;
        public float BaseVolume => baseVolume;
        public bool Loop => loop;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;
        public float RandomVolumeJitter => randomVolumeJitter;

        public bool TryPickClip(out AudioClip clip)
        {
            clip = null;

            if (clips == null || clips.Count == 0)
                return false;

            var valid = 0;
            for (var i = 0; i < clips.Count; i++)
            {
                if (clips[i] != null)
                    valid++;
            }

            if (valid <= 0)
                return false;

            var index = Random.Range(0, clips.Count);
            clip = clips[index];

            if (clip != null)
                return true;

            for (var i = 0; i < clips.Count; i++)
            {
                if (clips[i] != null)
                {
                    clip = clips[i];
                    return true;
                }
            }

            return false;
        }

        public bool ValidateRuntime(out string reason)
        {
            if (clips == null || clips.Count == 0)
            {
                reason = "missing_clips";
                return false;
            }

            if (baseVolume < 0f || baseVolume > 1f)
            {
                reason = "invalid_base_volume";
                return false;
            }

            if (pitchMin > pitchMax)
            {
                reason = "invalid_pitch_range";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
