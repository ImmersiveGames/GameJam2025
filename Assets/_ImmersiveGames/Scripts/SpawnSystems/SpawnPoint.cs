﻿using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private PoolableObjectData poolableData;
        [SerializeField] public bool useManagerLocking = true;
        [SerializeField] protected EnhancedTriggerData triggerData;
        [SerializeField] private EnhancedStrategyData strategyData;

        private string _poolKey;
        private bool _isExhausted;
        private ObjectPool _cachedPool;
        private Vector3 _cachedPosition;
        private EventBinding<SpawnRequestEvent> _spawnBinding;
        private EventBinding<PoolExhaustedEvent> _exhaustedBinding;
        private EventBinding<PoolRestoredEvent> _restoredBinding;
        private EventBinding<SpawnPointLockedEvent> _lockedBinding;
        private EventBinding<SpawnPointUnlockedEvent> _unlockedBinding;
        private EventBinding<SpawnPointResetEvent> _resetBinding;

        private SpawnManager _spawnManager;
        private PoolManager _poolManager;
        private ISpawnStrategy SpawnStrategy { get; set; }
        public ISpawnTrigger SpawnTrigger { get; protected set; }
        private Vector3? _pendingTriggerPosition;
        private GameObject _pendingSourceObject;

        protected virtual void Awake()
        {
            if (!poolableData || string.IsNullOrEmpty(poolableData.ObjectName))
            {
                DebugUtility.LogError<SpawnPoint>("PoolableObjectData inválido.", this);
                enabled = false;
                return;
            }

            _spawnManager = SpawnManager.Instance;
            _poolManager = PoolManager.Instance;
            if (!_spawnManager || !_poolManager)
            {
                DebugUtility.LogError<SpawnPoint>("SpawnManager ou PoolManager não encontrado.", this);
                enabled = false;
                return;
            }

            _poolKey = poolableData.ObjectName;
            UpdateTransformCache();

            InitializeStrategy();
            InitializeTrigger();
            _spawnManager.RegisterSpawnPoint(this, useManagerLocking);

            _spawnBinding = new EventBinding<SpawnRequestEvent>(HandleSpawnRequest);
            _exhaustedBinding = new EventBinding<PoolExhaustedEvent>(HandlePoolExhausted);
            _restoredBinding = new EventBinding<PoolRestoredEvent>(HandlePoolRestored);
            _lockedBinding = new EventBinding<SpawnPointLockedEvent>(HandleSpawnPointLocked);
            _unlockedBinding = new EventBinding<SpawnPointUnlockedEvent>(HandleSpawnPointUnlocked);
            _resetBinding = new EventBinding<SpawnPointResetEvent>(HandleSpawnPointReset);
        }

        private void Start()
        {
            _cachedPool = _poolManager.GetPool(_poolKey);
            if (!_cachedPool)
            {
                DebugUtility.LogError<SpawnPoint>($"Pool '{_poolKey}' não registrado.", this);
                enabled = false;
            }
            UpdateSpawnState();
            if (enabled)
                DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' inicializado com trigger '{triggerData?.triggerType}' e estratégia '{strategyData?.strategyType}'.", "green", this);
        }

        private void InitializeStrategy()
        {
            SpawnStrategy = EnhancedSpawnFactory.Instance.CreateStrategy(strategyData);
            if (SpawnStrategy == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Falha ao criar estratégia para {strategyData?.strategyType}.", this);
                enabled = false;
            }
        }

        protected virtual void InitializeTrigger()
        {
            SpawnTrigger = EnhancedSpawnFactory.Instance.CreateTrigger(triggerData);
            if (SpawnTrigger == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Falha ao criar trigger para {triggerData?.triggerType}.", this);
                enabled = false;
                return;
            }
            SpawnTrigger.Initialize(this);
        }

        protected virtual void OnEnable()
        {
            FilteredEventBus<SpawnRequestEvent>.Register(_spawnBinding, this);
            EventBus<PoolExhaustedEvent>.Register(_exhaustedBinding);
            EventBus<PoolRestoredEvent>.Register(_restoredBinding);
            EventBus<SpawnPointLockedEvent>.Register(_lockedBinding);
            EventBus<SpawnPointUnlockedEvent>.Register(_unlockedBinding);
            EventBus<SpawnPointResetEvent>.Register(_resetBinding);
        }

        protected virtual void OnDisable()
        {
            FilteredEventBus<SpawnRequestEvent>.Unregister(this);
            EventBus<PoolExhaustedEvent>.Unregister(_exhaustedBinding);
            EventBus<PoolRestoredEvent>.Unregister(_restoredBinding);
            EventBus<SpawnPointLockedEvent>.Unregister(_lockedBinding);
            EventBus<SpawnPointUnlockedEvent>.Unregister(_unlockedBinding);
            EventBus<SpawnPointResetEvent>.Unregister(_resetBinding);
        }

        private void Update()
        {
            if(!GameManager.Instance.ShouldPlayingGame())return;
            if (!IsSpawnValid || SpawnTrigger == null)
                return;

            UpdateTransformCache();
            if (SpawnTrigger.CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject))
            {
                _pendingTriggerPosition = triggerPosition;
                _pendingSourceObject = sourceObject ?? gameObject;
                DebugUtility.LogVerbose<SpawnPoint>($"Trigger detectado em Update para '{name}'. Posição: {triggerPosition}, SourceObject: {_pendingSourceObject?.name ?? "null"}", "cyan");
            }
            else
            {
                _pendingTriggerPosition = null;
                _pendingSourceObject = null;
            }
        }

        private void LateUpdate()
        {
            if(!GameManager.Instance.ShouldPlayingGame())return;
            if (!IsSpawnValid || _pendingTriggerPosition == null || _pendingSourceObject == null)
                return;

            Vector3 spawnPosition = _pendingTriggerPosition ?? _cachedPosition;
            ExecuteSpawn(spawnPosition, _pendingSourceObject);
        }

        private void UpdateTransformCache()
        {
            _cachedPosition = transform.position;
        }

        protected virtual void ExecuteSpawn(Vector3 position, GameObject sourceObject)
        {
            if (!_cachedPool)
            {
                DebugUtility.LogError<SpawnPoint>($"Pool '{_poolKey}' não encontrado.", this);
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, position));
                return;
            }

            if (_cachedPool.GetAvailableCount() <= 0)
            {
                if (!useManagerLocking)
                    _isExhausted = true;
                UpdateSpawnState();
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(_poolKey));
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, position));
                DebugUtility.LogVerbose<SpawnPoint>($"Pool '{_poolKey}' esgotado para '{name}'.", "yellow", this);
                return;
            }
            DebugUtility.LogVerbose<SpawnPoint>($"Executando Spawn para '{name}' com sourceObject '{sourceObject?.name ?? "null"}'.", "blue");
            SpawnStrategy?.Spawn(_cachedPool, position, sourceObject);

            if (useManagerLocking)
                _spawnManager?.RegisterSpawn(this);
            EventBus<SpawnTriggeredEvent>.Raise(new SpawnTriggeredEvent(_poolKey, position));
            DebugUtility.LogVerbose<SpawnPoint>($"Spawn executado em '{name}' na posição {position} com sourceObject {sourceObject?.name ?? "null"}.", "green", this);
        }

        private void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (evt.PoolKey != _poolKey || !IsSpawnValid || evt.SourceGameObject != gameObject)
                return;
            DebugUtility.LogVerbose<SpawnPoint>($"Recebido SpawnRequestEvent para pool '{evt.PoolKey}' de origem {(evt.SourceGameObject != null ? evt.SourceGameObject.name : "desconhecida")}", "blue", this);

            Vector3 spawnPosition = evt.Position ?? _cachedPosition;
            ExecuteSpawn(spawnPosition, evt.SourceGameObject ?? gameObject);
        }

        private void HandlePoolExhausted(PoolExhaustedEvent evt)
        {
            if (evt.PoolKey != _poolKey || useManagerLocking)
                return;

            _isExhausted = true;
            UpdateSpawnState();
            DebugUtility.LogVerbose<SpawnPoint>($"Pool '{_poolKey}' esgotado para '{name}'.", "yellow", this);
        }

        private void HandlePoolRestored(PoolRestoredEvent evt)
        {
            if (evt.PoolKey != _poolKey || useManagerLocking)
                return;

            if (_isExhausted && _cachedPool != null && _cachedPool.GetAvailableCount() > 0)
            {
                _isExhausted = false;
                UpdateSpawnState();
                DebugUtility.LogVerbose<SpawnPoint>($"Pool '{_poolKey}' restaurado para '{name}'.", "green", this);
            }
        }

        private void HandleSpawnPointLocked(SpawnPointLockedEvent evt)
        {
            if (evt.Point != this)
                return;

            _isExhausted = true;
            UpdateSpawnState();
            DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' bloqueado.", "yellow", this);
        }

        private void HandleSpawnPointUnlocked(SpawnPointUnlockedEvent evt)
        {
            if (evt.Point != this)
                return;

            _isExhausted = false;
            UpdateSpawnState();
            DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' desbloqueado.", "green", this);
        }

        private void HandleSpawnPointReset(SpawnPointResetEvent evt)
        {
            if (evt.Point != this)
                return;

            _isExhausted = false;
            SpawnTrigger?.SetActive(true);
            SpawnTrigger?.Reset();
            UpdateSpawnState();
            DebugUtility.LogVerbose<SpawnPoint>($"SpawnPoint '{name}' resetado.", "green", this);
        }

        private void UpdateSpawnState()
        {
            IsSpawnValid = _spawnManager.CanSpawn(this) && (!_isExhausted || useManagerLocking) && SpawnTrigger != null && SpawnStrategy != null && _cachedPool != null;
        }

        public void SetStrategyData(EnhancedStrategyData newData)
        {
            if (newData == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Novo StrategyData é nulo.", this);
                return;
            }

            strategyData = newData;
            SpawnStrategy = EnhancedSpawnFactory.Instance.CreateStrategy(strategyData);
            UpdateSpawnState();
            DebugUtility.LogVerbose<SpawnPoint>($"Estratégia atualizada para '{strategyData?.strategyType}'.", "green", this);
        }

        public virtual void TriggerReset()
        {
            _spawnManager?.ResetSpawnPoint(this);
        }

        public void SetTriggerActive(bool active)
        {
            SpawnTrigger?.SetActive(active);
            UpdateSpawnState();
            DebugUtility.LogVerbose<SpawnPoint>($"Trigger {(active ? "ativado" : "desativado")}.", "yellow", this);
        }

        public string GetPoolKey() => _poolKey;
        public PoolableObjectData GetPoolableData() => poolableData;
        public EnhancedTriggerData GetTriggerData() => triggerData;
        public bool IsSpawnValid { get; private set; }
        public bool GetTriggerActive() => SpawnTrigger?.IsActive ?? false;
    }
}