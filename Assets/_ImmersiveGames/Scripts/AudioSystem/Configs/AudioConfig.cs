using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Audio/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Volume Defaults")]
        public float defaultVolume = 1f;
        public AudioMixerGroup defaultMixerGroup;

        [Header("3D Settings")]
        public float maxDistance = 50f;
        public bool useSpatialBlend = true;

    }
}