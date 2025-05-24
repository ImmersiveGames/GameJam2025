using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces
{
    public interface IPoolable
    {
        bool IsActive { get; }
        void Initialize(PoolableObjectData data, ObjectPool pool);
        void Activate(Vector3 position);
        void Deactivate();
        void OnObjectReturned();
        void OnObjectSpawned();
    }

    public interface IPoolableFactory
    {
        void BuildStructure(GameObject target, PoolableObjectData data);
    }

    public interface IProjectile
    {
        void Configure(Vector3 direction, float speed);
    }
}