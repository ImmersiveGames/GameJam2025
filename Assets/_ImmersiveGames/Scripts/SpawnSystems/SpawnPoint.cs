using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnData spawnData;
        [SerializeField] private bool useInputTrigger;
        [SerializeField] private KeyCode inputKey = KeyCode.S;
        [SerializeField] private bool useIntervalTrigger;
        [SerializeField] private float interval = 3f;
        [SerializeField] private bool startImmediately = true;
        [SerializeField] private float cooldownAfterExhausted = 5f;

        private ISpawnTrigger _trigger;
        private ISpawnStrategy _strategy;
        private string _poolKey;
        private bool _isExhausted;
        private float _cooldownEndTime;
        private EventBinding<SpawnRequestEvent> _spawnBinding;
        private EventBinding<PoolExhaustedEvent> _exhaustedBinding;

        private void Awake()
        {
            if (spawnData == null || spawnData.PoolableData == null || string.IsNullOrEmpty(spawnData.PoolableData.ObjectName))
            {
                DebugUtility.LogError<SpawnPoint>("Configuração inválida no SpawnData.", this);
                enabled = false;
                return;
            }

            _poolKey = spawnData.PoolableData.ObjectName;
            _strategy = new SingleSpawnStrategy();
            _trigger = useInputTrigger ? new InputTrigger(inputKey) :
                       useIntervalTrigger ? new IntervalTrigger(interval, startImmediately) :
                       new InitializationTrigger();
            _spawnBinding = new EventBinding<SpawnRequestEvent>(HandleSpawnRequest);
            _exhaustedBinding = new EventBinding<PoolExhaustedEvent>(HandlePoolExhausted);
        }

        private void Start()
        {
            if (!RegisterPoolIfNeeded())
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EventBus<SpawnRequestEvent>.Register(_spawnBinding);
            EventBus<PoolExhaustedEvent>.Register(_exhaustedBinding);
        }

        private void OnDisable()
        {
            EventBus<SpawnRequestEvent>.Unregister(_spawnBinding);
            EventBus<PoolExhaustedEvent>.Unregister(_exhaustedBinding);
        }

        private void Update()
        {
            if (_isExhausted || Time.time < _cooldownEndTime)
            {
                var pool = PoolManager.Instance.GetPool(_poolKey);
                if (pool != null && pool.GetAvailableCount() > 0)
                {
                    _isExhausted = false;
                    _cooldownEndTime = 0f;
                }
                else
                {
                    return;
                }
            }
            _trigger.CheckTrigger(transform.position, spawnData);
        }

        private bool RegisterPoolIfNeeded()
        {
            if (PoolManager.Instance == null)
            {
                DebugUtility.LogError<SpawnPoint>("PoolManager.Instance é nulo.", this);
                return false;
            }

            if (PoolManager.Instance.GetPool(_poolKey) == null)
            {
                PoolManager.Instance.RegisterPool(spawnData.PoolableData);
            }
            return PoolManager.Instance.GetPool(_poolKey) != null;
        }

        private void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (evt.Data != spawnData) return;

            var pool = PoolManager.Instance.GetPool(_poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Pool '{_poolKey}' não encontrado.", this);
                return;
            }

            int spawnCount = Mathf.Min(spawnData.SpawnCount, pool.GetAvailableCount());
            if (spawnCount == 0)
            {
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, evt.Origin, spawnData));
                return;
            }

            var objects = new IPoolable[spawnCount];
            for (int i = 0; i < spawnCount; i++)
            {
                objects[i] = PoolManager.Instance.GetObject(_poolKey, evt.Origin);
                if (objects[i] == null)
                {
                    EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, evt.Origin, spawnData));
                    return;
                }
            }

            _strategy.Spawn(objects, evt.Origin, spawnData);
            EventBus<SpawnTriggeredEvent>.Raise(new SpawnTriggeredEvent(_poolKey, evt.Origin, spawnData));
        }

        private void HandlePoolExhausted(PoolExhaustedEvent evt)
        {
            if (evt.PoolKey != _poolKey) return;
            _isExhausted = true;
            _cooldownEndTime = Time.time + cooldownAfterExhausted;
            DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' esgotado. Cooldown: {cooldownAfterExhausted}s.", "yellow", this);
        }

        public void ResetSpawnPoint()
        {
            _isExhausted = false;
            _cooldownEndTime = 0f;
            if (_trigger is IntervalTrigger intervalTrigger)
            {
                intervalTrigger.Reset();
            }
            DebugUtility.Log<SpawnPoint>($"SpawnPoint '{_poolKey}' resetado.", "green", this);
        }
    }
}