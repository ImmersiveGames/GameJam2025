using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] protected SpawnData spawnData;
        [SerializeField] public bool useManagerLocking = true;
        [SerializeField]
        protected TriggerData triggerData;
        [SerializeField] private StrategyData strategyData;

        protected ISpawnTrigger _trigger;
        private ISpawnStrategy _strategy;
        private string _poolKey;
        protected bool _isExhausted;
        private EventBinding<SpawnRequestEvent> _spawnBinding;
        private EventBinding<PoolExhaustedEvent> _exhaustedBinding;
        protected SpawnManager spawnManager;
        protected PoolManager poolManager;

        protected virtual void Awake()
        {
            if (!spawnData || !spawnData.PoolableData || string.IsNullOrEmpty(spawnData.PoolableData.ObjectName))
            {
                DebugUtility.LogError<SpawnPoint>("Configuração inválida no SpawnData.", this);
                enabled = false;
                return;
            }

            spawnManager = SpawnManager.Instance;
            poolManager = PoolManager.Instance;
            if (!spawnManager)
            {
                DebugUtility.LogError<SpawnPoint>("SpawnManager.Instance não encontrado.", this);
                enabled = false;
                return;
            }
            spawnManager.RegisterSpawnPoint(this, useManagerLocking);

            _poolKey = spawnData.PoolableData.ObjectName;
            InitializeStrategy();
            InitializeTrigger();
            _spawnBinding = new EventBinding<SpawnRequestEvent>(HandleSpawnRequest);
            _exhaustedBinding = new EventBinding<PoolExhaustedEvent>(HandlePoolExhausted);

            enabled = spawnManager.CanSpawn(this) && !_isExhausted && _trigger != null;
            if (enabled)
                DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' inicializado com trigger '{triggerData?.triggerType}' e estratégia '{strategyData?.strategyType}'.", "green", this);
        }

        private void InitializeStrategy()
        {
            _strategy = SpawnFactory.Instance.CreateStrategy(strategyData);
            if (_strategy == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Falha ao criar estratégia para {strategyData?.strategyType}.", this);
                enabled = false;
            }
        }

        protected virtual void InitializeTrigger()
        {
            if (triggerData?.triggerType is TriggerType.InputSystemTrigger or TriggerType.InputSystemHoldTrigger)
            {
                DebugUtility.LogError<SpawnPoint>($"Trigger '{triggerData.triggerType}' requer PlayerInputSpawnPoint.", this);
                enabled = false;
                return;
            }
            _trigger = SpawnFactory.Instance.CreateTrigger(triggerData);
            if (_trigger == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Falha ao criar trigger para {triggerData?.triggerType}.", this);
                enabled = false;
                return;
            }
            _trigger.Initialize(this);
        }

        private void Start()
        {
            if (!RegisterPoolIfNeeded())
                enabled = false;
        }

        protected virtual void OnEnable()
        {
            if (_spawnBinding != null)
                EventBus<SpawnRequestEvent>.Register(_spawnBinding);
            if (_exhaustedBinding != null)
                EventBus<PoolExhaustedEvent>.Register(_exhaustedBinding);
        }

        protected virtual void OnDisable()
        {
            if (_spawnBinding != null)
                EventBus<SpawnRequestEvent>.Unregister(_spawnBinding);
            if (_exhaustedBinding != null)
                EventBus<PoolExhaustedEvent>.Unregister(_exhaustedBinding);
        }

        private void Update()
        {
            if (!spawnManager.CanSpawn(this) || _isExhausted)
            {
                enabled = false;
                return;
            }

            if (!useManagerLocking && _isExhausted)
            {
                var pool = poolManager.GetPool(_poolKey);
                if (pool && pool.GetAvailableCount() > 0)
                {
                    _isExhausted = false;
                    DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' restaurado para '{name}'.", "green", this);
                    enabled = true;
                }
            }

            if (_trigger == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Trigger é nulo em '{name}'. Desativando SpawnPoint.", this);
                enabled = false;
                return;
            }
            _trigger.CheckTrigger(transform.position, spawnData);
        }

        private bool RegisterPoolIfNeeded()
        {
            if (!poolManager)
            {
                DebugUtility.LogError<SpawnPoint>("PoolManager.Instance é nulo.", this);
                return false;
            }

            if (!poolManager.GetPool(_poolKey))
                poolManager.RegisterPool(spawnData.PoolableData);
            return poolManager.GetPool(_poolKey) != null;
        }

        private void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (evt.Data != spawnData)
                return;

            if (!spawnManager.CanSpawn(this))
            {
                DebugUtility.Log<SpawnPoint>($"Spawn bloqueado para '{name}'.", "yellow", this);
                return;
            }

            var pool = poolManager.GetPool(_poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Pool '{_poolKey}' não encontrado.", this);
                return;
            }

            int spawnCount = Mathf.Min(spawnData.SpawnCount, pool.GetAvailableCount());
            if (spawnCount == 0)
            {
                DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' esgotado para '{name}'.", "yellow", this);
                if (useManagerLocking)
                    _isExhausted = true;
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(_poolKey));
                return;
            }

            var objects = new IPoolable[spawnCount];
            for (int i = 0; i < spawnCount; i++)
            {
                objects[i] = poolManager.GetObject(_poolKey, evt.Origin);
                if (objects[i] == null)
                {
                    DebugUtility.LogError<SpawnPoint>($"Falha ao obter objeto do pool '{_poolKey}'.", this);
                    if (useManagerLocking)
                        _isExhausted = true;
                    EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(_poolKey));
                    return;
                }
            }

            _strategy.Spawn(objects, spawnData, evt.Origin, transform.forward);
            spawnManager.RegisterSpawn(this);
            EventBus<SpawnTriggeredEvent>.Raise(new SpawnTriggeredEvent(_poolKey, evt.Origin, spawnData));
            DebugUtility.Log<SpawnPoint>($"Spawn de {spawnCount} objetos em '{name}' na posição {evt.Origin}.", "green", this);
        }

        private void HandlePoolExhausted(PoolExhaustedEvent evt)
        {
            if (evt.PoolKey != _poolKey)
                return;
            if (useManagerLocking)
            {
                _isExhausted = true;
                spawnManager.LockSpawns(this);
                enabled = false;
                DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' esgotado para '{name}' (gerenciado).", "yellow", this);
            }
            else
            {
                DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' esgotado para '{name}' (independente).", "yellow", this);
            }
        }

        public void SetStrategyData(StrategyData newData)
        {
            if (newData == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Novo StrategyData é nulo para '{name}'.", this);
                return;
            }
            strategyData = newData;
            InitializeStrategy();
            DebugUtility.Log<SpawnPoint>($"Estratégia de '{name}' atualizada para '{strategyData.strategyType}'.", "green", this);
        }

        public void TriggerReset()
        {
            _isExhausted = false;
            _trigger?.SetActive(true);
            _trigger?.Reset();
            enabled = spawnManager.CanSpawn(this);
            DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' resetado.", "green", this);
        }

        public void SetTriggerActive(bool active)
        {
            _trigger?.SetActive(active);
            DebugUtility.Log<SpawnPoint>($"Trigger de '{name}' {(active ? "ativado" : "desativado")}.", "yellow", this);
        }

        public string GetPoolKey() => _poolKey;
        public SpawnData GetSpawnData() => spawnData;
    }
}