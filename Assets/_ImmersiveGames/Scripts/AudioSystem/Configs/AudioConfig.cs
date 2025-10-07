using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Audio/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Sound Effects")]
        public SoundData shootSound;
        [Header("Damage System Sounds")]
        public SoundData hitSound;
        public SoundData deathSound;
        public SoundData reviveSound;
    
        [Header("Configuration")]
        public bool useSpatialBlend = true;
        public float maxDistance = 50f;
        public float defaultVolume = 1f;
    }
}