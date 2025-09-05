using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SpawnSystems.Events;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [RequireComponent(typeof(Transform))]
    public class SpawnSystem : MonoBehaviour
    {
        #region Configurações
        [Serializable]
        public class PoolConfig
        {
            public PoolData poolData;
            public TriggerConfig triggerConfig = new TriggerConfig();
            public StrategyConfig strategyConfig = new StrategyConfig();
            public Transform spawnPoint; // Ponto inicial de spawn (opcional)
            public RotationMode rotationMode = RotationMode.Follow; // Modo de rotação
        }

        [Serializable]
        public class TriggerConfig
        {
            [Tooltip("Supported types: KeyPress. Add new types in SpawnTriggerFactory.SupportedTriggerTypes.")]
            public string type = "KeyPress";
            public KeyCode key = KeyCode.Space;
        }

        [Serializable]
        public class StrategyConfig
        {
            [Tooltip("Supported types: SinglePoint, Circular. Add new types in SpawnStrategyFactory.SupportedStrategyTypes.")]
            public string type = "SinglePoint";
            public int spawnCount = 3;
            public float radius = 2f; // Para Circular
            public float spacing = 360f; // Para Circular (graus entre objetos)
            public float angleOffset; // Offset angular para Circular
        }

        public enum RotationMode
        {
            Follow, // Mesma direção do spawnPoint
            Opposite, // Direção oposta ao spawnPoint
            RadialInward, // Voltado para o centro (ex.: Circular)
            RadialOutward // Voltado para fora (ex.: Circular)
        }
        #endregion

        [SerializeField] private List<PoolConfig> poolConfigs;

        private readonly Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();
        private readonly Dictionary<PoolConfig, ISpawnTrigger> _triggers = new Dictionary<PoolConfig, ISpawnTrigger>();
        private readonly Dictionary<PoolConfig, ISpawnStrategy> _strategies = new Dictionary<PoolConfig, ISpawnStrategy>();

        private void Awake() => InitializeSystems();

        private void Update() => CheckAndSpawnObjects();

        #region Inicialização
        private void InitializeSystems()
        {
            if (poolConfigs == null || poolConfigs.Count == 0)
            {
                DebugUtility.LogError<SpawnSystem>("No PoolConfigs assigned.", this);
                return;
            }

            foreach (var config in poolConfigs)
            {
                if (config.poolData == null)
                {
                    DebugUtility.LogError<SpawnSystem>("Null PoolData in PoolConfig.", this);
                    continue;
                }

                InitializeSystemForConfig(config);
            }
        }

        private void InitializeSystemForConfig(PoolConfig config)
        {
            if (!InitializePool(config))
                return;

            InitializeTrigger(config);
            InitializeStrategy(config);
        }

        private bool InitializePool(PoolConfig config)
        {
            PoolManager.Instance.RegisterPool(config.poolData);
            var pool = PoolManager.Instance.GetPool(config.poolData.ObjectName);

            if (pool == null)
            {
                DebugUtility.LogError<SpawnSystem>($"Failed to register pool '{config.poolData.ObjectName}'.", this);
                return false;
            }

            pool.SetAllowMultipleGetsInFrame(true);
            _pools[config.poolData.ObjectName] = pool;
            LogSuccess("Pool", config.poolData.ObjectName);
            return true;
        }

        private void InitializeTrigger(PoolConfig config)
        {
            try
            {
                var trigger = SpawnTriggerFactory.CreateTrigger(config.triggerConfig);
                if (trigger == null)
                {
                    LogInitFailure("trigger", config.triggerConfig.type, config.poolData.ObjectName);
                    return;
                }

                _triggers[config] = trigger;
                LogSuccess("Trigger", $"{config.triggerConfig.type} for pool '{config.poolData.ObjectName}'");
            }
            catch (Exception ex)
            {
                LogInitException("trigger", config.triggerConfig.type, config.poolData.ObjectName, ex.Message);
            }
        }

        private void InitializeStrategy(PoolConfig config)
        {
            try
            {
                var strategy = SpawnStrategyFactory.CreateStrategy(config.strategyConfig);
                if (strategy == null)
                {
                    LogInitFailure("strategy", config.strategyConfig.type, config.poolData.ObjectName);
                    return;
                }

                _strategies[config] = strategy;
                LogSuccess("Strategy", $"{config.strategyConfig.type} for pool '{config.poolData.ObjectName}'");
            }
            catch (Exception ex)
            {
                LogInitException("strategy", config.strategyConfig.type, config.poolData.ObjectName, ex.Message);
            }
        }
        #endregion

        #region Spawn de Objetos
        private void CheckAndSpawnObjects()
        {
            foreach (var config in poolConfigs)
            {
                if (IsReadyToSpawn(config))
                    SpawnObjects(config);
            }
        }

        private bool IsReadyToSpawn(PoolConfig config) =>
            _triggers.TryGetValue(config, out var trigger) &&
            _strategies.ContainsKey(config) &&
            trigger.ShouldSpawn();

        private void SpawnObjects(PoolConfig config)
        {
            if (!_pools.TryGetValue(config.poolData.ObjectName, out var pool) ||
                !_strategies.TryGetValue(config, out var strategy))
            {
                var errorMessage = $"Pool '{config.poolData.ObjectName}' or strategy not found.";
                DebugUtility.LogError<SpawnSystem>(errorMessage, this);
                EventBus<SpawnFailureEvent>.Raise(new SpawnFailureEvent(
                    this, GetComponent<IActor>(), config, errorMessage, GetSpawnPosition(config), gameObject));
                return;
            }

            var position = GetSpawnPosition(config);
            var direction = GetSpawnDirection(config);
            var spawner = GetComponent<IActor>();

            var spawnedObjects = strategy.Spawn(pool, position, direction, spawner, config.rotationMode);

            if (spawnedObjects != null && spawnedObjects.Count > 0)
            {
                EventBus<SpawnSuccessEvent>.Raise(new SpawnSuccessEvent(
                    this, spawner, config, spawnedObjects, position, gameObject));
                // Opção para FilteredEventBus (descomentar se desejar filtrar per spawner):
                // FilteredEventBus<SpawnSuccessEvent>.RaiseFiltered(
                //     new SpawnSuccessEvent(this, spawner, config, spawnedObjects, position, gameObject),
                //     spawner ?? gameObject);
                LogSpawnResult(spawnedObjects.Count, config, position, spawner);
            }
            else
            {
                var errorMessage = $"Failed to spawn objects from pool '{config.poolData.ObjectName}'. No objects returned.";
                DebugUtility.LogError<SpawnSystem>(errorMessage, this);
                EventBus<SpawnFailureEvent>.Raise(new SpawnFailureEvent(
                    this, spawner, config, errorMessage, position, gameObject));
                // Opção para FilteredEventBus (descomentar se desejar filtrar per spawner):
                // FilteredEventBus<SpawnFailureEvent>.RaiseFiltered(
                //     new SpawnFailureEvent(this, spawner, config, errorMessage, position, gameObject),
                //     spawner ?? gameObject);
            }
        }

        private Vector3 GetSpawnPosition(PoolConfig config) =>
            config.spawnPoint != null ? config.spawnPoint.position : transform.position;

        private Vector3 GetSpawnDirection(PoolConfig config) =>
            config.spawnPoint != null ? config.spawnPoint.forward : transform.forward;
        #endregion

        #region API Pública
        /// <summary>
        /// Altera o ponto de spawn para um pool específico
        /// </summary>
        public void SetSpawnPoint(string poolName, Transform newSpawnPoint)
        {
            var config = FindPoolConfig(poolName);
            if (config != null)
            {
                config.spawnPoint = newSpawnPoint;
                DebugUtility.Log<SpawnSystem>($"Spawn point updated for pool '{poolName}' to {(newSpawnPoint != null ? newSpawnPoint.position.ToString() : "null")}.", "cyan", this);
            }
        }

        /// <summary>
        /// Altera os parâmetros de estratégia para um pool específico
        /// </summary>
        public void SetStrategyParameters(string poolName, StrategyConfig newConfig)
        {
            var config = FindPoolConfig(poolName);
            if (config == null) return;

            config.strategyConfig = newConfig;

            try
            {
                var strategy = SpawnStrategyFactory.CreateStrategy(newConfig);
                if (strategy != null)
                {
                    _strategies[config] = strategy;
                    DebugUtility.Log<SpawnSystem>($"Strategy parameters updated for pool '{poolName}'.", "cyan", this);
                }
                else
                {
                    DebugUtility.LogError<SpawnSystem>($"Failed to create strategy '{newConfig.type}' for pool '{poolName}'.", this);
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<SpawnSystem>($"Error updating strategy for pool '{poolName}': {ex.Message}", this);
            }
        }
        #endregion

        #region Utilitários
        private PoolConfig FindPoolConfig(string poolName)
        {
            var config = poolConfigs.Find(c => c.poolData?.ObjectName == poolName);
            if (config == null)
                DebugUtility.LogError<SpawnSystem>($"Pool '{poolName}' not found in PoolConfigs.", this);

            return config;
        }

        private void LogSuccess(string componentType, string details) =>
            DebugUtility.Log<SpawnSystem>($"{componentType} '{details}' initialized.", "green", this);

        private void LogInitFailure(string componentType, string typeName, string poolName) =>
            DebugUtility.LogError<SpawnSystem>($"Failed to create {componentType} '{typeName}' for pool '{poolName}'.", this);

        private void LogInitException(string componentType, string typeName, string poolName, string errorMessage) =>
            DebugUtility.LogError<SpawnSystem>($"Error creating {componentType} '{typeName}' for pool '{poolName}': {errorMessage}", this);

        private void LogSpawnResult(int count, PoolConfig config, Vector3 position, IActor spawner) =>
            DebugUtility.Log<SpawnSystem>(
                $"Spawned {count} objects from pool '{config.poolData.ObjectName}' " +
                $"at {position} using strategy {config.strategyConfig.type} " +
                $"and rotation {config.rotationMode}. " +
                $"Spawner: {(spawner != null ? spawner.ToString() : "null")}. " +
                $"Source: {gameObject.name}. [SpawnSuccessEvent raised]",
                "green", this);
        #endregion
    }
}