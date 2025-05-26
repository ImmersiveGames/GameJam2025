using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    public class RandomSpawnStrategy : ISpawnStrategy
    {
        public void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data, Vector3 transformForward)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                Vector3 randomOffset = new Vector3(
                    Random.Range(-data.SpawnArea.x / 2, data.SpawnArea.x / 2),
                    0,
                    Random.Range(-data.SpawnArea.y / 2, data.SpawnArea.y / 2)
                );
                obj.Activate(origin + randomOffset);
            }
        }
    }
}