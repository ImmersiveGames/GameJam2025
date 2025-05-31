using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "BurstSpawnStrategy",menuName = "ImmersiveGames/Strategies/Burst")]
    public class BurstSpawnStrategy : SpawnStrategySo
    {
        [SerializeField] private float radius = 2f; // Raio do círculo
        [SerializeField] private float space = 5f; 
        public override void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            float angleStep = 360f / objects.Length;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null) continue;
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                objects[i].Activate(origin + offset);
            }
        }
    }
}