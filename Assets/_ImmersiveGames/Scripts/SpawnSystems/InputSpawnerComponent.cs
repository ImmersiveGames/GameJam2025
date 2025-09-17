using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.ActorSystems;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    /// <summary>
    /// Componente para spawn de objetos do pool, na direção do Transform.forward do spawner.
    /// Usa Input System para ação configurável por jogador, compatível com multiplayer local.
    /// Inclui cooldown e estratégias modulares de posicionamento selecionáveis.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    [DebugLevel(DebugLevel.Verbose)]
    public class InputSpawnerComponent : MonoBehaviour
    {
        [Header("Pool Config")]
        [SerializeField] private PoolData poolData; // Dados do pool, define ObjectConfigs

        [Header("Input Config")]
        [SerializeField] private string actionName = "Spawn"; // Nome da ação no InputActionMap

        [Header("Cooldown Config")]
        [SerializeField, Min(0f)] private float cooldown = 0.5f; // Tempo de cooldown em segundos

        [Header("Spawn Strategy Config")]
        [SerializeField] private SpawnStrategyType strategyType = SpawnStrategyType.Single; // Tipo de estratégia
        [SerializeField] private SingleSpawnStrategy singleStrategy = new SingleSpawnStrategy();
        [SerializeField] private MultipleLinearSpawnStrategy multipleLinearStrategy = new MultipleLinearSpawnStrategy();
        [SerializeField] private CircularSpawnStrategy circularStrategy = new CircularSpawnStrategy();

        private ISpawnStrategy _activeStrategy; // Estratégia ativa
        private ObjectPool _pool;
        private PlayerInput _playerInput;
        private InputAction _spawnAction;
        private float _lastShotTime = -Mathf.Infinity; // Inicializa para permitir disparo imediato
        
        private bool _isGameActive;

        private void Awake()
        {
            // Inicializa referências
            _playerInput = GetComponent<PlayerInput>();
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
            // Subscrição a eventos para desacoplar do GameManager
            EventBus<GameStartEvent>.Register(new EventBinding<GameStartEvent>(OnGameStart));
            EventBus<GamePauseEvent>.Register(new EventBinding<GamePauseEvent>(OnGamePause));
            _isGameActive = false;  // Inicializa como inativo
            

            // Registra o pool com base no ObjectName do PoolData
            PoolManager.Instance.RegisterPool(poolData);
            _pool = PoolManager.Instance.GetPool(poolData.ObjectName);

            if (_pool == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"Pool não registrado ou encontrado para '{poolData.ObjectName}' em '{name}'.", this);
                enabled = false;
                return;
            }

            // Inicializa estratégias, se necessário
            singleStrategy ??= new SingleSpawnStrategy();
            multipleLinearStrategy ??= new MultipleLinearSpawnStrategy();
            circularStrategy ??= new CircularSpawnStrategy();

            // Configura a estratégia ativa com base no tipo selecionado
            SetStrategy(strategyType);

            if (_activeStrategy == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"Estratégia de spawn não configurada em '{name}'.", this);
                enabled = false;
                return;
            }

            // Configura a ação de input
            _spawnAction = _playerInput.actions.FindAction(actionName);
            if (_spawnAction == null)
            {
                DebugUtility.LogError<InputSpawnerComponent>($"Ação '{actionName}' não encontrada no InputActionMap de '{name}'.", this);
                enabled = false;
                return;
            }

            _spawnAction.performed += OnSpawnPerformed;
            DebugUtility.LogVerbose<InputSpawnerComponent>($"InputSpawnerComponent inicializado em '{name}' com ação '{actionName}', PoolData '{poolData.ObjectName}', cooldown {cooldown}s e estratégia '{strategyType}'.", "cyan", this);
        }

        private void OnDestroy()
        {
            if (_spawnAction != null)
            {
                _spawnAction.performed -= OnSpawnPerformed;
            }
            // Desinscrever eventos
            EventBus<GameStartEvent>.Unregister(new EventBinding<GameStartEvent>(OnGameStart));
            EventBus<GamePauseEvent>.Unregister(new EventBinding<GamePauseEvent>(OnGamePause));
            DebugUtility.LogVerbose<InputSpawnerComponent>($"InputSpawnerComponent destruído em '{name}'.", "blue", this);
        }
        private void OnGameStart(GameStartEvent evt)
        {
            _isGameActive = true;
        }

        private void OnGamePause(GamePauseEvent evt)
        {
            _isGameActive = !evt.IsPaused;
        }

        private void OnSpawnPerformed(InputAction.CallbackContext context)
        {
            if (!_isGameActive) return;
            if (_pool == null)
            {
                DebugUtility.LogWarning<InputSpawnerComponent>($"Pool nulo em '{name}'.", this);
                return;
            }

            // Verifica se o cooldown expirou
            if (Time.time < _lastShotTime + cooldown)
            {
                DebugUtility.LogVerbose<InputSpawnerComponent>($"[{name}] Disparo bloqueado por cooldown. Tempo restante: {(_lastShotTime + cooldown - Time.time):F3}s.", "yellow", this);
                return;
            }

            var basePosition = transform.position; // Posição base do spawner
            var baseDirection = transform.forward; // Direção base do Transform.forward
            var spawner = GetComponent<IActor>(); // Usa IActor como spawner

            // Obtém os dados de spawn da estratégia ativa
            var spawnDataList = _activeStrategy.GetSpawnData(basePosition, baseDirection);

            bool success = false;
            foreach (var spawnData in spawnDataList)
            {
                var poolable = _pool.GetObject(spawnData.Position, spawner, spawnData.Direction);
                if (poolable != null)
                {
                    success = true;
                    DebugUtility.LogVerbose<InputSpawnerComponent>($"[{name}] Spawned '{poolable.GetGameObject().name}' em '{spawnData.Position}' na direção '{spawnData.Direction}'.", "green", this);
                }
                else
                {
                    DebugUtility.LogWarning<InputSpawnerComponent>($"Falha ao spawnar objeto em '{name}' na posição '{spawnData.Position}'. Pool esgotado?", this);
                }
            }

            if (success)
            {
                _lastShotTime = Time.time; // Atualiza o tempo do último disparo apenas se pelo menos um spawn sucedeu
            }
        }

        /// <summary>
        /// Define a estratégia de spawn ativa com base no tipo fornecido.
        /// Pode ser chamado em runtime para trocar estratégias dinamicamente.
        /// </summary>
        public void SetStrategy(SpawnStrategyType type)
        {
            strategyType = type;
            _activeStrategy = type switch
            {
                SpawnStrategyType.Single => singleStrategy,
                SpawnStrategyType.MultipleLinear => multipleLinearStrategy,
                SpawnStrategyType.Circular => circularStrategy,
                _ => singleStrategy // Fallback para Single
            };
            DebugUtility.LogVerbose<InputSpawnerComponent>($"Estratégia alterada para '{type}' em '{name}'.", "cyan", this);
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
                    // desenha a posição
                    Gizmos.DrawWireSphere(spawnData.Position, 0.1f);

                    // desenha a direção
                    Gizmos.DrawLine(spawnData.Position, spawnData.Position + spawnData.Direction * 0.5f);
                }

                // desenha a base do spawner
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(basePosition, 0.15f);
                Gizmos.DrawLine(basePosition, basePosition + baseDirection * 1f);
            }
        }
#endif

    }
    
}