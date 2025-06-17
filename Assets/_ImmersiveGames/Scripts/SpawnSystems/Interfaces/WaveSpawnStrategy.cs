using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public class WaveSpawnStrategy : ISpawnStrategy
    {
        private readonly float _waveInterval;
        private readonly int _waveCount;

        public WaveSpawnStrategy(float waveInterval, int waveCount)
        {
            _waveInterval = waveInterval;
            _waveCount = waveCount;
        }

        public void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            int objectsPerWave = Mathf.CeilToInt((float)objects.Length / _waveCount);
            for (int wave = 0; wave < _waveCount; wave++)
            {
                for (int i = 0; i < objectsPerWave; i++)
                {
                    int index = wave * objectsPerWave + i;
                    if (index >= objects.Length) break;
                    if (objects[index] == null) continue;
                    var waveOffset = new Vector3(wave * 1f, 0, 0);
                    objects[index].Activate(origin + waveOffset);
                }
            }
        }
    }
}