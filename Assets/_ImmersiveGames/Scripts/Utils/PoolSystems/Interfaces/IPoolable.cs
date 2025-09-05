using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces
{
    public interface IPoolable
    {
        void Configure(PoolableObjectData data, ObjectPool pool);
        void Reconfigure(PoolableObjectData data);
        void Activate(Vector3 position, IActor spawner = null);
        void Deactivate();
        GameObject GetGameObject();
        void PoolableReset();
        void ReturnToPool();
        UnityEvent OnActivated { get; }
        UnityEvent OnDeactivated { get; }
        PoolableObjectData Data { get; }
        T GetData<T>() where T : PoolableObjectData;
        ObjectPool GetPool();
    }
    public interface IObjectPoolFactory
    {
        IPoolable CreateObject(PoolableObjectData config, Transform parent, Vector3 position, string objectName, ObjectPool pool);
    }
}