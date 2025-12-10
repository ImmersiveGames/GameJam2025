// Path: _ImmersiveGames/Scripts/PlayerControllerSystem/Shooting/PlayerShootController.cs

using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Shooting
{
    /// <summary>
    /// Controlador responsável pelo disparo do jogador.
    /// Utiliza estratégias de spawn para instanciar projéteis via Pool.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerShootController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Pool Config")]
        [SerializeField] private PoolData poolData;

        [Header("Input Config")]
        [SerializeField] private string actionName = "Spawn";

        [Header("Cooldown Config")]
        [SerializeField, Min(0f)] private float cooldown = 0.5f;

        [Header("Spawn Strategy Config")]
        [SerializeField] private SpawnStrategyType strategyType = SpawnStrategyType.Single;
        [SerializeField] private SingleSpawnStrategy singleStrategy = new();
        [SerializeField] private MultipleLinearSpawnStrategy multipleLinearStrategy = new();
        [SerializeField] private CircularSpawnStrategy circularStrategy = new();

        [Header("Audio Config")]
        [SerializeField] private SoundData defaultShootSound;

        #endregion

        #region Private Fields

        private ISpawnStrategy _activeStrategy;
        private ObjectPool _pool;
        private PlayerInput _playerInput;
        private InputAction _spawnAction;
        private float _lastShotTime = -Mathf.Infinity;

        private IActor _actor;
        private EntityAudioEmitter _audioEmitter;
        private bool _isInitialized;

        [Inject] private IStateDependentService _stateService;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _actor = GetComponent<IActor>();
            _audioEmitter = GetComponent<EntityAudioEmitter>();

            DependencyManager.Provider.InjectDependencies(this);
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                SubscribeToSpawnAction();
            }
        }

        private void OnDisable()
        {
            if (_spawnAction != null)
            {
                _spawnAction.performed -= OnSpawnPerformed;
            }
        }

        private void Start()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            InitializeStrategies();

            if (!TryInitializePool())
            {
                enabled = false;
                return;
            }

            if (!TryBindInputAction())
            {
                enabled = false;
                return;
            }

            _isInitialized = true;
            SubscribeToSpawnAction();

            DebugUtility.Log<PlayerShootController>("PlayerShootController inicializado", "blue", this);
        }

        private void OnDestroy()
        {
            if (_spawnAction != null)
                _spawnAction.performed -= OnSpawnPerformed;
        }

        #endregion

        #region Initialization Helpers

        private bool ValidateConfiguration()
        {
            if (_playerInput == null)
            {
                DebugUtility.LogError<PlayerShootController>($"PlayerInput não encontrado em '{name}'.", this);
                return false;
            }

            if (_actor == null)
            {
                DebugUtility.LogError<PlayerShootController>($"IActor não encontrado em '{name}'.", this);
                return false;
            }

            if (poolData == null)
            {
                DebugUtility.LogError<PlayerShootController>($"PoolData não configurado em '{name}'.", this);
                return false;
            }

            return true;
        }

        private void InitializeStrategies()
        {
            singleStrategy ??= new SingleSpawnStrategy();
            multipleLinearStrategy ??= new MultipleLinearSpawnStrategy();
            circularStrategy ??= new CircularSpawnStrategy();

            SetStrategy(strategyType);
        }

        private bool TryInitializePool()
        {
            var manager = PoolManager.Instance;
            if (manager == null)
            {
                DebugUtility.LogError<PlayerShootController>("PoolManager não encontrado na cena.", this);
                return false;
            }

            _pool = manager.RegisterPool(poolData);
            if (_pool == null)
            {
                DebugUtility.LogError<PlayerShootController>($"Pool não encontrado para '{poolData.ObjectName}' em '{name}'.", this);
                return false;
            }

            return true;
        }

        private bool TryBindInputAction()
        {
            if (_playerInput.actions == null)
            {
                DebugUtility.LogError<PlayerShootController>($"Mapa de ações não encontrado em '{name}'.", this);
                return false;
            }

            _spawnAction = _playerInput.actions.FindAction(actionName);
            if (_spawnAction == null)
            {
                DebugUtility.LogError<PlayerShootController>($"Ação '{actionName}' não encontrada em '{name}'.", this);
                return false;
            }

            _spawnAction.performed += OnSpawnPerformed;

            DebugUtility.Log<PlayerShootController>(
                "PlayerShootController vinculado à ação de tiro.",
                DebugUtility.Colors.CrucialInfo);

            return true;
        }

        private void SubscribeToSpawnAction()
        {
            if (_spawnAction == null)
                return;

            _spawnAction.performed -= OnSpawnPerformed;
            _spawnAction.performed += OnSpawnPerformed;
        }

        #endregion

        #region Shooting Logic

        private void OnSpawnPerformed(InputAction.CallbackContext context)
        {
            if (!_actor.IsActive || !_stateService.CanExecuteAction(ActionType.Shoot))
                return;

            if (_pool == null)
                return;

            if (Time.time < _lastShotTime + cooldown)
                return;

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
            if (_audioEmitter == null)
            {
                DebugUtility.LogWarning<PlayerShootController>("EntityAudioEmitter não encontrado — som de tiro não tocado.");
                return;
            }

            var strategySound = _activeStrategy.GetShootSound();

            if (strategySound == null || strategySound.clip == null)
            {
                strategySound = defaultShootSound;

                if (strategySound == null || strategySound.clip == null)
                {
                    DebugUtility.LogWarning<PlayerShootController>("Nenhum som de tiro configurado.");
                    return;
                }

                DebugUtility.LogVerbose<PlayerShootController>("Usando som de tiro default.");
            }

            var context = AudioContext.Default(transform.position, _audioEmitter.UsesSpatialBlend);
            _audioEmitter.Play(strategySound, context);
        }

        #endregion
    }
}
