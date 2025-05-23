using UnityEngine;
using _ImmersiveGames.Scripts.PoolSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [CreateAssetMenu(fileName = "SingleShotStrategy", menuName = "ImmersiveGames/ShootingStrategy/SingleShot")]
    public class SingleShotStrategy : ShootingStrategy
    {
        public override void Fire(bool isFiring, float deltaTime)
        {
            if (!isFiring || Time.time < nextFireTime) return;

            GameObject projectile = pool.GetProjectile(firePoint.position, firePoint.rotation, firePoint.forward, 50);
            if (projectile != null)
            {
                Debug.DrawRay(firePoint.position, firePoint.forward * 10f, Color.red, 0.5f);
            }
            nextFireTime = Time.time + fireRate;
        }
    }
}