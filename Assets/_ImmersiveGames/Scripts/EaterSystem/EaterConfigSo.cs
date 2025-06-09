using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [CreateAssetMenu(fileName = "EaterDesireConfig", menuName = "ImmersiveGames/EaterDesireConfig")]
    public class EaterConfigSo : ScriptableObject
    {
        [Header("Configurações de Desejos do Eater")]
        [SerializeField, Tooltip("Número máximo de recursos recentes a evitar repetição")]
        private int maxRecentDesires = 3;
        [SerializeField, Tooltip("Limiar para iniciar os desejos (0-1)")]
        private float desireThreshold = 0.9f;
        [SerializeField, Tooltip("Atraso para iniciar a escolha de desejos (segundos)")]
        private float delayTimer = 2;
        [SerializeField, Tooltip("Intervalo normal para mudança de vontade (segundos)")]
        private float desireChangeInterval = 10f;
        
        [Header("Configuração de Movimento")]
        [SerializeField, Tooltip("Intervalo para mudar de direção (segundos)")] 
        private float directionChangeInterval = 1f;
        [SerializeField, Tooltip("Mínimo de velocidade")]  private float minSpeed;
        [SerializeField, Tooltip("Máximo de velocidade")]  private float maxSpeed;
        [SerializeField, Tooltip("Multiplicador da velocidade para perseguição")]  private int multiplierChase = 2;
        [SerializeField, Tooltip("Velocidade de Rotação")]  private float rotationSpeed = 5f;
        
        [SerializeField, Tooltip("Fome restaurada ao consumir recurso desejado")]
        private float desiredHungerRestored = 50f;
        [SerializeField, Tooltip("Fome restaurada ao consumir recurso indesejado")]
        private float nonDesiredHungerRestored = 25f;
        [SerializeField, Tooltip("HP restaurado ao consumir recurso desejado")]
        private float desiredHealthRestored = 30f;

        public int MaxRecentDesires => maxRecentDesires;
        public float DesireThreshold => desireThreshold;
        public float DelayTimer => delayTimer;
        public float DesireChangeInterval => desireChangeInterval;
        public float DirectionChangeInterval => directionChangeInterval;
        public float MinSpeed => minSpeed;
        public float MaxSpeed => maxSpeed;
        public int MultiplierChase => multiplierChase;
        public float RotationSpeed => rotationSpeed;
        
        
        public float DesiredHungerRestored => desiredHungerRestored;
        public float NonDesiredHungerRestored => nonDesiredHungerRestored;
        public float DesiredHealthRestored => desiredHealthRestored;
    }
}