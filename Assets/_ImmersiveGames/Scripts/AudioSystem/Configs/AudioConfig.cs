using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Audio/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Sound Effects (entity defaults)")]
        public SoundData shootSound;
        public SoundData hitSound;
        public SoundData deathSound;
        public SoundData reviveSound;

        [Header("Defaults")]
        public bool useSpatialBlend = true;
        [Range(0f, 200f)] public float maxDistance = 50f;
        [Range(0f, 1f)] public float defaultVolume = 1f;
    }
}