// Path: _ImmersiveGames/Scripts/SpawnSystems/InputSpawnerComponent.cs
using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [RequireComponent(typeof(PlayerInput))]
    [DebugLevel(DebugLevel.Error)]
    public class InputSpawnerComponent : MonoBehaviour
    {
        [Header("Pool Config")] [SerializeField] private PoolData poolData;

        [Header("Input Config")] [SerializeField] private string actionName = "Spawn";

        [Header("Cooldown Config")] [SerializeField, Min(0f)] private float cooldown = 0.5f;

        [Header("Spawn Strategy Config")]
        [SerializeField] private SpawnStrategyType strategyType = SpawnStrategyType.Single;
        [SerializeField] private SingleSpawnStrategy singleStrategy = new();
        [SerializeField] private MultipleLinearSpawnStrategy multipleLinearStrategy = new();
        [SerializeField] private CircularSpawnStrategy circularStrategy = new();

        private ISpawnStrategy _activeStrategy;
        private ObjectPool _pool;
        private PlayerInput _playerInput;
        private InputAction _spawnAction;
        private float _lastShotTime = -Mathf.Infinity;
        private IActor _actor;

        [Inject] private IStateDependentService _stateService;

        private PlayerAudioController _playerAudio;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _actor = GetComponent<IActor>();
            _playerAudio = GetComponent<PlayerAudioController>();

            DependencyManager.Instance.InjectDependencies(this);

            if (_playerInput == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"PlayerInput não encontrado em '{name}'.", this);
                enabled = false;
                return;
            }

            if (poolData == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"PoolData não configurado em '{name}'.", this);
                enabled = false;
                return;
            }

            PoolManager.Instance.RegisterPool(poolData);
            _pool = PoolManager.Instance.GetPool(poolData.ObjectName);

            if (_pool == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"Pool não encontrado para '{poolData.ObjectName}' em '{name}'.", this);
                enabled = false;
                return;
            }

            singleStrategy ??= new SingleSpawnStrategy();
            multipleLinearStrategy ??= new MultipleLinearSpawnStrategy();
            circularStrategy ??= new CircularSpawnStrategy();

            SetStrategy(strategyType);

            _spawnAction = _playerInput.actions.FindAction(actionName);
            if (_spawnAction == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"Ação '{actionName}' não encontrada em '{name}'.", this);
                enabled = false;
                return;
            }

            _spawnAction.performed += OnSpawnPerformed;
            DebugUtility.Log<InputSpawnerComponent>($"InputSpawnerComponent inicializado", "blue");
        }

        private void OnDestroy()
        {
            if (_spawnAction != null) _spawnAction.performed -= OnSpawnPerformed;
        }

        private void OnSpawnPerformed(InputAction.CallbackContext context)
        {
            if (!_actor.IsActive || !_stateService.CanExecuteAction(ActionType.Shoot)) return;
            if (_pool == null) return;
            if (Time.time < _lastShotTime + cooldown) return;

            var basePosition = transform.position;
            var baseDirection = transform.forward;
            var spawner = _actor;

            var spawnDataList = _activeStrategy.GetSpawnData(basePosition, baseDirection);

            bool success = false;
            int spawnedCount = 0;
            foreach (var spawnData in spawnDataList)
            {
                var poolable = _pool.GetObject(spawnData.Position, spawner, spawnData.Direction);
                if (poolable != null)
                {
                    success = true;
                    spawnedCount++;
                }
            }

            if (success)
            {
                _lastShotTime = Time.time;

                // Decide qual SoundData usar (estratégia > player config)
                SoundData sound = null;
                if (_activeStrategy is IProvidesShootSound provider)
                    sound = provider.GetShootSound();
                else if (_playerAudio != null)
                    sound = _playerAudio.AudioConfig?.shootSound;

                _playerAudio?.PlayShootSound(sound, 1f); // volume multiplier can be adjusted per spawnedCount if needed
            }
        }

        public void SetStrategy(SpawnStrategyType type)
        {
            strategyType = type;
            _activeStrategy = type switch
            {
                SpawnStrategyType.Single => singleStrategy,
                SpawnStrategyType.MultipleLinear => multipleLinearStrategy,
                SpawnStrategyType.Circular => circularStrategy,
                _ => singleStrategy
            };
        }
    }

    // Simple interface to mark strategies that provide a SoundData
    public interface IProvidesShootSound
    {
        SoundData GetShootSound();
    }
}
