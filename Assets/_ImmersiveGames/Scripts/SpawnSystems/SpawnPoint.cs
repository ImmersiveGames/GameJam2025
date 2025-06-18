using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private PoolableObjectData poolableData;
        [SerializeField] public bool useManagerLocking = true;
        [SerializeField] protected EnhancedTriggerData triggerData;
        [SerializeField] private EnhancedStrategyData strategyData;

        private string _poolKey;
        private bool _isExhausted;
        private bool _isSpawnValid;
        private ObjectPool _cachedPool;
        private Vector3 _cachedPosition;
        private Vector3 _cachedForward;
        private EventBinding<SpawnRequestEvent> _spawnBinding;
        private EventBinding<PoolExhaustedEvent> _exhaustedBinding;
        private EventBinding<PoolRestoredEvent> _restoredBinding;
        private EventBinding<SpawnPointLockedEvent> _lockedBinding;
        private EventBinding<SpawnPointUnlockedEvent> _unlockedBinding;
        private EventBinding<SpawnPointResetEvent> _resetBinding;

        protected SpawnManager spawnManager;
        protected PoolManager poolManager;
        protected ISpawnStrategy SpawnStrategy { get; private set; }
        public ISpawnTrigger SpawnTrigger { get; protected set; }

        protected virtual void Awake()
        {
            if (!poolableData || string.IsNullOrEmpty(poolableData.ObjectName))
            {
                DebugUtility.LogError<SpawnPoint>("PoolableObjectData inválido. Verifique a configuração no Inspector.", this);
                return;
            }

            spawnManager = SpawnManager.Instance;
            poolManager = PoolManager.Instance;
            if (!spawnManager)
            {
                DebugUtility.LogError<SpawnPoint>("SpawnManager.Instance não encontrado. Certifique-se de que existe um SpawnManager na cena.", this);
                return;
            }
            if (!poolManager)
            {
                DebugUtility.LogError<SpawnPoint>("PoolManager.Instance não encontrado. Certifique-se de que existe um PoolManager na cena.", this);
                return;
            }

            _poolKey = poolableData.ObjectName;
            _cachedPosition = transform.position;
            _cachedForward = transform.forward;

            InitializeStrategy();
            InitializeTrigger();
            spawnManager.RegisterSpawnPoint(this, useManagerLocking);

            _spawnBinding = new EventBinding<SpawnRequestEvent>(HandleSpawnRequest);
            _exhaustedBinding = new EventBinding<PoolExhaustedEvent>(HandlePoolExhausted);
            _restoredBinding = new EventBinding<PoolRestoredEvent>(HandlePoolRestored);
            _lockedBinding = new EventBinding<SpawnPointLockedEvent>(HandleSpawnPointLocked);
            _unlockedBinding = new EventBinding<SpawnPointUnlockedEvent>(HandleSpawnPointUnlocked);
            _resetBinding = new EventBinding<SpawnPointResetEvent>(HandleSpawnPointReset);

            UpdateSpawnState();
            if (_isSpawnValid)
                DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' inicializado com trigger '{triggerData?.triggerType}' e estratégia '{strategyData?.strategyType}'.", "green", this);
        }

        private void Start()
        {
            _cachedPool = poolManager.GetPool(_poolKey);
            if (!_cachedPool)
            {
                DebugUtility.LogError<SpawnPoint>($"Pool '{_poolKey}' não foi registrado ou inicializado. Verifique o PoolableObjectData.", this);
            }
            UpdateSpawnState();
        }

        private void InitializeStrategy()
        {
            SpawnStrategy = EnhancedSpawnFactory.Instance.CreateStrategy(strategyData);
            if (SpawnStrategy == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Falha ao criar estratégia para {strategyData?.strategyType}.", this);
            }
        }

        protected virtual void InitializeTrigger()
        {
            SpawnTrigger = EnhancedSpawnFactory.Instance.CreateTrigger(triggerData);
            if (SpawnTrigger == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Falha ao criar trigger para {triggerData?.triggerType}.", this);
                return;
            }
            SpawnTrigger.Initialize(this);
        }

        protected virtual void OnEnable()
        {
            EventBus<SpawnRequestEvent>.Register(_spawnBinding);
            EventBus<PoolExhaustedEvent>.Register(_exhaustedBinding);
            EventBus<PoolRestoredEvent>.Register(_restoredBinding);
            EventBus<SpawnPointLockedEvent>.Register(_lockedBinding);
            EventBus<SpawnPointUnlockedEvent>.Register(_unlockedBinding);
            EventBus<SpawnPointResetEvent>.Register(_resetBinding);
        }

        protected virtual void OnDisable()
        {
            EventBus<SpawnRequestEvent>.Unregister(_spawnBinding);
            EventBus<PoolExhaustedEvent>.Unregister(_exhaustedBinding);
            EventBus<PoolRestoredEvent>.Unregister(_restoredBinding);
            EventBus<SpawnPointLockedEvent>.Unregister(_lockedBinding);
            EventBus<SpawnPointUnlockedEvent>.Unregister(_unlockedBinding);
            EventBus<SpawnPointResetEvent>.Unregister(_resetBinding);
        }

        protected virtual void Update()
        {
            if (!_isSpawnValid || SpawnTrigger == null)
                return;

            UpdateTransformCache();
            SpawnTrigger.CheckTrigger(_cachedPosition);
        }

        private void UpdateTransformCache()
        {
            _cachedPosition = transform.position;
            _cachedForward = transform.forward;
        }

        protected virtual void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            DebugUtility.Log<SpawnPoint>($"Recebido SpawnRequestEvent para pool '{evt.PoolKey}' de origem {(evt.SourceGameObject != null ? evt.SourceGameObject.name : "desconhecida")}", "blue", this);
            if (evt.PoolKey != _poolKey || !_isSpawnValid || evt.SourceGameObject != gameObject)
            {
                DebugUtility.Log<SpawnPoint>($"SpawnRequest ignorado: PoolKey mismatch, _isSpawnValid={_isSpawnValid}, ou SourceGameObject não é '{name}'", "yellow", this);
                return;
            }

            if (!_cachedPool)
            {
                DebugUtility.LogError<SpawnPoint>($"Pool '{_poolKey}' não encontrado.", this);
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, _cachedPosition));
                return;
            }

            var availableCount = _cachedPool.GetAvailableCount();
            if (availableCount <= 0)
            {
                if (!useManagerLocking)
                    _isExhausted = true;
                UpdateSpawnState();
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(_poolKey));
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, _cachedPosition));
                DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' esgotado para '{name}'.", "yellow", this);
                return;
            }

            SpawnStrategy?.Spawn(_cachedPool, _cachedPosition, _cachedForward);
            if (useManagerLocking)
                spawnManager?.RegisterSpawn(this);
            EventBus<SpawnTriggeredEvent>.Raise(new SpawnTriggeredEvent(_poolKey, _cachedPosition));
            DebugUtility.Log<SpawnPoint>($"Spawn executado em '{name}' na posição {_cachedPosition}.", "green", this);
        }

        private void HandlePoolExhausted(PoolExhaustedEvent evt)
        {
            if (evt.PoolKey != _poolKey || useManagerLocking)
                return;

            _isExhausted = true;
            UpdateSpawnState();
            DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' esgotado para '{name}' (independente).", "yellow", this);
        }

        private void HandlePoolRestored(PoolRestoredEvent evt)
        {
            if (evt.PoolKey != _poolKey || useManagerLocking)
                return;

            if (_isExhausted && _cachedPool != null && _cachedPool.GetAvailableCount() > 0)
            {
                _isExhausted = false;
                UpdateSpawnState();
                DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' restaurado para '{name}'.", "green", this);
            }
        }

        private void HandleSpawnPointLocked(SpawnPointLockedEvent evt)
        {
            if (evt.Point != this)
                return;

            _isExhausted = true;
            UpdateSpawnState();
            DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' bloqueado pelo SpawnManager.", "yellow", this);
        }

        private void HandleSpawnPointUnlocked(SpawnPointUnlockedEvent evt)
        {
            if (evt.Point != this)
                return;

            _isExhausted = false;
            UpdateSpawnState();
            DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' desbloqueado pelo SpawnManager.", "green", this);
        }

        private void HandleSpawnPointReset(SpawnPointResetEvent evt)
        {
            if (evt.Point != this)
                return;

            _isExhausted = false;
            SpawnTrigger?.SetActive(true);
            SpawnTrigger?.Reset();
            UpdateSpawnState();
            DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' resetado pelo SpawnManager.", "green", this);
        }

        private void UpdateSpawnState()
        {
            _isSpawnValid = spawnManager.CanSpawn(this) && (!_isExhausted || useManagerLocking) && SpawnTrigger != null && _cachedPool != null;
        }

        public void SetStrategyData(EnhancedStrategyData newData)
        {
            if (newData == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Novo StrategyData é nulo para '{name}'.", this);
                return;
            }

            strategyData = newData;
            SpawnStrategy = EnhancedSpawnFactory.Instance.CreateStrategy(strategyData);
            DebugUtility.Log<SpawnPoint>($"Estratégia de '{name}' atualizada para '{strategyData?.strategyType}'.", "green", this);
        }

        public virtual void TriggerReset()
        {
            spawnManager?.ResetSpawnPoint(this);
        }

        public void SetTriggerActive(bool active)
        {
            SpawnTrigger?.SetActive(active);
            UpdateSpawnState();
            DebugUtility.Log<SpawnPoint>($"Trigger de '{name}' {(active ? "ativado" : "desativado")}.", "yellow", this);
        }

        public string GetPoolKey() => _poolKey;
        public PoolableObjectData GetPoolableData() => poolableData;
        public EnhancedTriggerData GetTriggerData() => triggerData;
        public bool IsSpawnValid() => _isSpawnValid;
        public bool GetTriggerActive() => SpawnTrigger?.IsActive ?? false;
    }
}