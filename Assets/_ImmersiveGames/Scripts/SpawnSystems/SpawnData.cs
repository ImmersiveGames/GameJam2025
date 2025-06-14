using _ImmersiveGames.Scripts.SpawnSystems.Strategies;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [CreateAssetMenu(fileName = "SpawnData", menuName = "SpawnSystem/SpawnData")]
    public class SpawnData : ScriptableObject
    {
        [SerializeField] private PoolableObjectData poolableData;
        [SerializeField] private int spawnCount = 1;
        [SerializeField] private SpawnStrategySo pattern;
        [SerializeField] private SpawnTriggerSo triggerStrategy;
        
        

        // Propriedades públicas
        public PoolableObjectData PoolableData => poolableData;
        public int SpawnCount => spawnCount;
        public SpawnStrategySo Pattern => pattern;
        public SpawnTriggerSo TriggerStrategy => triggerStrategy;
    }
}