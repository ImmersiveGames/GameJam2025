using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
// Para PoolableObjectData

namespace _ImmersiveGames.Scripts.SpawnSystem
{
    [CreateAssetMenu(fileName = "SpawnData", menuName = "SpawnSystem/SpawnData")]
    public class SpawnData : ScriptableObject
    {
        [SerializeField] private string objectName = "";
        [SerializeField] private int spawnCount = 1;
        [SerializeField] private string patternType = "Mock";
        [SerializeField] private PoolableObjectData poolableData; // Referência ao PoolableObjectData

        public string ObjectName => objectName.Trim();
        public int SpawnCount => spawnCount;
        public string PatternType => patternType;
        public PoolableObjectData PoolableData => poolableData;
    }
}