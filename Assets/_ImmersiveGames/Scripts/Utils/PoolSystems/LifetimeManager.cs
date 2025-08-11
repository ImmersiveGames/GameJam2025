using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class LifetimeManager : MonoBehaviour
    {
        public static LifetimeManager Instance { get; private set; }

        private readonly Dictionary<IPoolable, Coroutine> _activeObjects = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DebugUtility.LogError<LifetimeManager>("Another instance of LifetimeManager already exists. Destroying this duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterObject(IPoolable poolable, float lifetime)
        {
            if (poolable == null || lifetime <= 0) return;

            if (_activeObjects.ContainsKey(poolable))
            {
                DebugUtility.LogWarning<LifetimeManager>($"Object '{poolable.GetGameObject().name}' already registered.", this);
                return;
            }

            var coroutine = StartCoroutine(LifetimeCoroutine(poolable, lifetime));
            _activeObjects.Add(poolable, coroutine);
            DebugUtility.Log<LifetimeManager>($"Object '{poolable.GetGameObject().name}' registered with lifetime {lifetime}.", "cyan", this);
        }

        public void UnregisterObject(IPoolable poolable)
        {
            if (poolable == null || !_activeObjects.ContainsKey(poolable)) return;

            var coroutine = _activeObjects[poolable];
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            _activeObjects.Remove(poolable);
            DebugUtility.Log<LifetimeManager>($"Object '{poolable.GetGameObject().name}' unregistered from LifetimeManager.", "cyan", this);
        }

        private System.Collections.IEnumerator LifetimeCoroutine(IPoolable poolable, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);

            if (poolable != null && poolable.GetGameObject() != null)
            {
                poolable.Deactivate();
                poolable.ReturnToPool();
                DebugUtility.Log<LifetimeManager>($"Object '{poolable.GetGameObject().name}' expired and returned to pool. Active: {poolable.GetGameObject().activeSelf}", "blue", this);
            }
            _activeObjects.Remove(poolable);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}