using System;
using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    public enum AudioContextMode
    {
        Auto,
        Spatial,
        NonSpatial
    }

    [Serializable]
    public class SoundData
    {
        [Header("Audio Clip")]
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;

        [Header("Audio Settings")]
        [Range(0, 1)] public float volume = 1f;
        [Range(0, 256)] public int priority = 128;
        public bool loop = false;
        public bool playOnAwake = false;

        [Header("Behavior")]
        public bool frequentSound = false;
        public bool randomPitch = false;
        [Range(-0.2f, 0.2f)] public float pitchVariation = 0.05f;

        [Header("Spatial Settings")]
        [Range(0, 1)] public float spatialBlend = 0f;
        public float maxDistance = 50f;

        public bool IsSpatial => spatialBlend > 0.5f;

        public float GetEffectiveVolume(float multiplier = 1f)
        {
            return Mathf.Clamp01(volume * multiplier);
        }
    }
}