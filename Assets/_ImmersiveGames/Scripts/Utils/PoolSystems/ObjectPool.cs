using System.Collections.Generic;
using _ImmersiveGames.Scripts.SpawnSystem;
using _ImmersiveGames.Scripts.Utils.BusEventSystems; // Novo
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class ObjectPool : MonoBehaviour
    {
        private readonly Queue<IPoolable> _pool = new Queue<IPoolable>();
        private readonly List<IPoolable> _activeObjects = new List<IPoolable>();
        private PoolableObjectData Data { get; set; }
        private readonly ObjectPoolFactory _factory = new ObjectPoolFactory();

        public void SetData(PoolableObjectData data)
        {
            if (data == null || data.Prefab == null || data.ModelPrefab == null)
            {
                DebugUtility.LogError<ObjectPool>($"PoolableObjectData, LogicPrefab ou ModelPrefab nulo.", this);
                return;
            }
            Data = data;
        }

        public void Initialize()
        {
            if (Data == null)
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
                DebugUtility.LogVerbose<ObjectPool>($"Objeto {poolable.GetGameObject().name} criado. Total: {_pool.Count}.", "blue", this);
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
                DebugUtility.LogVerbose<ObjectPool>($"Objeto criado dinamicamente para '{Data.ObjectName}'.", "blue", this);
            }

            if (poolable == null)
            {
                DebugUtility.LogVerbose<ObjectPool>($"Nenhum objeto disponível no pool '{Data.ObjectName}'.", "yellow", this);
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(Data.ObjectName)); // Novo evento
                return null;
            }

            poolable.Activate(position);
            _activeObjects.Add(poolable);
            DebugUtility.LogVerbose<ObjectPool>($"Objeto {poolable.GetGameObject().name} ativado em {position}.", "blue", this);
            return poolable;
        }

        public void ReturnObject(IPoolable poolable)
        {
            if (poolable == null || !_activeObjects.Remove(poolable)) return;
            poolable.Deactivate();
            _pool.Enqueue(poolable);
            DebugUtility.LogVerbose<ObjectPool>($"Objeto retornado ao pool.", "blue", this);
        }

        public IReadOnlyList<IPoolable> GetActiveObjects() => _activeObjects;
    }
}