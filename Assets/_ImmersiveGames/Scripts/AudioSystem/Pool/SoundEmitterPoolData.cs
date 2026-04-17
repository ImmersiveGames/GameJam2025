using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Pool
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Legacy/Audio/SoundEmitter Pool Data")]
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
                    $"MaxSoundInstances nï¿½o pode ser menor que InitialPoolSize em {name}. Ajustando...",
                    this);
                maxSoundInstances = initialPoolSize;
            }
        }
#endif
    }
}
