using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public enum SpawnPattern { Single } // Expansível para Wave, Random, Burst

    [CreateAssetMenu(fileName = "SpawnData", menuName = "SpawnSystem/SpawnData")]
    public class SpawnData : ScriptableObject
    {
        [SerializeField] private PoolableObjectData poolableData;
        [SerializeField] private int spawnCount = 1;
        [SerializeField] private SpawnPattern pattern = SpawnPattern.Single;

        public PoolableObjectData PoolableData => poolableData;
        public int SpawnCount => spawnCount;
        public SpawnPattern Pattern => pattern;
    }
}