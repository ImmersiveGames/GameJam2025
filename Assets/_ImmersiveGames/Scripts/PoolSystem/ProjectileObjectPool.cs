using UnityEngine;
using _ImmersiveGames.Scripts.PlayerControllerSystem;

namespace _ImmersiveGames.Scripts.PoolSystem
{
    public class ProjectileObjectPool : ObjectPoolBase
    {
        private ProjectileData config;

        public void Initialize(GameObject projectilePrefab, ProjectileData projectileData)
        {
            config = projectileData;
            prefab = projectilePrefab;
            InitializePool();
        }

        protected override void ConfigureObject(GameObject obj)
        {
            Projectile projectile = obj.GetComponent<Projectile>();
            projectile.SetProjectileData(config);

            // Configurar o ProjectilePooledObject para saber a qual pool pertence
            ProjectilePooledObject pooledObj = obj.GetComponent<ProjectilePooledObject>();
            if (pooledObj != null)
            {
                pooledObj.SetPool(this);
            }
            else
            {
                Debug.LogWarning($"Projectile {obj.name} não tem ProjectilePooledObject.", this);
            }
        }

        public GameObject GetProjectile(Vector3 position, Quaternion rotation, Vector3 direction, int maxObjects)
        {
            GameObject projectileObj = GetObject(position, rotation, maxObjects);
            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                projectile.Initialize(direction);
            }
            return projectileObj;
        }
    }
}