using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.SpawnSystems.Strategies;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class ShootingSpawnPoint : MonoBehaviour
    {
        [SerializeField] private ShootingSpawnData initialSpawnData;
        private ShootingSpawnData _currentSpawnData;
        private ISpawnTrigger _trigger;
        private ISpawnStrategy _strategy;
        private string _poolKey;
        private EventBinding<SpawnRequestEvent> _spawnBinding;

        public void Initialize(PlayerInputActions inputActions, ShootingSpawnData initialData)
        {
            if (initialData == null || initialData.PoolableData == null || string.IsNullOrEmpty(initialData.PoolableData.ObjectName))
            {
                DebugUtility.LogError<ShootingSpawnPoint>("Configuração inválida no initialSpawnData.", this);
                enabled = false;
                return;
            }

            _currentSpawnData = initialData;
            _poolKey = _currentSpawnData.PoolableData.ObjectName;
            _strategy = _currentSpawnData.Pattern switch
            {
                SpawnPattern.Single => new SingleSpawnStrategy(),
                _ => new SingleSpawnStrategy()
            };
            _trigger = new InputShotTrigger(inputActions);
            if (_trigger is InputShotTrigger shotTrigger)
            {
                shotTrigger.SetFireRate(_currentSpawnData.FireRate);
            }
            _spawnBinding = new EventBinding<SpawnRequestEvent>(HandleSpawnRequest);

            if (!RegisterPoolIfNeeded())
            {
                enabled = false;
            }
        }

        private void Awake()
        {
            if (_trigger == null)
            {
                Initialize(new PlayerInputActions(), initialSpawnData);
            }
        }

        private void OnEnable()
        {
            EventBus<SpawnRequestEvent>.Register(_spawnBinding);
        }

        private void OnDisable()
        {
            EventBus<SpawnRequestEvent>.Unregister(_spawnBinding);
        }

        private void Update()
        {
            _trigger.CheckTrigger(transform.position, _currentSpawnData);
        }

        private bool RegisterPoolIfNeeded()
        {
            if (PoolManager.Instance == null)
            {
                DebugUtility.LogError<ShootingSpawnPoint>("PoolManager.Instance é nulo.", this);
                return false;
            }
            if (PoolManager.Instance.GetPool(_poolKey) == null)
            {
                PoolManager.Instance.RegisterPool(_currentSpawnData.PoolableData);
            }
            return PoolManager.Instance.GetPool(_poolKey) != null;
        }

        private void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (evt.Data != _currentSpawnData)
                return;

            var pool = PoolManager.Instance.GetPool(_poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<ShootingSpawnPoint>($"Pool '{_poolKey}' não encontrado.", this);
                return;
            }

            int spawnCount = Mathf.Min(_currentSpawnData.SpawnCount, pool.GetAvailableCount());
            if (spawnCount == 0)
            {
                DebugUtility.LogError<ShootingSpawnPoint>($"Spawn falhou para '{name}': Pool esgotado.", this);
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, evt.Origin, _currentSpawnData));
                return;
            }

            var objects = new IPoolable[spawnCount];
            for (int i = 0; i < spawnCount; i++)
            {
                objects[i] = PoolManager.Instance.GetObject(_poolKey, evt.Origin);
                if (objects[i] == null)
                {
                    DebugUtility.LogError<ShootingSpawnPoint>($"Spawn falhou para '{name}': Objeto nulo.", this);
                    EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, evt.Origin, _currentSpawnData));
                    return;
                }
            }

            _strategy.Spawn(objects, evt.Origin, _currentSpawnData, transform.forward);
            EventBus<SpawnTriggeredEvent>.Raise(new SpawnTriggeredEvent(_poolKey, evt.Origin, _currentSpawnData));
        }

        public void SetSpawnData(ShootingSpawnData newData)
        {
            if (newData == null || newData.PoolableData == null || string.IsNullOrEmpty(newData.PoolableData.ObjectName))
            {
                DebugUtility.LogError<ShootingSpawnPoint>($"Novo SpawnData inválido para '{name}'.", this);
                return;
            }

            _currentSpawnData = newData;
            _poolKey = _currentSpawnData.PoolableData.ObjectName;
            _strategy = _currentSpawnData.Pattern switch
            {
                SpawnPattern.Single => new SingleSpawnStrategy(),
                _ => new SingleSpawnStrategy()
            };
            if (_trigger is InputShotTrigger shotTrigger)
            {
                shotTrigger.SetFireRate(_currentSpawnData.FireRate);
            }
            RegisterPoolIfNeeded();
        }
    }
}