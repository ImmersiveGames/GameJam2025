using System.Collections.Generic;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class ObjectPool : MonoBehaviour
    {
        private readonly Queue<IPoolable> _pool = new();
        private readonly List<IPoolable> _activeObjects = new();
        public PoolableObjectData Data { get; private set; }
        public bool IsInitialized { get; private set; }
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
            IsInitialized = true;
            DebugUtility.Log<ObjectPool>($"Pool inicializado com {_pool.Count} objetos para '{Data.ObjectName}'.", "green", this);
        }

        public IPoolable GetObject(Vector3 position)
        {
            if (!IsInitialized || !Data)
            {
                DebugUtility.LogError<ObjectPool>($"Pool '{Data?.ObjectName}' não inicializado ou dados inválidos.", this);
                return null;
            }

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

            poolable.Reset();
            poolable.Activate(position);
            _activeObjects.Add(poolable);
            EventBus<SpawnTriggeredEvent>.Raise(new SpawnTriggeredEvent(Data.ObjectName, position));
            return poolable;
        }

        public void ReturnObject(IPoolable poolable)
        {
            if (poolable == null || !_activeObjects.Remove(poolable)) return;

            var wasEmpty = _pool.Count == 0;
            poolable.Deactivate();
            _pool.Enqueue(poolable);
            if (wasEmpty && _pool.Count > 0)
            {
                EventBus<PoolRestoredEvent>.Raise(new PoolRestoredEvent(Data.ObjectName));
            }
        }

        public void ClearPool()
        {
            foreach (var obj in _activeObjects)
            {
                if (obj != null && obj.GetGameObject() != null)
                    obj.Deactivate();
            }
            _activeObjects.Clear();
            _pool.Clear();
            IsInitialized = false;
        }

        public IReadOnlyList<IPoolable> GetActiveObjects() => _activeObjects;
        public int GetAvailableCount() => _pool.Count;
    }
}