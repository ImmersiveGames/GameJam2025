using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "WaveSpawnStrategy",menuName = "ImmersiveGames/Strategies/Wave")]
    public class WaveSpawnStrategy : SpawnStrategySo
    {
        [SerializeField] private float waveInterval = 1f;
        [SerializeField] private int waveCount = 3;
        public override void Spawn(IPoolable[] objects,SpawnData data, Vector3 origin, Vector3 forward)
        {
            int objectsPerWave = Mathf.CeilToInt((float)objects.Length / waveCount);
            for (int wave = 0; wave < waveCount; wave++)
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