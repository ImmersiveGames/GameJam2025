using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    [DefaultExecutionOrder(-50)]
    public class SpawnSystem : MonoBehaviour
    {
        [SerializeField] private ObjectPool[] pools; // Pools diretamente configurados
        [SerializeField] private MonoBehaviour triggerComponent; // Componente que implementa ITrigger
        [SerializeField] private MonoBehaviour strategyComponent; // Componente que implementa ISpawnStrategy
        [SerializeField] private MonoBehaviour actorComponent; // Componente que implementa IActor (opcional)
        [SerializeField] private bool exhaustOnSpawn = false; // Tentar exaurir o pool
        [SerializeField] private bool autoRespawnOnReturn = false; // Respawnar quando objetos retornam
        [SerializeField] private bool asyncInit = false; // Inicialização assíncrona dos pools
        [SerializeField] private Transform orbitCenter; // Centro da órbita para a estratégia (opcional)

        private ITrigger _trigger;
        private ISpawnStrategy _spawnStrategy;
        private IActor _actor;
        private bool _isInitialized;
        private EventBinding<PoolObjectReturnedEvent> _returnEventBinding;

        private void Awake()
        {
            ValidateComponents();

            // Autoinjeção de IActor se não configurado manualmente
            if (_actor == null)
            {
                _actor = GetComponent<IActor>();
                if (_actor != null)
                {
                    DebugUtility.LogVerbose<SpawnSystem>($"Auto-injetado IActor: {_actor.Name}", "cyan", this);
                }
            }
        }

        private void Start()
        {
            // Configura centro da órbita, se definido
            if (orbitCenter != null && _spawnStrategy != null)
            {
                _spawnStrategy.SetCenterTransform(orbitCenter);
            }

            if (asyncInit)
            {
                StartCoroutine(InitializePoolsAsync());
            }
            else
            {
                InitializePoolsSync();
            }
        }

        private void OnEnable()
        {
            if (_trigger != null)
            {
                _trigger.OnTriggered += HandleTrigger;
            }
            if (autoRespawnOnReturn)
            {
                _returnEventBinding = new EventBinding<PoolObjectReturnedEvent>(OnObjectReturned);
                EventBus<PoolObjectReturnedEvent>.Register(_returnEventBinding);
            }
        }

        private void OnDisable()
        {
            if (_trigger != null)
            {
                _trigger.OnTriggered -= HandleTrigger;
            }
            if (_returnEventBinding != null)
            {
                EventBus<PoolObjectReturnedEvent>.Unregister(_returnEventBinding);
            }
        }

        private void ValidateComponents()
        {
            _trigger = triggerComponent as ITrigger;
            if (_trigger == null)
            {
                DebugUtility.LogError<SpawnSystem>("Trigger component must implement ITrigger.", this);
                enabled = false;
                return;
            }

            _spawnStrategy = strategyComponent as ISpawnStrategy;
            if (_spawnStrategy == null)
            {
                DebugUtility.LogError<SpawnSystem>("Strategy component must implement ISpawnStrategy.", this);
                enabled = false;
                return;
            }

            _actor = actorComponent as IActor;
            if (actorComponent != null && _actor == null)
            {
                DebugUtility.LogError<SpawnSystem>("Actor component must implement IActor.", this);
                enabled = false;
                return;
            }
            if (_actor != null)
            {
                DebugUtility.LogVerbose<SpawnSystem>($"Actor configured: {_actor.Name}", "cyan", this);
            }
        }

        private void InitializePoolsSync()
        {
            foreach (var pool in pools)
            {
                if (pool != null && !pool.IsInitialized)
                {
                    pool.Initialize();
                }
            }
            _isInitialized = true;
            DebugUtility.Log<SpawnSystem>($"Pools initialized synchronously. Total pools: {pools.Length}", "green", this);
        }

        private IEnumerator InitializePoolsAsync()
        {
            foreach (var pool in pools)
            {
                if (pool != null && !pool.IsInitialized)
                {
                    pool.Initialize();
                }
                yield return null;
            }
            _isInitialized = true;
            DebugUtility.Log<SpawnSystem>($"Pools initialized asynchronously. Total pools: {pools.Length}", "green", this);
        }

        private void HandleTrigger()
        {
            if (!_isInitialized)
            {
                DebugUtility.LogWarning<SpawnSystem>("Cannot spawn, pools not initialized.", this);
                return;
            }

            foreach (var pool in pools)
            {
                if (pool == null) continue;
                _spawnStrategy.Execute(pool, transform, exhaustOnSpawn, _actor, this);
            }
        }

        private void OnObjectReturned(PoolObjectReturnedEvent evt)
        {
            if (!_isInitialized || !autoRespawnOnReturn) return;

            foreach (var pool in pools)
            {
                /*if (pool != null && pool.PoolKey == evt.PoolKey)
                {
                    _spawnStrategy.Execute(pool, transform, exhaustOnSpawn, _actor, this);
                    break;
                }*/
            }
        }

        public void ReturnAllToPool()
        {
            foreach (var pool in pools)
            {
                if (pool != null)
                {
                    pool.ClearPool();
                }
            }
        }

        public List<IPoolable> GetActiveObjects()
        {
            var activeObjects = new List<IPoolable>();
            foreach (var pool in pools)
            {
                if (pool != null)
                {
                    activeObjects.AddRange(pool.GetActiveObjects());
                }
            }
            return activeObjects;
        }

        public void ResetPool()
        {
            foreach (var pool in pools)
            {
                if (pool != null)
                {
                    pool.ClearPool();
                }
            }
            _isInitialized = false;
        }
    }
}