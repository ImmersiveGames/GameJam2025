using _ImmersiveGames.Scripts.Utils.PoolSystems;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    [CreateAssetMenu(fileName = "BulletData", menuName = "ImmersiveGames/PoolableObjectData/Bullets")]
    public class ProjectilesData : PoolableObjectData
    {
        [Header("Projectile Settings")]
        [SerializeField] public float moveSpeed = 10f; // Velocidade de movimento para frente
        [SerializeField] public float rotationSpeed = 10f; // Velocidade de rotação
        [SerializeField] public int damage = 10;
        [SerializeField] public MovementType movementType = MovementType.Curve;
        [SerializeField] public Ease moveEase = Ease.Linear; // Ease para movimento
        [SerializeField] public Ease rotationEase = Ease.OutQuad; // Ease para rotação
        [SerializeField] public float errorRadius = 1f; // Variação de erro no alvo
        [SerializeField] public bool faceTarget = true;
        
        
    }
    public enum MovementType
    {
        None,
        Curve,
        Spiral,
        ZigZag,
        Combined
    }
}