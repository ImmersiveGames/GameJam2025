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
        [SerializeField] private bool startImmediately = true; // Novo
        [SerializeField] private float cooldownAfterExhausted = 5f; // Novo

        private ISpawnStrategy _strategy;
        private ISpawnTrigger _trigger;
        private EventBinding<SpawnRequestEvent> _spawnBinding;
        private EventBinding<PoolExhaustedEvent> _exhaustedBinding;
        private string _poolKey;
        private float _cooldownEndTime;

        private void Awake()
        {
            if (spawnData == null)
            {
                DebugUtility.LogError<SpawnPoint>("SpawnData não configurado.", this);
                enabled = false;
                return;
            }

            if (spawnData.PoolableData == null)
            {
                DebugUtility.LogError<SpawnPoint>("PoolableObjectData não configurado no SpawnData.", this);
                enabled = false;
                return;
            }

            _poolKey = spawnData.PoolableData.ObjectName;
            if (string.IsNullOrEmpty(_poolKey))
            {
                DebugUtility.LogError<SpawnPoint>("PoolableObjectData.ObjectName está vazio.", this);
                enabled = false;
                return;
            }

            _strategy = new MockSpawnStrategy();
            _trigger = useInputTrigger ? new InputTrigger(inputKey) :
                       useIntervalTrigger ? new IntervalTrigger(interval, startImmediately) :
                       new InitializationTrigger();
            _spawnBinding = new EventBinding<SpawnRequestEvent>(HandleSpawnRequest);
            _exhaustedBinding = new EventBinding<PoolExhaustedEvent>(HandlePoolExhausted);

            if (!RegisterPoolIfNeeded())
            {
                enabled = false;
                return;
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
            if (Time.time < _cooldownEndTime)
                return; // Em cooldown

            _trigger.CheckTrigger(transform.position, spawnData);
        }

        private bool RegisterPoolIfNeeded()
        {
            if (PoolManager.Instance.GetPool(_poolKey) != null)
            {
                return true;
            }

            PoolManager.Instance.RegisterPool(spawnData.PoolableData);
            if (PoolManager.Instance.GetPool(_poolKey) == null)
            {
                DebugUtility.LogError<SpawnPoint>($"Falha ao registrar pool para '{_poolKey}'.", this);
                return false;
            }

            DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' registrado automaticamente.", "green", this);
            return true;
        }

        private void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (evt.Data != spawnData) return;

            var objects = new IPoolable[spawnData.SpawnCount];
            for (int i = 0; i < spawnData.SpawnCount; i++)
            {
                objects[i] = PoolManager.Instance.GetObject(_poolKey, evt.Origin);
                if (objects[i] == null)
                {
                    DebugUtility.LogVerbose<SpawnPoint>($"Nenhum objeto disponível no pool '{_poolKey}'.", "yellow", this);
                    continue;
                }
            }

            _strategy.Spawn(objects, evt.Origin, spawnData);

            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    EventBus<ObjectSpawnedEvent>.Raise(new ObjectSpawnedEvent(obj, obj.GetGameObject().transform.position));
                }
            }
        }

        private void HandlePoolExhausted(PoolExhaustedEvent evt)
        {
            if (evt.PoolKey != _poolKey) return;

            DebugUtility.Log<SpawnPoint>($"Pool '{_poolKey}' esgotado. Iniciando cooldown de {cooldownAfterExhausted}s.", "yellow", this);
            _cooldownEndTime = Time.time + cooldownAfterExhausted;
        }
    }
}