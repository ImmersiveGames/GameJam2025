// Path: _ImmersiveGames/Scripts/SpawnSystems/InputSpawnerComponent.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem;
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
        private PlayerAudioController _audioController;

        [Inject] private IStateDependentService _stateService;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _actor = GetComponent<IActor>();
            _audioController = GetComponent<PlayerAudioController>(); // Adicionado: Obtém controller local
            
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

            List<SpawnData> spawnDataList = _activeStrategy.GetSpawnData(basePosition, baseDirection);

            bool success = false;
            foreach (var spawnData in spawnDataList)
            {
                var poolable = _pool.GetObject(spawnData.position, spawner, spawnData.direction);
                if (poolable != null)
                {
                    success = true;
                }
            }

            if (success)
            {
                _lastShotTime = Time.time;
                PlayShootAudio();
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
        private void PlayShootAudio()
        {
            if (_audioController == null) 
            {
                DebugUtility.LogWarning<InputSpawnerComponent>("PlayerAudioController não encontrado — som de tiro não tocado.");
                return;
            }

            var strategySound = _activeStrategy.GetShootSound();
            if (strategySound == null || strategySound.clip == null) // Adicionado: Checa se estratégia tem áudio válido
            {
                strategySound = _audioController.GetAudioConfig()?.shootSound; // Fallback para default do controller
                if (strategySound == null || strategySound.clip == null)
                {
                    DebugUtility.LogWarning<InputSpawnerComponent>("Nenhum som de tiro default configurado no controller.");
                    return;
                }
                DebugUtility.LogVerbose<InputSpawnerComponent>("Usando som de tiro default do controller (estratégia sem áudio).", "cyan");
            }

            _audioController.PlayShootSound(strategySound); // Toca com som resolvido
        }
    }
}
