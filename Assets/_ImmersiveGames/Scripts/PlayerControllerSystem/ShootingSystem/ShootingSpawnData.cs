using _ImmersiveGames.Scripts.SpawnSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    [CreateAssetMenu(fileName = "ShootingSpawnData", menuName = "Shooting/ShootingSpawnData")]
    public class ShootingSpawnData : SpawnData
    {
        [SerializeField] private float fireRate = 2f;
        [SerializeField] private float projectileSpeed = 10f;

        public float FireRate => fireRate;
        public float ProjectileSpeed => projectileSpeed;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fireRate <= 0)
            {
                Debug.LogError($"FireRate deve ser maior que 0 em {name}. Definindo como 1.", this);
                fireRate = 1f;
            }
            if (projectileSpeed <= 0)
            {
                Debug.LogError($"ProjectileSpeed deve ser maior que 0 em {name}. Definindo como 5.", this);
                projectileSpeed = 5f;
            }
        }
#endif
    }
}