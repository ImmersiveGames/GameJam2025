using _ImmersiveGames.Scripts.Predicates;
using _ImmersiveGames.Scripts.SpawnSystems.Strategies;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField]
        protected SpawnData spawnData;
        [SerializeField]
        public bool useManagerLocking = true;

        private SpawnTriggerSo _trigger;
        private SpawnStrategySo _strategy;
        private string _poolKey;
        private bool _isExhausted;
        private EventBinding<SpawnRequestEvent> _spawnBinding;
        private EventBinding<PoolExhaustedEvent> _exhaustedBinding;
        private SpawnManager _spawnManager;
        private PoolManager _poolManager;

        protected virtual void Awake()
        {
            if (spawnData == null || spawnData.PoolableData == null || string.IsNullOrEmpty(spawnData.PoolableData.ObjectName))
            {
                DebugUtility.LogError<SpawnPoint>("Configuração inválida no SpawnData.", this);
                enabled = false;
                return;
            }

            _spawnManager = SpawnManager.Instance;
            _poolManager = PoolManager.Instance;
            if (_spawnManager == null)
            {
                DebugUtility.LogError<SpawnPoint>("SpawnManager.Instance não encontrado.", this);
                enabled = false;
                return;
            }
            _spawnManager.RegisterSpawnPoint(this, useManagerLocking);

            _poolKey = spawnData.PoolableData.ObjectName;
            _strategy = spawnData.Pattern;
            _trigger = spawnData.TriggerStrategy;
            _trigger.Initialize(this);
            _spawnBinding = new EventBinding<SpawnRequestEvent>(HandleSpawnRequest);
            _exhaustedBinding = new EventBinding<PoolExhaustedEvent>(HandlePoolExhausted);

            enabled = _spawnManager.CanSpawn(this) && !_isExhausted;
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
            if (!_spawnManager.CanSpawn(this) || _isExhausted)
            {
                enabled = false;
                return;
            }

            if (!useManagerLocking && _isExhausted)
            {
                var pool = _poolManager.GetPool(_poolKey);
                if (pool != null && pool.GetAvailableCount() > 0)
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
            if (_poolManager == null)
            {
                DebugUtility.LogError<SpawnPoint>("PoolManager.Instance é nulo.", this);
                return false;
            }

            if (_poolManager.GetPool(_poolKey) == null)
                _poolManager.RegisterPool(spawnData.PoolableData);
            return _poolManager.GetPool(_poolKey) != null;
        }

        private void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (evt.Data != spawnData)
                return;

            if (!_spawnManager.CanSpawn(this))
            {
                DebugUtility.Log<SpawnPoint>($"Spawn falhou para '{name}': Bloqueado.", "yellow", this);
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, evt.Origin, spawnData));
                return;
            }

            if (useManagerLocking && _isExhausted)
            {
                DebugUtility.Log<SpawnPoint>($"Spawn falhou para '{name}': Exausto.", "yellow", this);
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, evt.Origin, spawnData));
                return;
            }

            var pool = _poolManager.GetPool(_poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Pool '{_poolKey}' não encontrado.", this);
                return;
            }

            int spawnCount = Mathf.Min(spawnData.SpawnCount, pool.GetAvailableCount());
            if (spawnCount == 0)
            {
                DebugUtility.Log<SpawnPoint>($"Spawn falhou para '{name}': Pool esgotado.", "yellow", this);
                if (useManagerLocking)
                    _isExhausted = true;
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, evt.Origin, spawnData));
                return;
            }

            Vector3 spawnPosition = evt.Origin;
            DebugUtility.Log<SpawnPoint>($"Usando posição do SpawnRequestEvent: {spawnPosition}", "green", this);

            var objects = new IPoolable[spawnCount];
            for (int i = 0; i < spawnCount; i++)
            {
                objects[i] = _poolManager.GetObject(_poolKey, spawnPosition);
                if (objects[i] == null)
                {
                    DebugUtility.Log<SpawnPoint>($"Spawn falhou para '{name}': Objeto nulo.", "yellow", this);
                    if (useManagerLocking)
                        _isExhausted = true;
                    EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, spawnPosition, spawnData));
                    return;
                }
            }

            _strategy.Spawn(objects, spawnData, spawnPosition, transform.forward);
            _spawnManager.RegisterSpawn(this);
            EventBus<SpawnTriggeredEvent>.Raise(new SpawnTriggeredEvent(_poolKey, spawnPosition, spawnData));
        }

        private void HandlePoolExhausted(PoolExhaustedEvent evt)
        {
            if (evt.PoolKey != _poolKey)
                return;
            if (useManagerLocking)
            {
                _isExhausted = true;
                _spawnManager.LockSpawns(this);
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
            enabled = _spawnManager.CanSpawn(this);
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