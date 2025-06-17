using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public interface ISpawnStrategy
    {
        void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward);
    }
    

    public interface ISpawnTrigger
    {
        void Initialize(SpawnPoint spawnPoint);
        bool CheckTrigger(Vector3 origin, SpawnData data);
        void SetActive(bool active);
        void Reset();
    }

}