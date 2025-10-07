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
        [Header("Pool Config")]
        [SerializeField] private PoolData poolData;

        [Header("Input Config")]
        [SerializeField] private string actionName = "Spawn";

        [Header("Cooldown Config")]
        [SerializeField, Min(0f)] private float cooldown = 0.5f;

        [Header("Audio Config")]
        [SerializeField] private bool enableShootSounds = true;
        [SerializeField] private AudioConfig audioConfig; // Configuração de áudio opcional

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

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _actor = GetComponent<IActor>();
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

            if (!PoolData.Validate(poolData, this))
            {
                DebugUtility.LogError<InputSpawnerComponent>($"PoolData inválido em '{name}'.", this);
                enabled = false;
                return;
            }

            PoolManager.Instance.RegisterPool(poolData);
            _pool = PoolManager.Instance.GetPool(poolData.ObjectName);

            if (_pool == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"Pool não registrado ou encontrado para '{poolData.ObjectName}' em '{name}'.", this);
                enabled = false;
                return;
            }

            singleStrategy ??= new SingleSpawnStrategy();
            multipleLinearStrategy ??= new MultipleLinearSpawnStrategy();
            circularStrategy ??= new CircularSpawnStrategy();

            SetStrategy(strategyType);

            if (_activeStrategy == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"Estratégia de spawn não configurada em '{name}'.", this);
                enabled = false;
                return;
            }

            _spawnAction = _playerInput.actions.FindAction(actionName);
            if (_spawnAction == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"Ação '{actionName}' não encontrada no InputActionMap de '{name}'.", this);
                enabled = false;
                return;
            }

            _spawnAction.performed += OnSpawnPerformed;

            // Garante que o sistema de áudio está inicializado
            if (enableShootSounds)
            {
                AudioSystemInitializer.EnsureAudioSystemInitialized();
            }
            DebugUtility.LogVerbose<InputSpawnerComponent>($"InputSpawnerComponent inicializado em '{name}' com ação '{actionName}', PoolData '{poolData.ObjectName}', cooldown {cooldown}s e estratégia '{strategyType}'.", "cyan", this);
        }

        private void OnDestroy()
        {
            if (_spawnAction != null)
            {
                _spawnAction.performed -= OnSpawnPerformed;
            }
            DebugUtility.LogVerbose<InputSpawnerComponent>($"InputSpawnerComponent destruído em '{name}'.", "blue", this);
        }

        private void OnSpawnPerformed(InputAction.CallbackContext context)
        {
            if (!_actor.IsActive || !_stateService.CanExecuteAction(ActionType.Shoot))
                return;

            if (_pool == null)
            {
                DebugUtility.LogWarning<InputSpawnerComponent>($"Pool nulo em '{name}'.", this);
                return;
            }

            if (Time.time < _lastShotTime + cooldown)
            {
                DebugUtility.LogVerbose<InputSpawnerComponent>($"[{name}] Disparo bloqueado por cooldown. Tempo restante: {(_lastShotTime + cooldown - Time.time):F3}s.", "yellow", this);
                return;
            }

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
                    DebugUtility.LogVerbose<InputSpawnerComponent>($"[{name}] Spawned '{poolable.GetGameObject().name}' em '{spawnData.Position}' na direção '{spawnData.Direction}'.", "green", this);
                }
                else
                {
                    DebugUtility.LogWarning<InputSpawnerComponent>($"Falha ao spawnar objeto em '{name}' na posição '{spawnData.Position}'. Pool esgotado?", this);
                }
            }

            if (success)
            {
                _lastShotTime = Time.time;
                // Tocar som de tiro se configurado
                PlayShootSound(spawnedCount);
            }
        }
        private void PlayShootSound(int spawnedCount)
        {
            if (!enableShootSounds) return;

            SoundData soundToPlay = null;

            if (_activeStrategy.HasShootSound)
            {
                soundToPlay = _activeStrategy.ShootSound;
            }
            else if (audioConfig?.shootSound != null)
            {
                soundToPlay = audioConfig.shootSound;
            }

            if (soundToPlay != null)
            {
                float volumeMultiplier = 1f;
                if (spawnedCount > 1 && strategyType != SpawnStrategyType.Single)
                {
                    volumeMultiplier = Mathf.Clamp(1f / Mathf.Sqrt(spawnedCount), 0.5f, 1f);
                }

                // Usar AudioSystem diretamente com volume ajustado
                AudioSystemHelper.PlaySound(soundToPlay, transform.position);
        
                // Ou usar SoundBuilder se quiser mais controle:
                // _audioManager.CreateSound()
                //     .WithSoundData(soundToPlay)
                //     .WithPosition(transform.position)
                //     .WithVolumeMultiplier(volumeMultiplier)
                //     .Play();

                DebugUtility.LogVerbose<InputSpawnerComponent>($"Som de tiro tocado: {soundToPlay.clip?.name} (x{spawnedCount}, volume: {volumeMultiplier:F2})", "cyan", this);
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
            DebugUtility.LogVerbose<InputSpawnerComponent>($"Estratégia alterada para '{type}' em '{name}'.", "cyan", this);
        }
        // Método para configurar som específico em runtime
        public void SetShootSound(SoundData newShootSound)
        {
            if (_activeStrategy != null)
            {
                // Esta é uma limitação - como as estratégias são structs/classes serializáveis,
                // não podemos modificar diretamente. Precisamos de uma abordagem diferente.
                DebugUtility.LogWarning<InputSpawnerComponent>("Não é possível modificar o som da estratégia em runtime. Configure no Inspector.", this);
            }
        }

        // Método para habilitar/desabilitar sons em runtime
        public void SetShootSoundsEnabled(bool enabled)
        {
            enableShootSounds = enabled;
            DebugUtility.LogVerbose<InputSpawnerComponent>($"Sons de tiro {(enabled ? "habilitados" : "desabilitados")}", "yellow", this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying && poolData != null)
            {
                if (_activeStrategy == null)
                    SetStrategy(strategyType);

                if (_activeStrategy == null)
                    return;

                var basePosition = transform.position;
                var baseDirection = transform.forward;

                var previewList = _activeStrategy.GetSpawnData(basePosition, baseDirection);

                Gizmos.color = Color.cyan;

                foreach (var spawnData in previewList)
                {
                    Gizmos.DrawWireSphere(spawnData.Position, 0.1f);
                    Gizmos.DrawLine(spawnData.Position, spawnData.Position + spawnData.Direction * 0.5f);
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(basePosition, 0.15f);
                Gizmos.DrawLine(basePosition, basePosition + baseDirection * 1f);

                // Mostrar info de áudio no Gizmo
                if (_activeStrategy.HasShootSound && enableShootSounds)
                {
                    UnityEditor.Handles.Label(basePosition + Vector3.up * 0.3f,
                        $"Som: {_activeStrategy.ShootSound.clip?.name ?? "Nenhum"}");
                }
            }
        }
#endif
    }
}