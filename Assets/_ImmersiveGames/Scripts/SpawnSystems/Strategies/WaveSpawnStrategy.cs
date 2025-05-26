using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    public class WaveSpawnStrategy : ISpawnStrategy
    {
        public void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data, Vector3 transformForward)
        {
            int objectsPerWave = Mathf.CeilToInt((float)objects.Length / data.WaveCount);
            for (int wave = 0; wave < data.WaveCount; wave++)
            {
                for (int i = 0; i < objectsPerWave; i++)
                {
                    int index = wave * objectsPerWave + i;
                    if (index >= objects.Length) break;
                    if (objects[index] != null)
                    {
                        Vector3 waveOffset = new Vector3(wave * 1f, 0, 0);
                        objects[index].Activate(origin + waveOffset);
                    }
                }
            }
        }
    }
}