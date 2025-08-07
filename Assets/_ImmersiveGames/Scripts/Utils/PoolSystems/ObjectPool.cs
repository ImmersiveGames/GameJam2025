using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class ObjectPool : MonoBehaviour
    {
        private readonly Queue<IPoolable> _pool = new();
        private readonly List<IPoolable> _activeObjects = new();
        private PoolData Data { get; set; }
        public bool IsInitialized { get; private set; }
        private readonly ObjectPoolFactory _factory = new();

        public UnityEvent<IPoolable> OnObjectActivated { get; } = new UnityEvent<IPoolable>();
        public UnityEvent<IPoolable> OnObjectReturned { get; } = new UnityEvent<IPoolable>();

        public void SetData(PoolData data)
        {
            if (!PoolValidationUtility.ValidatePoolData(data, this))
                return;
            Data = data;
        }

        public void Initialize()
        {
            if (!Data)
            {
                DebugUtility.LogError<ObjectPool>("Dados não configurados para o pool.", this);
                return;
            }

            for (int i = 0; i < Data.InitialPoolSize; i++)
            {
                var config = Data.ObjectConfigs[i % Data.ObjectConfigs.Length];
                var poolable = _factory.CreateObject(config, transform, Vector3.zero, $"{Data.ObjectName}_{i}", this);
                if (poolable == null)
                {
                    DebugUtility.LogError<ObjectPool>($"Falha ao criar objeto {i} para pool '{Data.ObjectName}'.", this);
                    continue;
                }
                poolable.Deactivate();
                _pool.Enqueue(poolable);
                DebugUtility.LogVerbose<ObjectPool>($"Objeto {i} criado e enfileirado para '{Data.ObjectName}' com config {config.name}.", "cyan", this);
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
                var configIndex = (_activeObjects.Count + _pool.Count) % Data.ObjectConfigs.Length;
                var config = Data.ObjectConfigs[configIndex];
                poolable = _factory.CreateObject(config, transform, position, $"{Data.ObjectName}_{_activeObjects.Count + _pool.Count}", this);
            }
            else
            {
                if (_activeObjects.Count > 0)
                {
                    poolable = _activeObjects.OrderBy(obj => obj.GetGameObject().GetInstanceID()).First();
                    poolable.Deactivate();
                    _activeObjects.Remove(poolable);
                }
            }

            if (poolable == null)
            {
                DebugUtility.Log<ObjectPool>($"Nenhum objeto disponível no pool '{Data.ObjectName}'.", "yellow", this);
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(Data.ObjectName));
                return null;
            }

            poolable.PoolableReset();
            poolable.Activate(position, null);
            _activeObjects.Add(poolable);
            OnObjectActivated.Invoke(poolable);
            return poolable;
        }

        public List<IPoolable> GetMultipleObjects(int count, Vector3 position)
        {
            if (!IsInitialized || !Data)
            {
                DebugUtility.LogError<ObjectPool>($"Pool '{Data?.ObjectName}' não inicializado ou dados inválidos.", this);
                return new List<IPoolable>();
            }

            var result = new List<IPoolable>();
            bool wasEmpty = _pool.Count == 0;

            for (int i = 0; i < count; i++)
            {
                IPoolable poolable = null;
                if (_pool.Count > 0)
                {
                    poolable = _pool.Dequeue();
                }
                else if (Data.CanExpand)
                {
                    var configIndex = (_activeObjects.Count + _pool.Count + i) % Data.ObjectConfigs.Length;
                    var config = Data.ObjectConfigs[configIndex];
                    poolable = _factory.CreateObject(config, transform, position, $"{Data.ObjectName}_{_activeObjects.Count + _pool.Count + i}", this);
                }
                else
                {
                    if (_activeObjects.Count > 0)
                    {
                        poolable = _activeObjects.OrderBy(obj => obj.GetGameObject().GetInstanceID()).First();
                        poolable.Deactivate();
                        _activeObjects.Remove(poolable);
                    }
                }

                if (poolable == null)
                {
                    if (!wasEmpty)
                    {
                        DebugUtility.Log<ObjectPool>($"Pool '{Data.ObjectName}' exausto após obter {result.Count} objetos.", "yellow", this);
                        EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(Data.ObjectName));
                    }
                    break;
                }

                poolable.PoolableReset();
                poolable.Activate(position, null);
                result.Add(poolable);
                _activeObjects.Add(poolable);
                OnObjectActivated.Invoke(poolable);
            }

            if (wasEmpty && _pool.Count > 0)
            {
                EventBus<PoolRestoredEvent>.Raise(new PoolRestoredEvent(Data.ObjectName));
            }

            DebugUtility.Log<ObjectPool>($"Obtidos {result.Count} objetos do pool '{Data.ObjectName}'.", "green", this);
            return result;
        }

        public void ReturnObject(IPoolable poolable)
        {
            if (poolable == null || !_activeObjects.Contains(poolable)) return;

            bool wasEmpty = _pool.Count == 0;
            _activeObjects.Remove(poolable);
            poolable.Deactivate();
            poolable.GetGameObject().transform.SetParent(transform); // Único ponto de reparenting
            _pool.Enqueue(poolable);
            OnObjectReturned.Invoke(poolable);
            if (wasEmpty && _pool.Count > 0)
            {
                EventBus<PoolRestoredEvent>.Raise(new PoolRestoredEvent(Data.ObjectName));
            }
            DebugUtility.LogVerbose<ObjectPool>($"Objeto '{poolable.GetGameObject().name}' retornado ao pool '{Data.ObjectName}', Parent: {transform.name}.", "blue", this);
        }

        public void ClearPool()
        {
            foreach (var obj in _activeObjects.ToList())
            {
                if (obj != null && obj.GetGameObject() != null)
                {
                    obj.Deactivate();
                    obj.GetGameObject().transform.SetParent(transform);
                }
            }
            _activeObjects.Clear();
            _pool.Clear();
            IsInitialized = false;
        }

        public IReadOnlyList<IPoolable> GetActiveObjects()
        {
            return _activeObjects.ToList().AsReadOnly();
        }

        public int GetAvailableCount() => _pool.Count;
    }
}