using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class SingleSpawnStrategy : ISpawnStrategy
    {
        public void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data)
        {
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    obj.Activate(origin);
                }
            }
        }
    }
}