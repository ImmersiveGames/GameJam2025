using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public class RandomSpawnPattern : ISpawnPattern
    {
        public Vector3 GetSpawnPosition(Vector3 basePosition, Vector3 spawnAreaSize)
        {
            return new Vector3(
                basePosition.x + Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                basePosition.y,
                basePosition.z + Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );
        }
    }
    public interface ISpawnPattern
    {
        Vector3 GetSpawnPosition(Vector3 basePosition, Vector3 spawnAreaSize);
    }
}