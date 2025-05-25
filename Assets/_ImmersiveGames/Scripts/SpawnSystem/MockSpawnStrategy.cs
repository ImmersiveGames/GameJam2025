using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class MockSpawnStrategy : ISpawnStrategy
    {
        public void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data)
        {
            foreach (var obj in objects)
            {
                obj.Activate(origin); // Posiciona todos no origin
            }
        }
    }
}