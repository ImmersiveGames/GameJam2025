using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;
using Object = UnityEngine.Object;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Runtime
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
            Object.DontDestroyOnLoad(root);

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
                    $"Ensure no-op (already registered). asset='{validatedDefinition.name}' active={existingPool.ActiveCount} inactive={existingPool.InactiveCount} total={existingPool.TotalCount}.",
                    DebugUtility.Colors.Info);
                return;
            }

            var host = new PoolRuntimeHost(
                hostName: $"Pool_{Sanitize(validatedDefinition.PoolLabel)}",
                globalRoot: _globalRoot);
            var pool = new GameObjectPool(validatedDefinition, host);
            _pools.Add(validatedDefinition, pool);

            bool prewarmRequested = validatedDefinition.Prewarm;
            if (prewarmRequested)
            {
                pool.Prewarm();
            }

            DebugUtility.LogVerbose(typeof(PoolService),
                $"Ensure registered asset='{validatedDefinition.name}' label='{Sanitize(validatedDefinition.PoolLabel)}' lifetimeScope='{validatedDefinition.LifetimeScope}' total={pool.TotalCount} inactive={pool.InactiveCount} prewarmRequested={prewarmRequested} autoReturnSeconds={validatedDefinition.AutoReturnSeconds:0.###}.",
                DebugUtility.Colors.Info);
        }

        public void Prewarm(PoolDefinitionAsset definition)
        {
            var pool = GetOrCreatePool(definition);
            pool.Prewarm();

            DebugUtility.LogVerbose(typeof(PoolService),
                $"Prewarm asset='{definition.name}' active={pool.ActiveCount} inactive={pool.InactiveCount} total={pool.TotalCount}.",
                DebugUtility.Colors.Info);
        }

        public GameObject Rent(PoolDefinitionAsset definition, Transform parent = null)
        {
            var pool = GetOrCreatePool(definition);
            try
            {
                var instance = pool.Rent(parent);

                DebugUtility.LogVerbose(typeof(PoolService),
                    $"Rent asset='{definition.name}' active={pool.ActiveCount} inactive={pool.InactiveCount} total={pool.TotalCount}.",
                    DebugUtility.Colors.Info);
                return instance;
            }
            catch (InvalidOperationException ex)
            {
                DebugUtility.LogError(typeof(PoolService),
                    $"Rent failed by limit. asset='{definition.name}' reason='{ex.Message}'.");
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
                $"Return asset='{definition.name}' active={pool.ActiveCount} inactive={pool.InactiveCount} total={pool.TotalCount}.",
                DebugUtility.Colors.Info);
        }


        public PoolScopeReleaseResult ReleasePoolsForScope(PoolLifetimeScope scope)
        {
            if (scope == PoolLifetimeScope.Global)
            {
                throw new InvalidOperationException("[FATAL][Pooling] ReleasePoolsForScope(Global) is not allowed. Global pools use Shutdown only.");
            }

            int releasedPoolCount = 0;
            int activeObjectCountBeforeRelease = 0;
            int inactiveObjectCountBeforeRelease = 0;
            List<PoolDefinitionAsset> definitionsToRelease = null;

            foreach (KeyValuePair<PoolDefinitionAsset, GameObjectPool> kv in _pools)
            {
                PoolDefinitionAsset definition = kv.Key;
                GameObjectPool pool = kv.Value;
                if (definition == null || pool == null || definition.LifetimeScope != scope)
                {
                    continue;
                }

                if (definitionsToRelease == null)
                {
                    definitionsToRelease = new List<PoolDefinitionAsset>();
                }

                definitionsToRelease.Add(definition);
                activeObjectCountBeforeRelease += pool.ActiveCount;
                inactiveObjectCountBeforeRelease += pool.InactiveCount;
            }

            if (definitionsToRelease == null || definitionsToRelease.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(PoolService),
                    $"event='PoolScopeReleaseSkipped' scope='{scope}' reason='no_registered_pools_for_scope'.",
                    DebugUtility.Colors.Info);

                return new PoolScopeReleaseResult(
                    scope,
                    0,
                    0,
                    0,
                    "no_registered_pools_for_scope");
            }

            int returnedObjectCountBeforeRelease = 0;
            foreach (PoolDefinitionAsset definition in definitionsToRelease)
            {
                GameObjectPool pool = _pools[definition];
                returnedObjectCountBeforeRelease += pool.ReturnAllRentedObjects("scope_release_before_pool_cleanup");
                pool.Cleanup();
                _pools.Remove(definition);
                releasedPoolCount += 1;
            }

            DebugUtility.Log(typeof(PoolService),
                $"event='PoolScopeReleased' scope='{scope}' releasedPoolCount='{releasedPoolCount}' activeObjectCountBeforeRelease='{activeObjectCountBeforeRelease}' inactiveObjectCountBeforeRelease='{inactiveObjectCountBeforeRelease}' returnedObjectCountBeforeRelease='{returnedObjectCountBeforeRelease}' reason='scope_release_completed'.",
                DebugUtility.Colors.Success);

            return new PoolScopeReleaseResult(
                scope,
                releasedPoolCount,
                activeObjectCountBeforeRelease,
                inactiveObjectCountBeforeRelease,
                "scope_release_completed");
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
                Object.Destroy(_globalRoot.gameObject);
            }

            DebugUtility.Log(typeof(PoolService),
                "Cleanup complete (PoolService shutdown).",
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

