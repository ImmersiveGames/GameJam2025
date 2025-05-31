using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    [CreateAssetMenu(fileName = "BulletData", menuName = "ImmersiveGames/PoolableObjectData/Bullets")]
    public class ProjectilesData : PoolableObjectData
    {
        [Header("Projectile Settings")]
        [SerializeField] public float speed;
        [SerializeField] public float damage;
    }
}