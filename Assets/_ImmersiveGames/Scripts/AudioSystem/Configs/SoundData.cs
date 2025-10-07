using System;
using UnityEngine;
using UnityEngine.Audio;
namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    [Serializable]
    public class SoundData
    {
        [Header("Audio Clip")]
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;
        
        [Header("Audio Settings")]
        [Range(0, 1)]
        public float volume = 1f;
        [Range(0, 256)]
        public int priority = 128;
        public bool loop = false;
        public bool playOnAwake = false;
        
        [Header("Behavior")]
        public bool frequentSound = false;
        public bool randomPitch = false;
        
        [Header("Spatial Settings")]
        [Range(0, 1)]
        public float spatialBlend = 0f;
        public float maxDistance = 50f;
    }
}