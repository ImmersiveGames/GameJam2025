using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.AudioSystem.Pool
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Audio/SoundEmitter Pool Data")]
    public class SoundEmitterPoolData : PoolData
    {
        [Header("Audio Pool Settings")]
        [SerializeField] private int maxSoundInstances = 30;

        public int MaxSoundInstances => maxSoundInstances;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxSoundInstances < initialPoolSize)
            {
                DebugUtility.LogWarning<SoundEmitterPoolData>(
                    $"MaxSoundInstances não pode ser menor que InitialPoolSize em {name}. Ajustando...",
                    this);
                maxSoundInstances = initialPoolSize;
            }
        }
#endif
    }
}