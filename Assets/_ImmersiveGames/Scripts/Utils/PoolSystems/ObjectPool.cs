using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    /// <summary>
    /// Pool de objetos para otimizar criação e destruição, com suporte a expansão e reconfiguração.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public class ObjectPool : MonoBehaviour
    {
        private readonly Queue<IPoolable> _pool = new();
        private readonly List<IPoolable> _activeObjects = new();

        private PoolData Data { get; set; }
        public bool IsInitialized { get; private set; }

        private IObjectPoolFactory _factory;
        private int _lastGetFrame;
        private bool _allowMultipleGetsInFrame = true;
        private bool _hasWarnedExhausted;

        public UnityEvent<IPoolable> OnObjectActivated { get; } = new();
        public UnityEvent<IPoolable> OnObjectReturned { get; } = new();

        private void Awake() => _factory = new ObjectPoolFactory();

        /// <summary>Define a fábrica usada para criar objetos do pool.</summary>
        public void SetFactory(IObjectPoolFactory factory) => _factory = factory ?? new ObjectPoolFactory();

        /// <summary>Define se o pool permite múltiplos GetObject no mesmo frame.</summary>
        public void SetAllowMultipleGetsInFrame(bool allow) => _allowMultipleGetsInFrame = allow;

        /// <summary>Define os dados do pool.</summary>
        public void SetData(PoolData data)
        {
            if (!PoolData.Validate(data, this))
            {
                DebugUtility.LogError<ObjectPool>("Invalid PoolData.", this);
                return;
            }
            Data = data;
        }

        /// <summary>Inicializa o pool com objetos de acordo com o PoolData.</summary>
        public void Initialize()
        {
            if (!Data || _factory == null)
            {
                DebugUtility.LogError<ObjectPool>("Cannot initialize pool: missing Data or Factory.", this);
                return;
            }

            for (int i = 0; i < Data.InitialPoolSize; i++)
            {
                var poolable = CreatePoolable(i, Vector3.zero, null);
                if (poolable != null) _pool.Enqueue(poolable);
            }

            IsInitialized = _pool.Count > 0;
        }

        /// <summary>Obtém um objeto do pool.</summary>
        public IPoolable GetObject(Vector3 position, IActor spawner = null, bool activateImmediately = true)
        {
            if (!ValidatePool()) return null;
            WarnIfMultipleGetsSameFrame();

            var poolable = RetrieveOrCreate(position, spawner);
            if (poolable == null) return null;

            if (activateImmediately)
            {
                ActivatePoolable(poolable, position, spawner);
            }

            return poolable;
        }

        /// <summary>Obtém múltiplos objetos do pool.</summary>
        public List<IPoolable> GetMultipleObjects(int count, Vector3 position, IActor spawner = null, bool activateImmediately = true)
        {
            if (!ValidatePool()) return new();

            var result = new List<IPoolable>();
            int maxCount = Data.CanExpand ? count : Mathf.Min(count, _pool.Count);

            for (int i = 0; i < maxCount; i++)
            {
                var poolable = RetrieveOrCreate(position, spawner);
                if (poolable == null) break;

                if (activateImmediately)
                {
                    ActivatePoolable(poolable, position, spawner);
                }

                result.Add(poolable);
            }

            if (result.Count > 0) _hasWarnedExhausted = false;
            return result;
        }

        /// <summary>Ativa manualmente um objeto do pool.</summary>
        public void ActivateObject(IPoolable poolable, Vector3 position, IActor spawner = null)
        {
            if (poolable == null || _activeObjects.Contains(poolable)) return;

            ActivatePoolable(poolable, position, spawner);
        }

        /// <summary>Retorna um objeto para o pool.</summary>
        public void ReturnObject(IPoolable poolable)
        {
            if (poolable == null || !_activeObjects.Contains(poolable)) return;

            _activeObjects.Remove(poolable);
            if (poolable.GetGameObject().activeSelf)
                poolable.Deactivate();

            poolable.GetGameObject().transform.SetParent(transform);
            _pool.Enqueue(poolable);
            OnObjectReturned.Invoke(poolable);

            if (Data.ReconfigureOnReturn) ReconfigureObject(poolable);

            _hasWarnedExhausted = false;
        }

        /// <summary>Expande o pool criando novos objetos.</summary>
        public void ExpandPool(int additionalCount)
        {
            if (!Data.CanExpand)
            {
                DebugUtility.LogWarning<ObjectPool>($"Cannot expand pool '{Data.ObjectName}', expansion disabled.", this);
                return;
            }

            for (int i = 0; i < additionalCount; i++)
            {
                var poolable = CreatePoolable(_activeObjects.Count + _pool.Count, Vector3.zero, null);
                if (poolable != null) _pool.Enqueue(poolable);
            }
        }

        /// <summary>Destrói todos os objetos do pool.</summary>
        public void ClearPool()
        {
            foreach (var obj in _activeObjects) DestroyObject(obj);
            _activeObjects.Clear();

            foreach (var obj in _pool) DestroyObject(obj);
            _pool.Clear();

            IsInitialized = false;
        }

        // -----------------------------
        // Métodos auxiliares
        // -----------------------------

        private IPoolable RetrieveOrCreate(Vector3 position, IActor spawner)
        {
            if (_pool.Count > 0) return PreparePoolable(_pool.Dequeue());

            if (Data.CanExpand)
            {
                return CreatePoolable(_activeObjects.Count + _pool.Count, position, spawner);
            }

            if (!_hasWarnedExhausted)
            {
                DebugUtility.LogWarning<ObjectPool>($"Pool '{Data.ObjectName}' exhausted.", this);
                _hasWarnedExhausted = true;
            }

            return null;
        }

        private IPoolable CreatePoolable(int index, Vector3 position, IActor spawner)
        {
            var config = GetConfigForIndex(index);
            var poolable = _factory.CreateObject(config, transform, position, $"{Data.ObjectName}_{index}", this, spawner);
            return poolable;
        }

        private IPoolable PreparePoolable(IPoolable poolable)
        {
            if (poolable == null) return null;
            poolable.PoolableReset();
            return poolable;
        }

        private void ActivatePoolable(IPoolable poolable, Vector3 position, IActor spawner)
        {
            poolable.Activate(new Vector3(position.x, 0, position.z), spawner);
            _activeObjects.Add(poolable);
            OnObjectActivated.Invoke(poolable);
        }

        private void ReconfigureObject(IPoolable poolable)
        {
            var config = GetConfigForIndex(_activeObjects.Count + _pool.Count - 1);
            poolable.Reconfigure(config);
        }

        private void DestroyObject(IPoolable poolable)
        {
            if (poolable == null) return;
            LifetimeManager.Instance?.Unregister(poolable);
            if (poolable.GetGameObject() != null)
                Destroy(poolable.GetGameObject());
        }

        private PoolableObjectData GetConfigForIndex(int index)
        {
            return Data.ReconfigureOnReturn
                ? Data.ObjectConfigs[index % Data.ObjectConfigs.Length]
                : Data.ObjectConfigs[0];
        }

        private bool ValidatePool()
        {
            if (IsInitialized && Data) return true;
            DebugUtility.LogWarning<ObjectPool>($"Pool '{name}' not initialized or missing data.", this);
            return false;
        }

        private void WarnIfMultipleGetsSameFrame()
        {
            if (!_allowMultipleGetsInFrame && Time.frameCount == _lastGetFrame)
            {
                DebugUtility.LogWarning<ObjectPool>("Multiple GetObject calls in the same frame.", this);
            }
            _lastGetFrame = Time.frameCount;
        }
    }
}
