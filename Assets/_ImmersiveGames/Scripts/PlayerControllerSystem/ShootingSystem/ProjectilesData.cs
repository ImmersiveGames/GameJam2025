using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    [CreateAssetMenu(fileName = "BulletData", menuName = "ImmersiveGames/PoolableObjectData/Bullets")]
    public class ProjectilesData : PoolableObjectData
    {
        [Header("ProjectileMovement Settings")]
        [SerializeField]
        public MovementType movementType;
        [SerializeField] public float moveSpeed = 10f; // Velocidade de movimento para frente
        [SerializeField] public int damage = 10;
        [Header("Curva de velocidade")]
        [SerializeField] public CurveMode curveMode = CurveMode.OneShot;
        [SerializeField] public float curveDuration = 2f;
        [SerializeField] public AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Movement Configurations")]
        [SerializeField] public float errorRadius = 1f; // Variação de erro no alvo
        [SerializeField] public float spiralRadius = 3f; // Raio da espiral
        [SerializeField] public int spiralRotationPerSecond = 3; //Numero de rotações por segundo na espiral
        [SerializeField] public float zigzagAmplitude = 2f; // Amplitude do ziguezague
        [SerializeField] public float zigzagFrequency = 2f; // Frequência do ziguezague
        [SerializeField] public float missileRotationSpeed = 90f;
    }
    public enum CurveMode
    {
        OneShot, // ex: 0 → 1 ao longo da duração
        Loop     // curva reinicia constantemente
    }
    
}