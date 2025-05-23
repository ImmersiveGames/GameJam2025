using UnityEngine;
using _ImmersiveGames.Scripts.ScriptableObjects;

namespace _ImmersiveGames.Scripts.EnemySystem
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "ImmersiveGames/EnemyData")]
    public class EnemyData : DestructibleObjectSo
    {
        [SerializeField, Tooltip("Dano causado ao colidir com o player")]
        public float collisionDamage = 10f;
        
        [SerializeField, Tooltip("Prefab do modelo visual do inimigo")]
        public GameObject modelPrefab;
    
        [SerializeField, Tooltip("Velocidade do inimigo (metros por segundo)")]
        public float speed = 10f;
    
        [SerializeField, Tooltip("Velocidade de rotação do inimigo (graus por segundo)")]
        public float rotationSpeed = 5f;  // Movido do Enemy
    
        [SerializeField, Tooltip("Tipo de movimento do inimigo")]
        public EnemyMovementType movementType = EnemyMovementType.Linear;
    
        [SerializeField, Tooltip("Frequência da oscilação do movimento sinusoidal")]
        public float sinusoidalFrequency = 0.5f;  // Novo parâmetro para movimento sinusoidal
    
        [SerializeField, Tooltip("Amplitude da oscilação do movimento sinusoidal")]
        public float sinusoidalAmplitude = 0.5f;  // Novo parâmetro para movimento sinusoidal
    
        [SerializeField, Tooltip("Multiplicador de rotação para movimento homing")]
        public float homingRotationMultiplier = 2f;  // Novo parâmetro para movimento homing
    }

    public enum EnemyMovementType
    {
        Linear, // Movimento reto em direção ao alvo
        Sinusoidal, // Movimento ondulante
        Homing // Movimento com correção de trajetória
    }
}