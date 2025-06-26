using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using DG.Tweening;
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
        void ReArm();
        bool IsActive { get; }
    }
    public interface IObjectMovement
    {
        void Initialize(Vector3? direction, float speed, Transform target = null);
        void StopMovement();
    }
}