using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Contexto padrão para reprodução de áudio:
    /// - Position: posição onde o som deve tocar (usado quando UseSpatial = true)
    /// - UseSpatial: controla se o áudio será espacializado ou tocado no "camera" (não espacial)
    /// - VolumeMultiplier: multiplicador aplicado ao SoundData.volume
    /// </summary>
    public struct AudioContext
    {
        public Vector3 Position;
        public bool UseSpatial;
        public float VolumeMultiplier;

        public static AudioContext Default(Vector3 position) => new AudioContext
        {
            Position = position,
            UseSpatial = true,
            VolumeMultiplier = 1f
        };

        public static AudioContext NonSpatial(float volumeMultiplier = 1f) => new AudioContext
        {
            Position = Vector3.zero,
            UseSpatial = false,
            VolumeMultiplier = volumeMultiplier
        };
    }
}