using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public interface IObjectPoolFactory
    {
        IPoolable CreateObject(PoolableObjectData config, Transform parent, Vector3 position, string objectName, ObjectPool pool, IActor spawner);
    }

    public sealed class ObjectPoolFactory : IObjectPoolFactory
    {
        public IPoolable CreateObject(PoolableObjectData config, Transform parent, Vector3 position, string objectName, ObjectPool pool, IActor spawner)
        {
            if (config == null || config.Prefab == null)
            {
                DebugUtility.LogError<ObjectPoolFactory>($"Invalid PoolableObjectData or null Prefab in '{config?.ObjectName}'.");
                return null;
            }

            if (config.Prefab.GetComponent<IPoolable>() == null)
            {
                DebugUtility.LogError<ObjectPoolFactory>($"Prefab '{config.Prefab.name}' does not implement IPoolable.");
                return null;
            }

            switch (config.ModelFactory)
            {
                case FactoryType.Bullet:
                    return CreateBullet(config, parent, position, objectName, pool, spawner);
                case FactoryType.Enemy:
                    return CreateEnemy(config, parent, position, objectName, pool, spawner);
                case FactoryType.Skin:
                    return CreateSkin(config, parent, position, objectName, pool, spawner);
                default:
                    return CreateDefault(config, parent, position, objectName, pool, spawner);
            }
        }

        private IPoolable CreateDefault(PoolableObjectData config, Transform parent, Vector3 position, string objectName, ObjectPool pool, IActor spawner)
        {
            var instance = Object.Instantiate(config.Prefab, position, Quaternion.identity, parent);
            instance.name = objectName;
            instance.SetActive(false);
            var poolable = instance.GetComponent<IPoolable>();
            poolable.Configure(config, pool,spawner);
            DebugUtility.LogVerbose<ObjectPoolFactory>($"Created default object '{objectName}' with config '{config.ObjectName}'.", "cyan");
            return poolable;
        }

        private IPoolable CreateBullet(PoolableObjectData config, Transform parent, Vector3 position, string objectName, ObjectPool pool, IActor spawner)
        {
            var instance = Object.Instantiate(config.Prefab, position, Quaternion.identity, parent);
            instance.name = objectName;
            instance.SetActive(false);
            var poolable = instance.GetComponent<IPoolable>();
            poolable.Configure(config, pool,spawner);

            if (config is BulletObjectData bulletData)
            {
                var rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    var direction = spawner != null ? spawner.Transform.forward : bulletData.InitialDirection;
                    rb.linearVelocity = direction.normalized * bulletData.Speed;
                }
                else
                {
                    DebugUtility.LogWarning<ObjectPoolFactory>($"Bullet '{objectName}' has no Rigidbody component.");
                }
            }

            DebugUtility.LogVerbose<ObjectPoolFactory>($"Created bullet '{objectName}' with config '{config.ObjectName}'.", "cyan");
            return poolable;
        }

        private IPoolable CreateEnemy(PoolableObjectData config, Transform parent, Vector3 position, string objectName, ObjectPool pool, IActor spawner)
        {
            var instance = Object.Instantiate(config.Prefab, position, Quaternion.identity, parent);
            instance.name = objectName;
            instance.SetActive(false);
            var poolable = instance.GetComponent<IPoolable>();
            poolable.Configure(config, pool,spawner);

            if (config is EnemyObjectData enemyData)
            {
                var animator = instance.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.Play(enemyData.InitialAIState);
                }
                else
                {
                    DebugUtility.LogWarning<ObjectPoolFactory>($"Enemy '{objectName}' has no Animator component.");
                }
            }

            DebugUtility.LogVerbose<ObjectPoolFactory>($"Created enemy '{objectName}' with config '{config.ObjectName}'.", "cyan");
            return poolable;
        }

        private IPoolable CreateSkin(PoolableObjectData config, Transform parent, Vector3 position, string objectName, ObjectPool pool, IActor spawner)
        {
            var instance = Object.Instantiate(config.Prefab, position, Quaternion.identity, parent);
            instance.name = objectName;
            instance.SetActive(false);
            var poolable = instance.GetComponent<IPoolable>();
            poolable.Configure(config, pool,spawner);

            if (spawner != null && spawner is IHasSkin hasSkin)
            {
                var renderer = instance.GetComponent<Renderer>();
                if (renderer != null && hasSkin.ModelRoot != null)
                {
                    renderer.material = hasSkin.ModelRoot.GetComponent<Renderer>()?.material;
                }
                else
                {
                    DebugUtility.LogWarning<ObjectPoolFactory>($"Skin '{objectName}' or spawner has no valid Renderer/ModelRoot.");
                }
            }

            DebugUtility.LogVerbose<ObjectPoolFactory>($"Created skin '{objectName}' with config '{config.ObjectName}'.", "cyan");
            return poolable;
        }
    }
}