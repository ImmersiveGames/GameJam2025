using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public class BurstStrategy : ISpawnStrategy
    {
        private readonly float _radius;
        private readonly float _space;

        public BurstStrategy(float radius, float space)
        {
            _radius = radius;
            _space = space;
        }

        public void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            float angleStep = 360f / objects.Length;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null) continue;
                float angle = i * angleStep * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * _radius;
                objects[i].Activate(origin + offset);
            }
        }
    }
}