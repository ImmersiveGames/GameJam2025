using System;
using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    [Serializable]
    public class SoundData
    {
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;
        [Range(0, 1)]
        public float volume = 1f;
        [Range(0, 256)]
        public int priority = 128;
        public bool loop;
        public bool playOnAwake;
        public bool frequentSound;
        public bool randomPitch;
    }    
}


