using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public interface ISpawnStrategy
    {
        void Spawn(ObjectPool pool, Vector3 origin, GameObject sourceObject = null);
    }
    public interface ISpawnTrigger
    {
        void Initialize(SpawnPoint spawnPointRef);
        bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject);
        void SetActive(bool active);
        void Reset();
        bool IsActive { get; }
    }
    public interface IMoveObject
    { void Initialize(Vector3? direction, float speed, Transform target = null);
    }
    
}