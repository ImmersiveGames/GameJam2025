using _ImmersiveGames.Scripts.Predicates;
using _ImmersiveGames.Scripts.SpawnSystems.Strategies;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField]
        protected SpawnData spawnData;
        [SerializeField]
        public bool useManagerLocking = true;

        protected SpawnTriggerSo _trigger;
        private SpawnStrategySo _strategy;
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
            _strategy = spawnData.Pattern;
            _trigger = spawnData.TriggerStrategy;
            _trigger.Initialize(this);
            _spawnBinding = new EventBinding<SpawnRequestEvent>(HandleSpawnRequest);
            _exhaustedBinding = new EventBinding<PoolExhaustedEvent>(HandlePoolExhausted);

            enabled = spawnManager.CanSpawn(this) && !_isExhausted;
        }

        private void Start()
        {
            if (!RegisterPoolIfNeeded())
                enabled = false;
        }

        protected virtual void OnEnable()
        {
            EventBus<SpawnRequestEvent>.Register(_spawnBinding);
            EventBus<PoolExhaustedEvent>.Register(_exhaustedBinding);
        }

        protected virtual void OnDisable()
        {
            EventBus<SpawnRequestEvent>.Unregister(_spawnBinding);
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
            return poolManager.GetPool(_poolKey);
        }

        private void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (evt.Data != spawnData)
                return;

            // Verificação essencial: garantir que o spawn é permitido
            if (!spawnManager.CanSpawn(this))
            {
                DebugUtility.Log<SpawnPoint>($"Spawn bloqueado para '{name}'.", "yellow", this);
                return;
            }

            // Obter o pool
            var pool = poolManager.GetPool(_poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Pool '{_poolKey}' não encontrado.", this);
                return;
            }

            // Calcular quantidade de objetos a spawnar
            int spawnCount = Mathf.Min(spawnData.SpawnCount, pool.GetAvailableCount());
            if (spawnCount == 0)
            {
                DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' esgotado para '{name}'.", "yellow", this);
                if (useManagerLocking)
                    _isExhausted = true;
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(_poolKey));
                return;
            }

            // Obter objetos do pool
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

            // Executar o spawn
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

        public void TriggerReset()
        {
            _isExhausted = false;
            _trigger.SetActive(true);
            _trigger.Reset();
            enabled = spawnManager.CanSpawn(this);
            DebugUtility.Log<SpawnPoint>($"SpawnPoint '{name}' resetado.", "green", this);
        }

        public void SetTriggerActive(bool active)
        {
            _trigger.SetActive(active);
            if (this is PlayerInputSpawnPoint inputSpawnPoint && _trigger is PredicateTriggerSo predicateTrigger)
            {
                inputSpawnPoint.BindAllPredicates(predicateTrigger.predicate, inputSpawnPoint.PlayerInput.actions);
            }
            DebugUtility.Log<SpawnPoint>($"Trigger de '{name}' {(active ? "ativado" : "desativado")}.", "yellow", this);
        }

        public string GetPoolKey() => _poolKey;
        public SpawnData GetSpawnData() => spawnData;
    }
}