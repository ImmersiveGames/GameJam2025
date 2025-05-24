using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class ObjectPool : MonoBehaviour
    {
        private PoolableObjectData _data;
        private readonly Queue<IPoolable> _pool = new Queue<IPoolable>();
        private readonly List<IPoolable> _activeObjects = new List<IPoolable>();

        public void SetData(PoolableObjectData data)
        {
            _data = data;
        }

        public void Initialize()
        {
            if (_data == null || _data.LogicPrefab == null || _data.ModelPrefab == null)
            {
                DebugUtility.LogError<ObjectPool>("PoolableObjectData, LogicPrefab ou ModelPrefab nulo.", this);
                return;
            }

            var factory = FactoryRegistry.GetFactory(_data.FactoryType);
            if (factory == null)
            {
                DebugUtility.LogError<ObjectPool>($"Fábrica não encontrada para '{_data.FactoryType}'.", this);
                return;
            }

            for (int i = 0; i < _data.InitialPoolSize; i++)
            {
                var obj = Instantiate(_data.LogicPrefab, Vector3.zero, Quaternion.identity, transform);
                obj.name = $"{_data.ObjectName}_{i}";
                var poolable = obj.GetComponent<IPoolable>();
                if (poolable == null)
                {
                    DebugUtility.LogError<ObjectPool>($"LogicPrefab '{_data.ObjectName}' não possui IPoolable.", this);
                    Destroy(obj);
                    continue;
                }
                factory.BuildStructure(obj, _data);
                poolable.Initialize(_data, this);
                poolable.Deactivate();
                _pool.Enqueue(poolable);
                DebugUtility.LogVerbose<ObjectPool>($"Objeto {obj.name} criado e adicionado ao pool. Total: {_pool.Count}.", "blue", this);
            }
            DebugUtility.Log<ObjectPool>($"Pool inicializado com {_pool.Count} objetos para '{_data.ObjectName}'.", "green", this);
        }

        public IPoolable GetObject(Vector3 position)
        {
            IPoolable poolable;
            if (_pool.Count > 0)
            {
                poolable = _pool.Dequeue();
            }
            else
            {
                var obj = Instantiate(_data.LogicPrefab, position, Quaternion.identity, transform);
                obj.name = $"{_data.ObjectName}_{_activeObjects.Count + _pool.Count}";
                var factory = FactoryRegistry.GetFactory(_data.FactoryType);
                poolable = obj.GetComponent<IPoolable>();
                if (poolable == null)
                {
                    DebugUtility.LogError<ObjectPool>($"Novo LogicPrefab '{_data.ObjectName}' não possui IPoolable.", this);
                    Destroy(obj);
                    return null;
                }
                factory.BuildStructure(obj, _data);
                poolable.Initialize(_data, this);
            }

            var go = (poolable as Component)?.gameObject;
            if (go != null)
            {
                poolable.Activate(position);
                _activeObjects.Add(poolable);
                DebugUtility.LogVerbose<ObjectPool>($"Objeto {go.name} ativado em {position}.", "blue", this);
            }
            return poolable;
        }

        public void ReturnObject(IPoolable poolable)
        {
            if (poolable == null) return;
            if (_activeObjects.Remove(poolable))
            {
                poolable.Deactivate();
                _pool.Enqueue(poolable);
                DebugUtility.LogVerbose<ObjectPool>($"Objeto retornado ao pool.", "blue", this);
            }
        }
    }
}