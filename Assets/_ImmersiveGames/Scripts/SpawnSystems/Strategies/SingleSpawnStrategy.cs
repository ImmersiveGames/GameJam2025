using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "SingleShootSpawnStrategy",menuName = "ImmersiveGames/Strategies/SingleSpawn")]
    public class SingleSpawnStrategy : SpawnStrategySo
    {

        public override void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                var go = obj.GetGameObject();
                go.transform.position = origin;
                obj.Activate(origin);
            }
        }
    }
}