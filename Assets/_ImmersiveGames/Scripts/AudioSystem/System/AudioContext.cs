using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public struct AudioContext
    {
        public Vector3 position;
        public bool useSpatial;
        public float volumeMultiplier; // multiplicador habitual
        public float volumeOverride;   // se >=0 substitui (0..1)

        public static AudioContext Default(Vector3 pos, bool useSpatial = true, float volMult = 1f, float volOverride = -1f)
        {
            return new AudioContext { position = pos, useSpatial = useSpatial, volumeMultiplier = volMult, volumeOverride = volOverride };
        }

        public static AudioContext NonSpatial(float volMult = 1f, float volOverride = -1f)
        {
            return new AudioContext { position = Vector3.zero, useSpatial = false, volumeMultiplier = volMult, volumeOverride = volOverride };
        }
    }
}