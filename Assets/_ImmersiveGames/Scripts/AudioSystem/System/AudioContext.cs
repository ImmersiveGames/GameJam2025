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
        public Vector3 position;
        public bool useSpatial;
        public float volumeMultiplier;

        public static AudioContext Default(Vector3 pos, bool useSpatial = true, float volMult = 1f)
        {
            return new AudioContext { position = pos, useSpatial = useSpatial, volumeMultiplier = volMult };
        }

        public static AudioContext NonSpatial(float volMult = 1f)
        {
            return new AudioContext { position = Vector3.zero, useSpatial = false, volumeMultiplier = volMult };
        }
    }
}