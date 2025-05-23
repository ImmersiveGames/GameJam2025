using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.PoolSystem;
using System.Collections.Generic;

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

        private bool _isFiring;
        private PlayerInputActions _inputActions;
        private IShootingStrategy _currentStrategy;
        private Dictionary<ShootingStrategy, ProjectileObjectPool> _strategyPools;

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
            _strategyPools = new Dictionary<ShootingStrategy, ProjectileObjectPool>();

            // Criar um pool para cada estratégia
            foreach (var strategy in shootingStrategies)
            {
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
            if (shootingStrategies == null || shootingStrategies.Count == 0)
            {
                Debug.LogWarning("Nenhuma estratégia de tiro configurada em PlayerShooting.", this);
                return;
            }

            int index = Mathf.Clamp(initialStrategyIndex, 0, shootingStrategies.Count - 1);
            _currentStrategy = shootingStrategies[index];
            _currentStrategy.Initialize(firePoint, mainCamera, _strategyPools[shootingStrategies[index]]);
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
        }

        private void OnFireCanceled(InputAction.CallbackContext context)
        {
            _isFiring = false;
        }
        

        private void Update()
        {
            if (_currentStrategy != null && GameManager.Instance.ShouldPlayingGame())
            {
                _currentStrategy.Fire(_isFiring, Time.deltaTime);
            }
        }

        public void SetStrategy(int strategyIndex)
        {
            if (strategyIndex >= 0 && strategyIndex < shootingStrategies.Count)
            {
                _currentStrategy = shootingStrategies[strategyIndex];
                _currentStrategy.Initialize(firePoint, mainCamera, _strategyPools[shootingStrategies[strategyIndex]]);
                Debug.Log($"Estratégia alterada para {shootingStrategies[strategyIndex].name}");
            }
            else
            {
                Debug.LogWarning($"Índice de estratégia inválido: {strategyIndex}. Mantendo estratégia atual.", this);
            }
        }
    }
}