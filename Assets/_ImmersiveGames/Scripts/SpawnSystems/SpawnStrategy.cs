using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    /// <summary>
    /// Factory para criação de estratégias de spawn baseado na configuração
    /// </summary>
    public static class SpawnStrategyFactory
    {
        /// <summary>
        /// Lista de tipos de estratégia suportados
        /// </summary>
        public static readonly List<string> SupportedStrategyTypes = new List<string> { "SinglePoint", "Circular" };

        /// <summary>
        /// Cria uma estratégia de spawn com base na configuração fornecida
        /// </summary>
        /// <param name="config">Configuração da estratégia</param>
        /// <returns>Instância da estratégia de spawn ou null se inválida</returns>
        public static ISpawnStrategy CreateStrategy(SpawnSystem.StrategyConfig config)
        {
            if (!ValidateStrategyConfig(config))
                return null;

            string type = config.type.ToLower();

            return type switch
            {
                "singlepoint" => new SinglePointSpawnStrategy(config.spawnCount),
                "circular" => CreateCircularStrategy(config),
                _ => LogUnknownStrategyError(config.type)
            };
        }

        private static bool ValidateStrategyConfig(SpawnSystem.StrategyConfig config)
        {
            if (config == null)
            {
                DebugUtility.LogError(typeof(SpawnStrategyFactory), "StrategyConfig is null.");
                return false;
            }

            if (config.spawnCount < 1)
            {
                DebugUtility.LogError(typeof(SpawnStrategyFactory), $"Invalid spawnCount: {config.spawnCount}. Must be at least 1.");
                return false;
            }

            if (!SupportedStrategyTypes.Contains(config.type, System.StringComparer.OrdinalIgnoreCase))
            {
                DebugUtility.LogError(typeof(SpawnStrategyFactory), 
                    $"Unknown strategy type: {config.type}. Supported types: {string.Join(", ", SupportedStrategyTypes)}.");
                return false;
            }

            return true;
        }

        private static CircularSpawnStrategy CreateCircularStrategy(SpawnSystem.StrategyConfig config)
        {
            if (config.radius < 0.1f)
            {
                DebugUtility.LogError(typeof(SpawnStrategyFactory), $"Invalid radius: {config.radius}. Must be at least 0.1.");
                return null;
            }

            return new CircularSpawnStrategy(config.spawnCount, config.radius, config.spacing, config.angleOffset);
        }

        private static ISpawnStrategy LogUnknownStrategyError(string strategyType)
        {
            DebugUtility.LogError(typeof(SpawnStrategyFactory), 
                $"Unknown strategy type: {strategyType}. Supported types: {string.Join(", ", SupportedStrategyTypes)}.");
            return null;
        }
    }

    /// <summary>
    /// Base abstrata para estratégias de spawn compartilharem código comum
    /// </summary>
    public abstract class BaseSpawnStrategy : ISpawnStrategy
    {
        protected readonly int count;

        protected BaseSpawnStrategy(int count)
        {
            this.count = count;
        }

        public abstract List<IPoolable> Spawn(ObjectPool pool, Vector3 position, Vector3 direction, 
            IActor spawner, SpawnSystem.RotationMode rotationMode);

        protected Vector3 GetTopDownPosition(Vector3 position)
        {
            var topDownPosition = position;
            topDownPosition.y = 0f;  // Visão top-down (XZ)
            return topDownPosition;
        }

        protected void ActivatePoolable(ObjectPool pool, IPoolable poolable, Vector3 position, 
            IActor spawner, Quaternion rotation, int index)
        {
            if (poolable?.GetGameObject() == null)
            {
                DebugUtility.LogError(GetType(), $"Poolable object {index + 1}/{count} is null or has no GameObject.");
                return;
            }

            pool.ActivateObject(poolable, position, spawner);
            poolable.GetGameObject().transform.rotation = rotation;
        }

        protected Quaternion GetRotation(Vector3 direction, SpawnSystem.RotationMode rotationMode, 
            Vector3 spawnPosition, Vector3 referencePosition)
        {
            // Garantir plano XZ
            direction.y = 0f;

            return rotationMode switch
            {
                SpawnSystem.RotationMode.Follow => Quaternion.LookRotation(direction.normalized, Vector3.up),
                SpawnSystem.RotationMode.Opposite => Quaternion.LookRotation(-direction.normalized, Vector3.up),
                SpawnSystem.RotationMode.RadialInward => GetRadialInwardRotation(spawnPosition, referencePosition),
                SpawnSystem.RotationMode.RadialOutward => GetRadialOutwardRotation(spawnPosition, referencePosition),
                _ => LogInvalidRotationMode(rotationMode)
            };
        }

        protected virtual Quaternion GetRadialInwardRotation(Vector3 spawnPosition, Vector3 referencePosition)
        {
            Vector3 inwardDir = (referencePosition - spawnPosition).normalized;
            inwardDir.y = 0f;
            return Quaternion.LookRotation(inwardDir, Vector3.up);
        }

        protected virtual Quaternion GetRadialOutwardRotation(Vector3 spawnPosition, Vector3 referencePosition)
        {
            Vector3 outwardDir = (spawnPosition - referencePosition).normalized;
            outwardDir.y = 0f;
            return Quaternion.LookRotation(outwardDir, Vector3.up);
        }

        private Quaternion LogInvalidRotationMode(SpawnSystem.RotationMode rotationMode)
        {
            DebugUtility.LogError(GetType(), $"Unknown rotation mode: {rotationMode}. Using identity.");
            return Quaternion.identity;
        }
    }

    /// <summary>
    /// Estratégia que spawna objetos em um único ponto
    /// </summary>
    public class SinglePointSpawnStrategy : BaseSpawnStrategy
    {
        public SinglePointSpawnStrategy(int count) : base(count) { }

        public override List<IPoolable> Spawn(ObjectPool pool, Vector3 position, Vector3 direction, 
            IActor spawner, SpawnSystem.RotationMode rotationMode)
        {
            var spawnedObjects = pool.GetMultipleObjects(count, position, spawner, false);
            var rotation = GetRotation(direction, rotationMode, position, position);
            var spawnPosition = GetTopDownPosition(position);

            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                ActivatePoolable(pool, spawnedObjects[i], spawnPosition, spawner, rotation, i);
                DebugUtility.Log<SinglePointSpawnStrategy>(
                    $"Spawned object {i + 1}/{count} at {spawnPosition}, rotation: {rotation.eulerAngles}.", "cyan");
            }

            return spawnedObjects;
        }

        protected override Quaternion GetRadialInwardRotation(Vector3 spawnPosition, Vector3 referencePosition)
        {
            // Para SinglePoint, RadialInward/Outward não faz sentido; usa Follow como fallback
            DebugUtility.Log<SinglePointSpawnStrategy>("RadialInward not applicable for SinglePoint. Using Follow.", "yellow");
            return Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        protected override Quaternion GetRadialOutwardRotation(Vector3 spawnPosition, Vector3 referencePosition)
        {
            // Para SinglePoint, RadialInward/Outward não faz sentido; usa Follow como fallback
            DebugUtility.Log<SinglePointSpawnStrategy>("RadialOutward not applicable for SinglePoint. Using Follow.", "yellow");
            return Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }
    }

    /// <summary>
    /// Estratégia que spawna objetos em um padrão circular
    /// </summary>
    public class CircularSpawnStrategy : BaseSpawnStrategy
    {
        private readonly float _radius;
        private readonly float _spacing;
        private readonly float _angleOffset;

        public CircularSpawnStrategy(int count, float radius, float spacing, float angleOffset) : base(count)
        {
            _radius = radius;
            _spacing = Mathf.Clamp(spacing, 0f, 360f);
            _angleOffset = Mathf.Repeat(angleOffset, 360f);
        }

        public override List<IPoolable> Spawn(ObjectPool pool, Vector3 position, Vector3 direction, 
            IActor spawner, SpawnSystem.RotationMode rotationMode)
        {
            var spawnedObjects = pool.GetMultipleObjects(count, position, spawner, false);
            float angleStep = _spacing / Mathf.Max(1, count);

            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                var spawnPosition = CalculateCircularPosition(position, i, angleStep);
                var rotation = GetRotation(direction, rotationMode, spawnPosition, position);

                ActivatePoolable(pool, spawnedObjects[i], spawnPosition, spawner, rotation, i);

                float angle = (i * angleStep) + _angleOffset;
                DebugUtility.Log<CircularSpawnStrategy>(
                    $"Spawned object {i + 1}/{count} at {spawnPosition}, rotation: {rotation.eulerAngles} (angle: {angle}°).", 
                    "cyan");
            }

            return spawnedObjects;
        }

        private Vector3 CalculateCircularPosition(Vector3 centerPosition, int index, float angleStep)
        {
            float angle = (index * angleStep) + _angleOffset;

            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * _radius,
                0f, // Visão top-down (XZ)
                Mathf.Sin(angle * Mathf.Deg2Rad) * _radius
            );

            return centerPosition + offset;
        }
    }
}