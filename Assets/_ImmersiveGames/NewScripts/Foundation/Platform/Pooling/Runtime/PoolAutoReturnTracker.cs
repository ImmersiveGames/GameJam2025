using System;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Runtime
{
    /// <summary>
    /// Tracks optional auto-return timers for rented pooled instances.
    /// </summary>
    public sealed class PoolAutoReturnTracker
    {
        private readonly Dictionary<PoolRuntimeInstance, Coroutine> _activeTimers = new();
        private readonly Action<PoolRuntimeInstance> _onAutoReturn;
        private readonly AutoReturnCoroutineHost _coroutineHost;

        public PoolAutoReturnTracker(Transform ownerRoot, Action<PoolRuntimeInstance> onAutoReturn)
        {
            if (ownerRoot == null)
            {
                throw new ArgumentNullException(nameof(ownerRoot));
            }

            _onAutoReturn = onAutoReturn ?? throw new ArgumentNullException(nameof(onAutoReturn));

            var trackerObject = new GameObject("AutoReturnTracker");
            trackerObject.transform.SetParent(ownerRoot, false);
            _coroutineHost = trackerObject.AddComponent<AutoReturnCoroutineHost>();
        }

        public bool IsOperational => _coroutineHost != null;

        public void Track(PoolDefinitionAsset definition, PoolRuntimeInstance runtimeInstance)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (runtimeInstance == null)
            {
                throw new ArgumentNullException(nameof(runtimeInstance));
            }

            Cancel(runtimeInstance, "reschedule");

            if (definition.AutoReturnSeconds <= 0f)
            {
                return;
            }

            var timer = _coroutineHost.StartCoroutine(AutoReturnAfterDelay(runtimeInstance, definition.AutoReturnSeconds));
            _activeTimers[runtimeInstance] = timer;

            DebugUtility.LogVerbose(typeof(PoolAutoReturnTracker),
                $"[OBS][Pooling] AutoReturn scheduled. asset='{definition.name}' go='{SafeName(runtimeInstance.Instance)}' seconds={definition.AutoReturnSeconds:0.###}.",
                DebugUtility.Colors.Info);
        }

        public void Cancel(PoolRuntimeInstance runtimeInstance, string reason)
        {
            if (runtimeInstance == null)
            {
                return;
            }

            if (!_activeTimers.TryGetValue(runtimeInstance, out var timer))
            {
                return;
            }

            if (timer != null)
            {
                _coroutineHost.StopCoroutine(timer);
            }

            _activeTimers.Remove(runtimeInstance);
            DebugUtility.LogVerbose(typeof(PoolAutoReturnTracker),
                $"[OBS][Pooling] AutoReturn canceled. asset='{runtimeInstance.Definition.name}' go='{SafeName(runtimeInstance.Instance)}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        public void Clear(string reason)
        {
            if (_activeTimers.Count == 0)
            {
                return;
            }

            foreach (var timer in _activeTimers.Values)
            {
                if (timer != null)
                {
                    _coroutineHost.StopCoroutine(timer);
                }
            }

            int canceledCount = _activeTimers.Count;
            _activeTimers.Clear();
            DebugUtility.LogVerbose(typeof(PoolAutoReturnTracker),
                $"[OBS][Pooling] AutoReturn cleared. canceled={canceledCount} reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        public void Cleanup()
        {
            Clear("tracker-cleanup");

            if (_coroutineHost != null)
            {
                UnityEngine.Object.Destroy(_coroutineHost.gameObject);
            }
        }

        private IEnumerator AutoReturnAfterDelay(PoolRuntimeInstance runtimeInstance, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            _activeTimers.Remove(runtimeInstance);

            if (runtimeInstance == null || runtimeInstance.Instance == null)
            {
                yield break;
            }

            if (!runtimeInstance.IsRented)
            {
                DebugUtility.LogVerbose(typeof(PoolAutoReturnTracker),
                    $"[OBS][Pooling] AutoReturn skip. asset='{runtimeInstance.Definition.name}' go='{SafeName(runtimeInstance.Instance)}' reason='instance-already-returned'.",
                    DebugUtility.Colors.Info);
                yield break;
            }

            DebugUtility.LogVerbose(typeof(PoolAutoReturnTracker),
                $"[OBS][Pooling] AutoReturn execute. asset='{runtimeInstance.Definition.name}' go='{SafeName(runtimeInstance.Instance)}'.",
                DebugUtility.Colors.Info);
            _onAutoReturn(runtimeInstance);
        }

        private static string SafeName(GameObject instance)
        {
            return instance != null ? instance.name : "null";
        }

        private sealed class AutoReturnCoroutineHost : MonoBehaviour
        {
        }
    }
}

