using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Infrastructure.Pooling.Config;
using _ImmersiveGames.NewScripts.Core.Infrastructure.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Core.Infrastructure.Pooling.Runtime
{
    /// <summary>
    /// Canonical runtime pool for a single PoolDefinitionAsset.
    /// </summary>
    public sealed class GameObjectPool
    {
        private enum PoolReturnReason
        {
            Manual,
            Auto
        }

        private readonly Queue<PoolRuntimeInstance> _available = new();
        private readonly Dictionary<GameObject, PoolRuntimeInstance> _instancesByObject = new();
        private readonly HashSet<GameObject> _rentedObjects = new();
        private readonly PoolAutoReturnTracker _autoReturnTracker;

        public GameObjectPool(PoolDefinitionAsset definition, PoolRuntimeHost host)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            _autoReturnTracker = new PoolAutoReturnTracker(Host.Root, ReturnFromAutoTracker);
        }

        public PoolDefinitionAsset Definition { get; }
        public PoolRuntimeHost Host { get; }
        public int ActiveCount => _rentedObjects.Count;
        public int InactiveCount => _available.Count;
        public int TotalCount => _instancesByObject.Count;

        public void Prewarm()
        {
            Prewarm(Definition.InitialSize);
        }

        public void Prewarm(int targetSize)
        {
            if (targetSize <= 0)
            {
                return;
            }

            int before = TotalCount;
            while (TotalCount < targetSize)
            {
                if (!TryCreateAndQueue(isExpansion: false))
                {
                    break;
                }
            }

            int created = TotalCount - before;
            if (created > 0)
            {
                DebugUtility.LogVerbose(typeof(GameObjectPool),
                    $"[OBS][Pooling] Prewarm complete. asset='{Definition.name}' created={created} total={TotalCount} inactive={InactiveCount}.",
                    DebugUtility.Colors.Info);
            }
        }

        public GameObject Rent(Transform parent = null)
        {
            if (_available.Count == 0 && !TryCreateAndQueue(isExpansion: true))
            {
                throw new InvalidOperationException(
                    $"[Pooling] Pool limit reached for asset='{Definition.name}'. active={ActiveCount} total={TotalCount} max={Definition.MaxSize}.");
            }

            PoolRuntimeInstance runtimeInstance = _available.Dequeue();
            GameObject instance = runtimeInstance.Instance;

            runtimeInstance.MarkRented();
            _rentedObjects.Add(instance);
            Host.AttachAsRented(instance, parent);
            _autoReturnTracker.Track(Definition, runtimeInstance);

            CallPoolRent(instance);
            instance.SetActive(true);
            return instance;
        }

        public void Return(GameObject instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!_instancesByObject.TryGetValue(instance, out var runtimeInstance))
            {
                throw new InvalidOperationException(
                    $"[Pooling] Return rejected. Instance does not belong to this pool. asset='{Definition.name}'.");
            }

            if (!_rentedObjects.Contains(instance))
            {
                throw new InvalidOperationException(
                    $"[Pooling] Return rejected. Instance is not currently rented. asset='{Definition.name}' instance='{instance.name}'.");
            }

            ReturnInternal(runtimeInstance, PoolReturnReason.Manual);
        }

        public void Cleanup()
        {
            _autoReturnTracker.Clear("pool-cleanup");

            foreach (KeyValuePair<GameObject, PoolRuntimeInstance> kv in _instancesByObject)
            {
                GameObject instance = kv.Key;
                if (instance == null)
                {
                    continue;
                }

                CallPoolDestroyed(instance);
                UnityEngine.Object.Destroy(instance);
            }

            _available.Clear();
            _instancesByObject.Clear();
            _rentedObjects.Clear();
            _autoReturnTracker.Cleanup();
            Host.Cleanup();
        }

        private bool TryCreateAndQueue(bool isExpansion)
        {
            if (!CanCreate(isExpansion))
            {
                if (isExpansion)
                {
                    DebugUtility.LogWarning(typeof(GameObjectPool),
                        $"[OBS][Pooling] Expand blocked by max size. asset='{Definition.name}' total={TotalCount} max={Definition.MaxSize}.");
                }

                return false;
            }

            GameObject instance = UnityEngine.Object.Instantiate(Definition.Prefab, Host.AvailableRoot);
            instance.name = $"{Definition.Prefab.name}_Pooled_{TotalCount + 1}";
            instance.SetActive(false);

            var runtimeInstance = new PoolRuntimeInstance(Definition, instance, this);
            _instancesByObject.Add(instance, runtimeInstance);
            _available.Enqueue(runtimeInstance);
            CallPoolCreated(instance);

            if (isExpansion)
            {
                DebugUtility.LogVerbose(typeof(GameObjectPool),
                    $"[OBS][Pooling] Expand success. asset='{Definition.name}' total={TotalCount} max={Definition.MaxSize}.",
                    DebugUtility.Colors.Info);
            }

            return true;
        }

        private bool CanCreate(bool isExpansion)
        {
            if (TotalCount >= Definition.MaxSize)
            {
                return false;
            }

            if (TotalCount < Definition.InitialSize)
            {
                return true;
            }

            if (!isExpansion)
            {
                return true;
            }

            return Definition.CanExpand;
        }

        private void ReturnFromAutoTracker(PoolRuntimeInstance runtimeInstance)
        {
            if (runtimeInstance == null || runtimeInstance.Instance == null)
            {
                return;
            }

            ReturnInternal(runtimeInstance, PoolReturnReason.Auto);
        }

        private void ReturnInternal(PoolRuntimeInstance runtimeInstance, PoolReturnReason returnReason)
        {
            if (runtimeInstance == null)
            {
                throw new ArgumentNullException(nameof(runtimeInstance));
            }

            GameObject instance = runtimeInstance.Instance;
            if (instance == null)
            {
                if (returnReason == PoolReturnReason.Manual)
                {
                    throw new InvalidOperationException(
                        $"[Pooling] Return rejected. Instance was destroyed. asset='{Definition.name}'.");
                }

                DebugUtility.LogVerbose(typeof(GameObjectPool),
                    $"[OBS][Pooling] ReturnAuto skipped. asset='{Definition.name}' reason='instance-destroyed'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!_rentedObjects.Contains(instance))
            {
                if (returnReason == PoolReturnReason.Manual)
                {
                    throw new InvalidOperationException(
                        $"[Pooling] Return rejected. Instance is not currently rented. asset='{Definition.name}' instance='{instance.name}'.");
                }

                DebugUtility.LogVerbose(typeof(GameObjectPool),
                    $"[OBS][Pooling] ReturnAuto skipped. asset='{Definition.name}' go='{instance.name}' reason='already-returned'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (returnReason == PoolReturnReason.Manual)
            {
                _autoReturnTracker.Cancel(runtimeInstance, "manual-return");
            }

            runtimeInstance.MarkReturned();
            _rentedObjects.Remove(instance);

            CallPoolReturn(instance);
            instance.SetActive(false);
            Host.AttachAsAvailable(instance);
            _available.Enqueue(runtimeInstance);

            if (returnReason == PoolReturnReason.Auto)
            {
                DebugUtility.LogVerbose(typeof(GameObjectPool),
                    $"[OBS][Pooling] ReturnAuto complete. asset='{Definition.name}' active={ActiveCount} inactive={InactiveCount} total={TotalCount}.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.LogVerbose(typeof(GameObjectPool),
                    $"[OBS][Pooling] ReturnManual complete. asset='{Definition.name}' active={ActiveCount} inactive={InactiveCount} total={TotalCount}.",
                    DebugUtility.Colors.Info);
            }
        }

        private static void CallPoolCreated(GameObject instance)
        {
            foreach (var poolable in instance.GetComponents<IPoolableObject>())
            {
                poolable.OnPoolCreated();
            }
        }

        private static void CallPoolRent(GameObject instance)
        {
            foreach (var poolable in instance.GetComponents<IPoolableObject>())
            {
                poolable.OnPoolRent();
            }
        }

        private static void CallPoolReturn(GameObject instance)
        {
            foreach (var poolable in instance.GetComponents<IPoolableObject>())
            {
                poolable.OnPoolReturn();
            }
        }

        private static void CallPoolDestroyed(GameObject instance)
        {
            foreach (var poolable in instance.GetComponents<IPoolableObject>())
            {
                poolable.OnPoolDestroyed();
            }
        }
    }
}
