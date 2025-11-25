using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    
    public class LifetimeManager : PersistentSingleton<LifetimeManager>
    {
        private readonly Dictionary<IPoolable, float> _objectLifetimes = new();
        private readonly List<IPoolable> _objectsToRemove = new();

        private void Update()
        {
            _objectsToRemove.Clear();

            // Usar ToList() para criar snapshot e evitar exceções de modificação durante iteração
            var poolableEntries = new List<KeyValuePair<IPoolable, float>>(_objectLifetimes);

            foreach ((var poolable, float f) in poolableEntries)
            {

                // Se o poolable foi destruído (mas a referência ainda existe), remove da lista
                if (poolable == null || poolable.GetGameObject() == null)
                {
                    _objectsToRemove.Add(poolable);
                    continue;
                }

                float remainingTime = f - Time.deltaTime;
                if (remainingTime <= 0)
                {
                    _objectsToRemove.Add(poolable);
                }
                else
                {
                    _objectLifetimes[poolable] = remainingTime;
                }
            }

            // Remove objetos expirados ou nulos
            foreach (var poolable in _objectsToRemove)
            {
                if (poolable == null || poolable.GetGameObject() == null)
                {
                    // Para objetos nulos/destruídos, apenas remove do dicionário
                    _objectLifetimes.Remove(poolable);
                }
                else
                {
                    // Para objetos válidos expirados, retorna ao pool
                    ReturnToPool(poolable);
                }
            }
        }

        public void Register(IPoolable poolable, float lifetime)
        {
            if (poolable == null)
            {
                DebugUtility.LogError<LifetimeManager>("Cannot register null poolable object.", this);
                return;
            }

            if (!_objectLifetimes.TryAdd(poolable, lifetime))
            {
                DebugUtility.LogWarning<LifetimeManager>($"Object '{poolable.GetGameObject().name}' already registered in LifetimeManager.", this);
                return;
            }

            DebugUtility.LogVerbose<LifetimeManager>(
                $"Object '{poolable.GetGameObject().name}' registered with lifetime {lifetime}.",
                DebugUtility.Colors.Success,
                this);
        }

        public void Unregister(IPoolable poolable)
        {
            if (poolable == null)
            {
                DebugUtility.LogError<LifetimeManager>("Cannot unregister null poolable object.", this);
                return;
            }

            if (_objectLifetimes.Remove(poolable))
            {
                DebugUtility.LogVerbose<LifetimeManager>(
                    $"Object '{poolable.GetGameObject().name}' unregistered from LifetimeManager.",
                    DebugUtility.Colors.Success,
                    this);
            }
        }

        public void ReturnToPool(IPoolable poolable)
        {
            if (poolable == null)
            {
                DebugUtility.LogError<LifetimeManager>("Cannot return null poolable object to pool.", this);
                return;
            }

            if (!_objectLifetimes.Remove(poolable))
            {
                DebugUtility.LogWarning<LifetimeManager>($"Object '{poolable.GetGameObject().name}' not registered in LifetimeManager.", this);
                return;
            }

            poolable.Deactivate();
            var pooledObject = poolable.GetGameObject().GetComponent<PooledObject>();
            if (pooledObject != null && pooledObject.GetPool != null)
            {
                pooledObject.GetPool.ReturnObject(poolable);
                DebugUtility.LogVerbose<LifetimeManager>(
                    $"Object '{poolable.GetGameObject().name}' returned to pool.",
                    context: this);
            }
            else
            {
                DebugUtility.LogError<LifetimeManager>($"Object '{poolable.GetGameObject().name}' has no associated pool or PooledObject component.", this);
            }
        }
    }
}