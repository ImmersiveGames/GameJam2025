using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public interface ISpawnStrategy
    {
        void Spawn(ObjectPool pool, Vector3 origin, Vector3 forward);
    }
    

    public interface ISpawnTrigger
    {
        void Initialize(SpawnPoint spawnPoint);
        bool CheckTrigger(Vector3 origin);
        void SetActive(bool active);
        void Reset();
        bool IsActive { get; }
    }
    public interface IObjectMovement
    {
        void Initialize(Vector3 direction, float speed);
    }
    
}