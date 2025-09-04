using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class ObjectPoolFactory
    {
        public IPoolable CreateObject(PoolableObjectData config, Transform parent, Vector3 position, string objectName, ObjectPool pool)
        {
            if (config == null || config.Prefab == null)
            {
                DebugUtility.LogError<ObjectPoolFactory>("Invalid PoolableObjectData or null Prefab.", null);
                return null;
            }

            GameObject instance = Object.Instantiate(config.Prefab, position, Quaternion.identity, parent);
            instance.name = objectName;
            var poolable = instance.GetComponent<IPoolable>();
            if (poolable == null)
            {
                DebugUtility.LogError<ObjectPoolFactory>($"Object '{objectName}' does not implement IPoolable.", null);
                Object.Destroy(instance);
                return null;
            }

            poolable.Configure(config, pool);
            DebugUtility.LogVerbose<ObjectPoolFactory>($"Created object '{objectName}' with config '{config.ObjectName}'.", "cyan", null);
            return poolable;
        }

        public void ReinitializeObject(IPoolable poolable, PoolableObjectData config)
        {
            if (poolable == null || config == null)
            {
                DebugUtility.LogWarning<ObjectPoolFactory>("Cannot reinitialize: poolable or config is null.", null);
                return;
            }

            poolable.Configure(config, poolable.GetPool());
            DebugUtility.LogVerbose<ObjectPoolFactory>($"Reinitialized object '{poolable.GetGameObject().name}' with config '{config.ObjectName}'.", "blue", null);
        }
    }
}