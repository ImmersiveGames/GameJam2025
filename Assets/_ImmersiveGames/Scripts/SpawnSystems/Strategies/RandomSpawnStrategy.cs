using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "RandomSpawnStrategy",menuName = "ImmersiveGames/Strategies/RandomSpawn")]
    public class RandomSpawnStrategy : SpawnStrategySo
    {
        [SerializeField] private Vector2 spawnArea = new Vector2(5f, 5f);
        public override void Spawn(IPoolable[] objects, Vector3 origin, Vector3 forward)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                Vector3 randomOffset = new Vector3(
                    Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
                    0,
                    Random.Range(-spawnArea.y / 2, spawnArea.y / 2)
                );
                obj.Activate(origin + randomOffset);
            }
        }
    }
}