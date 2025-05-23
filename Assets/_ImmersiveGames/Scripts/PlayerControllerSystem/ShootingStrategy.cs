using UnityEngine;
using _ImmersiveGames.Scripts.PoolSystem;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [CreateAssetMenu(fileName = "ShootingStrategy", menuName = "ImmersiveGames/ShootingStrategy")]
    public abstract class ShootingStrategy : ScriptableObject, IShootingStrategy
    {
        [SerializeField, Tooltip("Dados do projétil usado por esta estratégia")]
        public ProjectileData projectileData;

        [SerializeField, Tooltip("Taxa de disparo (segundos entre disparos)")]
        protected float fireRate = 0.2f;

        protected Transform firePoint;
        protected Camera mainCamera;
        protected ProjectileObjectPool pool;
        protected float nextFireTime;

        public virtual void Initialize(Transform firePointRef, Camera mainCameraRef, ProjectileObjectPool poolRef)
        {
            firePoint = firePointRef;
            mainCamera = mainCameraRef;
            pool = poolRef;
            nextFireTime = 0f;
        }

        public abstract void Fire(bool isFiring, float deltaTime);
    }
}