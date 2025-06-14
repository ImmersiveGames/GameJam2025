using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "SingleSpawnStrategy", menuName = "ImmersiveGames/Strategies/SingleSpawn")]
    public class SingleSpawnStrategy : SpawnStrategySo
    {
        public override void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            DebugUtility.LogVerbose<SingleSpawnStrategy>($"Spawnando {objects.Length} objetos na posição {origin}.");
            if (objects.Length == 0) return;

            foreach (var obj in objects)
            {
                if (obj == null) continue;
                var go = obj.GetGameObject();
                go.transform.position = origin; // Garante a posição correta
                DebugUtility.LogVerbose<SingleSpawnStrategy>($"Objeto {go.name} spawnado na posição {go.transform.position}.");
            }
        }
    }
}