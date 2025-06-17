using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public class SingleSpawnStrategy : ISpawnStrategy
    {
        public void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            DebugUtility.LogVerbose<SingleSpawnStrategy>($"Spawnando {objects.Length} objetos na posição {origin}.");
            if (objects.Length == 0) return;

            foreach (var obj in objects)
            {
                if (obj == null) continue;
                var go = obj.GetGameObject();
                go.transform.position = origin;
                DebugUtility.LogVerbose<SingleSpawnStrategy>($"Objeto {go.name} spawnado na posição {go.transform.position}.");
            }
        }
    }
}