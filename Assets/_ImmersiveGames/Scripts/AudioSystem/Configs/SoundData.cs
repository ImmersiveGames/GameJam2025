using UnityEngine;
using UnityEngine.Audio;
namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    [CreateAssetMenu(fileName = "SoundData", menuName = "ImmersiveGames/Audio/SoundData", order = 1)] // Adicionado: Para criar assets reutilizáveis
    public class SoundData : ScriptableObject // Mudado de class para SO
    {
        [Header("Audio Clip")]
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;

        [Header("Audio Settings")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0, 256)] public int priority = 128;
        public bool loop;
        public bool playOnAwake;

        [Header("Behavior")]
        public bool randomPitch;
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;

        [Header("Spatial Settings")]
        [Range(0f, 1f)] public float spatialBlend;
        public float maxDistance = 50f;
    }
}