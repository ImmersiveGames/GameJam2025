using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.Predicates;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.FXSystems
{
    public class FxSpawnPoint : SpawnPoint
    {
        private EventBinding<DeathEvent> _deathEventBinding;
        private SpawnManager _spawnManager;
        private PoolManager _poolManager;

        protected override void Awake()
        {
            base.Awake();
            _spawnManager = SpawnManager.Instance;
            _poolManager = PoolManager.Instance;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeathEvent);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            DebugUtility.Log<FxSpawnPoint>($"FxSpawnPoint {name}: Registrado para DeathEvent.", "green", this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_deathEventBinding != null)
            {
                EventBus<DeathEvent>.Unregister(_deathEventBinding);
                DebugUtility.Log<FxSpawnPoint>($"FxSpawnPoint {name}: Desregistrado de DeathEvent.", "green", this);
            }
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            if (!spawnData || !spawnData.TriggerStrategy ||
                spawnData.TriggerStrategy is not PredicateTriggerSo { predicate: DeathEventPredicateSo deathPredicate })
            {
                DebugUtility.LogWarning<FxSpawnPoint>($"TriggerStrategy ou Predicate não configurado corretamente em {name}.", this);
                return;
            }

            if (!deathPredicate.Evaluate()) return;

            var spawnPosition = deathPredicate.TriggerPosition;
            DebugUtility.Log<FxSpawnPoint>($"Processando DeathEvent para {evt.Source.name} com posição {spawnPosition}.", "green", this);

            if (!_spawnManager.CanSpawn(this))
            {
                DebugUtility.Log<FxSpawnPoint>($"Spawn falhou para '{name}': Bloqueado.", "yellow", this);
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(spawnData.PoolableData.ObjectName, spawnPosition, spawnData));
                return;
            }

            var pool = _poolManager.GetPool(spawnData.PoolableData.ObjectName);
            if (!pool)
            {
                DebugUtility.LogError<FxSpawnPoint>($"Pool '{spawnData.PoolableData.ObjectName}' não encontrado.", this);
                return;
            }

            int spawnCount = Mathf.Min(1, pool.GetAvailableCount()); // Forçar um único objeto
            if (spawnCount == 0)
            {
                DebugUtility.Log<FxSpawnPoint>($"Spawn falhou para '{name}': Pool esgotado.", "yellow", this);
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(spawnData.PoolableData.ObjectName, spawnPosition, spawnData));
                return;
            }

            var objects = new IPoolable[spawnCount];
            objects[0] = _poolManager.GetObject(spawnData.PoolableData.ObjectName, spawnPosition);
            if (objects[0] == null)
            {
                DebugUtility.Log<FxSpawnPoint>($"Spawn falhou para '{name}': Objeto nulo.", "yellow", this);

                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(spawnData.PoolableData.ObjectName, spawnPosition, spawnData));
                return;
            }

            spawnData.Pattern.Spawn(objects, spawnData, spawnPosition, transform.forward);
            _spawnManager.RegisterSpawn(this);
            EventBus<SpawnTriggeredEvent>.Raise(new SpawnTriggeredEvent(spawnData.PoolableData.ObjectName, spawnPosition, spawnData));

            // Resetar o predicate para evitar spawns duplicados
            deathPredicate.Reset();
            DebugUtility.Log<FxSpawnPoint>($"DeathEventPredicateSo resetado após spawn em {name}.", "green", this);
        }
    }
}