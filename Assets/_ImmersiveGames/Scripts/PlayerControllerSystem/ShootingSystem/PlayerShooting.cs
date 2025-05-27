using _ImmersiveGames.Scripts.Predicates;
using _ImmersiveGames.Scripts.SpawnSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class PlayerShooting : SpawnPoint
    {
        private PlayerInput _playerInput;

        protected override void Awake()
        {
            base.Awake();
            _playerInput = GetComponent<PlayerInput>();

            TryBindInputSystemPredicate();
        }

        private void TryBindInputSystemPredicate()
        {
            if (spawnData?.TriggerStrategy is PredicateTriggerSo { predicate: InputSystemHoldPredicateSo inputPredicateHold } &&
                _playerInput)
            {
                inputPredicateHold.Bind(_playerInput.actions);
                Debug.Log($"[PlayerInputSpawnPoint] 🔗 Bind da ação '{inputPredicateHold.actionName}' feito com sucesso.");
            }
            if (spawnData?.TriggerStrategy is PredicateTriggerSo { predicate: InputSystemPredicateSo inputPredicate } &&
                _playerInput)
            {
                inputPredicate.Bind(_playerInput.actions);
                Debug.Log($"[PlayerInputSpawnPoint] 🔗 Bind da ação '{inputPredicate.actionName}' feito com sucesso.");
            }
        }
    }
}

/*using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.PoolSystemOld;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class PlayerShooting : MonoBehaviour
    {
        [SerializeField, Tooltip("Câmera principal usada para mirar")]
        private Camera mainCamera;

        [SerializeField, Tooltip("Ponto de origem dos projéteis")]
        private Transform firePoint;

        [SerializeField, Tooltip("Prefab do projétil usado por todos os pools")]
        private GameObject projectilePrefab;

        [SerializeField, Tooltip("Lista de estratégias de tiro disponíveis")]
        private List<ShootingStrategy> shootingStrategies;

        [SerializeField, Tooltip("Índice da estratégia inicial (ex.: 0 para a primeira)")]
        private int initialStrategyIndex = 0;

        [SerializeField, Tooltip("Ativar logs para depuração")]
        private bool debugMode;

        private bool _isFiring;
        private PlayerInputActions _inputActions;
        private IShootingStrategy _currentStrategy;
        private Dictionary<ShootingStrategy, ProjectileObjectPool> _strategyPools;

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
            _strategyPools = new Dictionary<ShootingStrategy, ProjectileObjectPool>();

            if (mainCamera == null)
            {
                DebugUtility.LogError<PlayerShooting>("MainCamera não configurada.", this);
                enabled = false;
                return;
            }
            if (firePoint == null)
            {
                DebugUtility.LogError<PlayerShooting>("FirePoint não configurado.", this);
                enabled = false;
                return;
            }
            if (projectilePrefab == null)
            {
                DebugUtility.LogError<PlayerShooting>("ProjectilePrefab não configurado.", this);
                enabled = false;
                return;
            }
            if (shootingStrategies == null || shootingStrategies.Count == 0)
            {
                DebugUtility.LogError<PlayerShooting>("Nenhuma estratégia de tiro configurada.", this);
                enabled = false;
                return;
            }

            // Criar um pool para cada estratégia
            foreach (var strategy in shootingStrategies)
            {
                if (strategy == null || strategy.projectileData == null)
                {
                    DebugUtility.LogWarning<PlayerShooting>($"Estratégia {strategy?.name} ou seu ProjectileData está nulo.", this);
                    continue;
                }
                if (strategy.projectileData.modelPrefab == null)
                {
                    DebugUtility.LogWarning<PlayerShooting>($"ProjectileData de {strategy.name} não tem modelPrefab configurado.", this);
                }

                GameObject poolObj = new GameObject($"Pool_{strategy.name}");
                poolObj.transform.SetParent(transform);
                ProjectileObjectPool pool = poolObj.AddComponent<ProjectileObjectPool>();
                pool.Initialize(projectilePrefab, strategy.projectileData);
                _strategyPools.Add(strategy, pool);
            }

            // Inicializar estratégia inicial
            SetInitialStrategy();
        }

        private void SetInitialStrategy()
        {
            if (_strategyPools.Count == 0)
            {
                DebugUtility.LogWarning<PlayerShooting>("Nenhum pool de projéteis válido configurado.", this);
                return;
            }

            int index = Mathf.Clamp(initialStrategyIndex, 0, shootingStrategies.Count - 1);
            _currentStrategy = shootingStrategies[index];
            _currentStrategy.Initialize(firePoint, mainCamera, _strategyPools[shootingStrategies[index]]);
            if (debugMode)
            {
                DebugUtility.LogVerbose<PlayerShooting>($"Estratégia inicial configurada: {shootingStrategies[index].name}", "cyan", this);
            }
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Fire.performed += OnFirePerformed;
            _inputActions.Player.Fire.canceled += OnFireCanceled;
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
            _inputActions.Player.Fire.performed -= OnFirePerformed;
            _inputActions.Player.Fire.canceled -= OnFireCanceled;
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            _isFiring = true;
            if (debugMode)
            {
                DebugUtility.LogVerbose<PlayerShooting>("Disparo iniciado.", "green", this);
            }
        }

        private void OnFireCanceled(InputAction.CallbackContext context)
        {
            _isFiring = false;
            if (debugMode)
            {
                DebugUtility.LogVerbose<PlayerShooting>("Disparo interrompido.", "red", this);
            }
        }

        private void Update()
        {
            if (_currentStrategy != null && GameManager.Instance.ShouldPlayingGame())
            {
                _currentStrategy.Fire(_isFiring, Time.deltaTime);
                if (debugMode && _isFiring)
                {
                    DebugUtility.LogVerbose<PlayerShooting>($"Disparando com estratégia {_currentStrategy.GetType().Name} de {firePoint.position}.", "yellow", this);
                }
            }
        }

        public void SetStrategy(int strategyIndex)
        {
            if (strategyIndex >= 0 && strategyIndex < shootingStrategies.Count)
            {
                _currentStrategy = shootingStrategies[strategyIndex];
                _currentStrategy.Initialize(firePoint, mainCamera, _strategyPools[shootingStrategies[strategyIndex]]);
                if (debugMode)
                {
                    DebugUtility.LogVerbose<PlayerShooting>($"Estratégia alterada para {shootingStrategies[strategyIndex].name}", "cyan", this);
                }
            }
            else
            {
                DebugUtility.LogWarning<PlayerShooting>($"Índice de estratégia inválido: {strategyIndex}. Mantendo estratégia atual.", this);
            }
        }
    }
}*/