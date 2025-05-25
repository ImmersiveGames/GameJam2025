using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class MockSpawnStrategy : ISpawnStrategy
    {
        public void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null)
                {
                    DebugUtility.LogVerbose<MockSpawnStrategy>("Objeto nulo ignorado no spawn.", "yellow");
                    continue;
                }
                GameObject go = objects[i].GetGameObject();
                if (go != null)
                {
                    go.transform.position = origin;
                    DebugUtility.LogVerbose<MockSpawnStrategy>($"Objeto {go.name} posicionado em {origin}.", "blue");
                }
            }
        }
    }
}