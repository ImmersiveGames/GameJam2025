using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ObjectPool : MonoBehaviour
    {
        private readonly Queue<IPoolable> _pool = new();
        private readonly List<IPoolable> _activeObjects = new();
        public PoolableObjectData Data { get; private set; }
        private readonly ObjectPoolFactory _factory = new();

        public void SetData(PoolableObjectData data)
        {
            if (!data || !data.Prefab || !data.ModelPrefab)
            {
                DebugUtility.LogError<ObjectPool>($"PoolableObjectData, LogicPrefab ou ModelPrefab nulo.", this);
                return;
            }
            Data = data;
        }

        public void Initialize()
        {
            if (!Data)
            {
                DebugUtility.LogError<ObjectPool>($"Dados não configurados para o pool.", this);
                return;
            }

            for (int i = 0; i < Data.InitialPoolSize; i++)
            {
                var poolable = _factory.CreateObject(Data, transform, Vector3.zero, $"{Data.ObjectName}_{i}", this);
                if (poolable == null) continue;
                poolable.Deactivate();
                _pool.Enqueue(poolable);
            }
            DebugUtility.Log<ObjectPool>($"Pool inicializado com {_pool.Count} objetos para '{Data.ObjectName}'.", "green", this);
        }

        public IPoolable GetObject(Vector3 position)
        {
            IPoolable poolable = null;

            if (_pool.Count > 0)
            {
                poolable = _pool.Dequeue();
            }
            else if (Data.CanExpand)
            {
                poolable = _factory.CreateObject(Data, transform, position, $"{Data.ObjectName}_{_activeObjects.Count + _pool.Count}", this);
            }

            if (poolable == null)
            {
                DebugUtility.Log<ObjectPool>($"Nenhum objeto disponível no pool '{Data.ObjectName}'.", "yellow", this);
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(Data.ObjectName));
                return null;
            }

            poolable.Reset(); // Reseta antes de ativar
            poolable.Activate(position);
            _activeObjects.Add(poolable);
            EventBus<ObjectSpawnedEvent>.Raise(new ObjectSpawnedEvent(Data.ObjectName, position, poolable.GetGameObject()));
            return poolable;
        }

        public void ReturnObject(IPoolable poolable)
        {
            if (poolable == null || !_activeObjects.Remove(poolable)) return;
            poolable.Deactivate();
            _pool.Enqueue(poolable);
            EventBus<ObjectReturnedEvent>.Raise(new ObjectReturnedEvent(Data.ObjectName, poolable.GetGameObject()));
        }

        public IReadOnlyList<IPoolable> GetActiveObjects() => _activeObjects;

        public int GetAvailableCount() => _pool.Count;
    }
}