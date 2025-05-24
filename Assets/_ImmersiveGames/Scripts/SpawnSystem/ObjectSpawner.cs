using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private bool debugMode;
        [SerializeField] private bool isGameActive = true;

        private SpawnStrategy spawnStrategy;
        private SpawnParameters parameters;
        private float fireRate;
        private bool isFiring;
        private float fireTimer;
        private float lastLogTime;

        private void Awake()
        {
            if (mainCamera == null || firePoint == null)
            {
                DebugUtility.LogError<ObjectSpawner>("Configuração inválida (mainCamera ou firePoint).", this);
                enabled = false;
                return;
            }
            if (!gameObject.activeInHierarchy)
            {
                DebugUtility.LogWarning<ObjectSpawner>("GameObject está inativo.", this);
                enabled = false;
                return;
            }
            if (debugMode)
            {
                DebugUtility.LogVerbose<ObjectSpawner>($"ObjectSpawner inicializado. isGameActive: {isGameActive}", "blue", this);
            }
        }

        public void SetStrategy(SpawnStrategy strategy)
        {
            if (strategy == null || strategy.ProjectileData == null)
            {
                DebugUtility.LogError<ObjectSpawner>("SpawnStrategy ou ProjectileData nulo.", this);
                return;
            }

            DebugUtility.LogVerbose<ObjectSpawner>($"Registrando pool para '{strategy.ProjectileData.ObjectName}'.", "blue", this);
            PoolManager.Instance.RegisterPool(strategy.ProjectileData);

            spawnStrategy = strategy;
            parameters = new SpawnParameters
            {
                poolName = strategy.ProjectileData.ObjectName,
                spawnPosition = firePoint.position,
                mainCamera = mainCamera
            };
            fireRate = strategy.FireRate;
            fireTimer = 0f;

            if (debugMode)
            {
                DebugUtility.LogVerbose<ObjectSpawner>($"Estratégia alterada para {strategy.name}. FireRate: {fireRate}", "cyan", this);
            }
        }

        private void Update()
        {
            if (!enabled)
            {
                if (debugMode)
                {
                    DebugUtility.LogVerbose<ObjectSpawner>("Componente desativado.", "gray", this);
                }
                return;
            }

            if (!isGameActive)
            {
                if (debugMode)
                {
                    DebugUtility.LogVerbose<ObjectSpawner>("Jogo inativo, spawn interrompido.", "gray", this);
                }
                return;
            }

            if (spawnStrategy == null)
            {
                DebugUtility.LogWarning<ObjectSpawner>("SpawnStrategy nulo.", this);
                return;
            }

            fireTimer += Time.deltaTime;

            if (debugMode && (isFiring || Time.time - lastLogTime >= 0.5f))
            {
                DebugUtility.LogVerbose<ObjectSpawner>($"Update: isFiring: {isFiring}, fireTimer: {fireTimer}, fireRate: {fireRate}", "white", this);
                lastLogTime = Time.time;
            }

            if (isFiring && fireTimer >= fireRate)
            {
                parameters.spawnPosition = firePoint.position;
                if (debugMode)
                {
                    DebugUtility.LogVerbose<ObjectSpawner>($"Tentando spawnar com estratégia {parameters.poolName} em {parameters.spawnPosition}.", "yellow", this);
                }
                spawnStrategy.Spawn(PoolManager.Instance, parameters);
                fireTimer -= fireRate;
            }
        }

        public void StartFiring()
        {
            isFiring = true;
            fireTimer = fireRate;
            if (debugMode)
            {
                DebugUtility.LogVerbose<ObjectSpawner>("Disparo iniciado.", "green", this);
            }
        }

        public void StopFiring()
        {
            isFiring = false;
            if (debugMode)
            {
                DebugUtility.LogVerbose<ObjectSpawner>("Disparo interrompido.", "red", this);
            }
        }

        public void SetGameActive(bool active)
        {
            isGameActive = active;
            if (debugMode)
            {
                DebugUtility.LogVerbose<ObjectSpawner>($"Estado do jogo alterado para {(active ? "ativo" : "inativo")}.", "gray", this);
            }
        }
    }
}