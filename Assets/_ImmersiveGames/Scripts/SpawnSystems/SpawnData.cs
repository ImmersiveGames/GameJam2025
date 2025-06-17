using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [CreateAssetMenu(fileName = "SpawnData", menuName = "SpawnSystem/SpawnData")]
    public class SpawnData : ScriptableObject
    {
        [SerializeField] private PoolableObjectData poolableData;
        [SerializeField] private int spawnCount = 1;

        public PoolableObjectData PoolableData => poolableData;
        public int SpawnCount => spawnCount;
    }
}