using UnityEngine;
using _ImmersiveGames.Scripts.PoolSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [CreateAssetMenu(fileName = "BurstShotStrategy", menuName = "ImmersiveGames/ShootingStrategy/BurstShot")]
    public class BurstShotStrategy : ShootingStrategy
    {
        [SerializeField, Tooltip("Número de projéteis por rajada")]
        private int burstCount = 3;

        [SerializeField, Tooltip("Intervalo entre projéteis na rajada (segundos)")]
        private float burstInterval = 0.1f;

        private float burstTimer;
        private int shotsFired;

        public override void Initialize(Transform firePoint, Camera mainCamera, ProjectileObjectPool pool)
        {
            base.Initialize(firePoint, mainCamera, pool);
            burstTimer = 0f;
            shotsFired = 0;
        }

        public override void Fire(bool isFiring, float deltaTime)
        {
            if (!isFiring) return;

            burstTimer += deltaTime;
            if (shotsFired < burstCount && burstTimer >= burstInterval)
            {
                GameObject projectile = pool.GetProjectile(firePoint.position, firePoint.rotation, firePoint.forward, 50);
                if (projectile != null)
                {
                    Debug.DrawRay(firePoint.position, firePoint.forward * 10f, Color.red, 0.5f);
                }
                burstTimer = 0f;
                shotsFired++;
            }
            else if (shotsFired >= burstCount && Time.time >= nextFireTime)
            {
                shotsFired = 0;
                nextFireTime = Time.time + fireRate;
            }
        }
    }
}