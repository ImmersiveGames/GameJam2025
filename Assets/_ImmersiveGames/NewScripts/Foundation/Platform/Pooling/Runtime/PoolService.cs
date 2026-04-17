using System;
using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Config;
using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Contracts;
using ImmersiveGames.GameJam2025.Core.Logging;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Infrastructure.Pooling.Runtime
{
    /// <summary>
    /// Canonical global pooling service runtime.
    /// </summary>
    public sealed class PoolService : IPoolService
    {
        private readonly Dictionary<PoolDefinitionAsset, GameObjectPool> _pools = new();
        private readonly Transform _globalRoot;

        public PoolService()
        {
            IsBootstrapped = true;
            var root = new GameObject("NewScripts_PoolingRuntime");
            _globalRoot = root.transform;
            UnityEngine.Object.DontDestroyOnLoad(root);

            DebugUtility.Log(typeof(PoolService),
                "[BOOT][Pooling] PoolService bootstrapped (Package B runtime core).",
                DebugUtility.Colors.Info);
        }

        public bool IsBootstrapped { get; }

        public void EnsureRegistered(PoolDefinitionAsset definition)
        {
            var validatedDefinition = ValidateDefinition(definition);

            if (_pools.TryGetValue(validatedDefinition, out var existingPool))
            {
                DebugUtility.LogVerbose(typeof(PoolService),
                    $"[OBS][Pooling] Ensure no-op (already registered). asset='{validatedDefinition.name}' active={existingPool.ActiveCount} inactive={existingPool.InactiveCount} total={existingPool.TotalCount}.",
                    DebugUtility.Colors.Info);
                return;
            }

            var host = new PoolRuntimeHost(
                hostName: $"Pool_{Sanitize(validatedDefinition.PoolLabel)}",
                globalRoot: _globalRoot);
            var pool = new GameObjectPool(validatedDefinition, host);
            _pools.Add(validatedDefinition, pool);

            DebugUtility.LogVerbose(typeof(PoolService),
                $"[OBS][Pooling] Ensure registered asset='{validatedDefinition.name}' label='{Sanitize(validatedDefinition.PoolLabel)}' total={pool.TotalCount} autoReturnSeconds={validatedDefinition.AutoReturnSeconds:0.###}.",
                DebugUtility.Colors.Info);
        }

        public void Prewarm(PoolDefinitionAsset definition)
        {
            var pool = GetOrCreatePool(definition);
            pool.Prewarm();

            DebugUtility.LogVerbose(typeof(PoolService),
                $"[OBS][Pooling] Prewarm asset='{definition.name}' active={pool.ActiveCount} inactive={pool.InactiveCount} total={pool.TotalCount}.",
                DebugUtility.Colors.Info);
        }

        public GameObject Rent(PoolDefinitionAsset definition, Transform parent = null)
        {
            var pool = GetOrCreatePool(definition);
            try
            {
                GameObject instance = pool.Rent(parent);

                DebugUtility.LogVerbose(typeof(PoolService),
                    $"[OBS][Pooling] Rent asset='{definition.name}' active={pool.ActiveCount} inactive={pool.InactiveCount} total={pool.TotalCount}.",
                    DebugUtility.Colors.Info);
                return instance;
            }
            catch (InvalidOperationException ex)
            {
                DebugUtility.LogError(typeof(PoolService),
                    $"[OBS][Pooling] Rent failed by limit. asset='{definition.name}' reason='{ex.Message}'.");
                throw;
            }
        }

        public void Return(PoolDefinitionAsset definition, GameObject instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance),
                    "Pooling Return requires a non-null GameObject instance.");
            }

            var pool = GetOrCreatePool(definition);
            pool.Return(instance);

            DebugUtility.LogVerbose(typeof(PoolService),
                $"[OBS][Pooling] Return asset='{definition.name}' active={pool.ActiveCount} inactive={pool.InactiveCount} total={pool.TotalCount}.",
                DebugUtility.Colors.Info);
        }

        public void Shutdown()
        {
            foreach (KeyValuePair<PoolDefinitionAsset, GameObjectPool> kv in _pools)
            {
                kv.Value.Cleanup();
            }

            _pools.Clear();
            if (_globalRoot != null)
            {
                UnityEngine.Object.Destroy(_globalRoot.gameObject);
            }

            DebugUtility.Log(typeof(PoolService),
                "[OBS][Pooling] Cleanup complete (PoolService shutdown).",
                DebugUtility.Colors.Info);
        }

        private GameObjectPool GetOrCreatePool(PoolDefinitionAsset definition)
        {
            var validated = ValidateDefinition(definition);
            EnsureRegistered(validated);
            return _pools[validated];
        }

        private static PoolDefinitionAsset ValidateDefinition(PoolDefinitionAsset definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition),
                    "Pooling identity must be a PoolDefinitionAsset reference (null is invalid).");
            }

            if (definition.Prefab == null)
            {
                throw new InvalidOperationException(
                    $"PoolDefinitionAsset requires prefab. asset='{definition.name}'.");
            }

            return definition;
        }

        private static string Sanitize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "n/a" : text.Trim();
        }
    }
}

