using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    public class BurstSpawnStrategy : ISpawnStrategy
    {
        public void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data, Vector3 transformForward)
        {
            float angleStep = 360f / objects.Length;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null) continue;
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * data.Radius;
                objects[i].Activate(origin + offset);
            }
        }
    }
}