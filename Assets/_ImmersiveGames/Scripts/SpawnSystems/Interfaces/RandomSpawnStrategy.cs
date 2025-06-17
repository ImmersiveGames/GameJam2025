using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public class RandomSpawnStrategy : ISpawnStrategy
    {
        private readonly Vector2 _spawnArea;

        public RandomSpawnStrategy(Vector2 spawnArea)
        {
            _spawnArea = spawnArea;
        }

        public void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                var randomOffset = new Vector3(
                    Random.Range(-_spawnArea.x / 2, _spawnArea.x / 2),
                    0,
                    Random.Range(-_spawnArea.y / 2, _spawnArea.y / 2)
                );
                obj.Activate(origin + randomOffset);
            }
        }
    }
}