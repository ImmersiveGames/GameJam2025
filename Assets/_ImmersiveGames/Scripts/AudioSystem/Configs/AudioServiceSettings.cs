using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Audio/Audio Service Settings")]
    public class AudioServiceSettings : ScriptableObject
    {
        [Header("Global / Master")]
        [Range(0f,1f)] public float masterVolume = 1f;

        [Header("Category base volumes")]
        [Range(0f,1f)] public float bgmVolume = 1f;
        [Range(0f,1f)] public float sfxVolume = 1f;

        [Header("Category multipliers (balancing)")]
        [Range(0.1f,2f)] public float bgmMultiplier = 1f;
        [Range(0.1f,2f)] public float sfxMultiplier = 1f;
    }
}