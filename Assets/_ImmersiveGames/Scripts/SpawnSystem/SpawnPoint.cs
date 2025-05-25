using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnData spawnData;
        [SerializeField] private bool useInputTrigger = true;
        [SerializeField] private KeyCode inputKey = KeyCode.S;
        [SerializeField] private bool useIntervalTrigger = false;
        [SerializeField] private float interval = 3f;
        [SerializeField] private bool startImmediately = true;

        private ISpawnStrategy _strategy;
        private ISpawnTrigger _trigger;
        private EventBinding<SpawnRequestEvent> _spawnBinding;
        private EventBinding<Utils.PoolSystems.PoolEvents> _exhaustedBinding;
        private string _poolKey;
        private bool _isExhausted;
        private bool _justReset;

        private void Awake()
        {
            if (spawnData == null || spawnData.PoolableData == null || string.IsNullOrEmpty(spawnData.PoolableData.ObjectName))
            {
                DebugUtility.LogError<SpawnPoint>("Configuração inválida no SpawnData.", this);
                enabled = false;
                return;
            }

            _poolKey = spawnData.PoolableData.ObjectName;
            _strategy = new MockSpawnStrategy();
            _trigger = useInputTrigger ? new InputTrigger(inputKey) :
                       useIntervalTrigger ? new IntervalTrigger(interval, startImmediately) :
                       new InitializationTrigger();
            _spawnBinding = new EventBinding<SpawnRequestEvent>(HandleSpawnRequest);
            _exhaustedBinding = new EventBinding<Utils.PoolSystems.PoolEvents>(HandlePoolExhausted);
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
            EventBus<Utils.PoolSystems.PoolEvents>.Register(_exhaustedBinding);
        }

        private void OnDisable()
        {
            EventBus<SpawnRequestEvent>.Unregister(_spawnBinding);
            EventBus<Utils.PoolSystems.PoolEvents>.Unregister(_exhaustedBinding);
        }

        private void Update()
        {
            if (_isExhausted)
            {
                var pool = PoolManager.Instance.GetPool(_poolKey);
                if (pool != null && pool.GetAvailableCount() > 0)
                {
                    _isExhausted = false;
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

            if (PoolManager.Instance.GetPool(_poolKey) != null)
            {
                return true;
            }

            PoolManager.Instance.RegisterPool(spawnData.PoolableData);
            return PoolManager.Instance.GetPool(_poolKey) != null;
        }

        private void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (evt.Data != spawnData) return;

            var pool = PoolManager.Instance.GetPool(_poolKey);
            if (pool == null) return;

            int availableCount = pool.GetAvailableCount();
            int spawnCount = Mathf.Min(spawnData.SpawnCount, availableCount);

            if (spawnCount == 0)
            {
                if (!_justReset)
                {
                    HandlePoolExhausted(new Utils.PoolSystems.PoolEvents(_poolKey));
                }
                return;
            }

            var objects = new IPoolable[spawnCount];
            for (int i = 0; i < spawnCount; i++)
            {
                objects[i] = PoolManager.Instance.GetObject(_poolKey, evt.Origin);
                if (objects[i] == null)
                {
                    if (!_justReset)
                    {
                        HandlePoolExhausted(new Utils.PoolSystems.PoolEvents(_poolKey));
                    }
                    return;
                }
            }

            _justReset = false;
            _strategy.Spawn(objects, evt.Origin, spawnData);

            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    EventBus<ObjectSpawnedEvent>.Raise(new ObjectSpawnedEvent(obj, obj.GetGameObject().transform.position));
                }
            }

            if (pool.GetAvailableCount() == 0)
            {
                HandlePoolExhausted(new Utils.PoolSystems.PoolEvents(_poolKey));
            }
        }

        private void HandlePoolExhausted(Utils.PoolSystems.PoolEvents evt)
        {
            if (evt.PoolKey != _poolKey) return;
            _isExhausted = true;
            _justReset = false;
            DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' esgotado.", "yellow", this);
        }

        public void ResetSpawnPoint()
        {
            _isExhausted = false;
            _justReset = true;
            if (useIntervalTrigger)
            {
                if (_trigger is IntervalTrigger intervalTrigger)
                {
                    intervalTrigger.Reset();
                }
                HandleSpawnRequest(new SpawnRequestEvent(_poolKey, transform.position, spawnData));
            }
            DebugUtility.Log<SpawnPoint>($"SpawnPoint '{_poolKey}' resetado.", "green", this);
        }
    }
}