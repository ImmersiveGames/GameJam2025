using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [CreateAssetMenu(fileName = "EaterDesireConfig", menuName = "ImmersiveGames/EaterDesireConfig")]
    public class EaterConfigSo : ScriptableObject
    {
        [Header("Configurações de Desejos do Eater")]
        [SerializeField, Tooltip("Número máximo de recursos recentes a evitar repetição")]
        private int maxRecentDesires = 3;
        [SerializeField, Tooltip("Tempo base (segundos) que cada desejo permanece ativo antes de ser trocado.")]
        private float desireChangeInterval = 10f;
        [SerializeField, Tooltip("Fator aplicado à duração do desejo quando nenhum planeta possui o recurso sorteado.")]
        private float unavailableDesireDurationMultiplier = 0.5f;
        [SerializeField, Tooltip("Peso base usado ao sortear desejos com recursos disponíveis em planetas ativos.")]
        private float availableDesireWeight = 3f;
        [SerializeField, Tooltip("Peso adicional aplicado por planeta ativo que possui o recurso desejado.")]
        private float perPlanetAvailableWeight = 1f;
        [SerializeField, Tooltip("Peso base usado ao sortear desejos sem planetas ativos correspondentes.")]
        private float unavailableDesireWeight = 0.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Multiplicador aplicado ao peso de desejos recentes quando existem novas opções.")]
        private float recentDesireWeightMultiplier = 0.35f;
        [SerializeField, Tooltip("Limiar para iniciar os desejos (0-1)")]
        private float desireThreshold = 0.9f;
        [SerializeField, Tooltip("Atraso para iniciar a escolha de desejos (segundos)")]
        private float delayTimer = 2;

        [Header("Áudio")]
        [SerializeField, Tooltip("Som reproduzido sempre que um novo desejo é sorteado.")]
        private SoundData desireSelectedSound;

        [Header("Saciedade")]
        [SerializeField, Tooltip("Tipo de recurso que indica quando o Eater está totalmente saciado (ex.: Stamina).")]
        private ResourceType satiationResourceType = ResourceType.Stamina;

        [Header("Configuração de Movimento")]
        [SerializeField, Tooltip("Intervalo para mudar de direção (segundos)")]
        private float directionChangeInterval = 1f;
        [SerializeField, Tooltip("Mínimo de velocidade")]  private float minSpeed;
        [SerializeField, Tooltip("Máximo de velocidade")]  private float maxSpeed;
        [SerializeField, Tooltip("Multiplicador da velocidade para perseguição")]  private int multiplierChase = 2;
        [SerializeField, Tooltip("Velocidade de Rotação")]  private float rotationSpeed = 5f;

        [Header("Comportamento de Fome")]
        [SerializeField, Tooltip("Tempo em segundos que o Eater permanece vagando antes de voltar a sentir fome.")]
        private float wanderingDuration = 15f;
        [SerializeField, Tooltip("Distância máxima em relação ao jogador mais próximo enquanto o Eater vaga satisfeito.")]
        private float wanderingMaxDistanceFromPlayer = 25f;
        [SerializeField, Tooltip("Influência usada para puxar a direção do movimento de volta para o jogador quando próximo do limite.")]
        private float wanderingReturnBias = 0.35f;
        [SerializeField, Tooltip("Influência usada para puxar o movimento em direção aos jogadores quando o Eater está com fome.")]
        private float hungryPlayerAttraction = 0.75f;
        
        [SerializeField, Tooltip("Fome restaurada ao consumir recurso desejado")]
        private float desiredHungerRestored = 50f;
        [SerializeField, Tooltip("Fome restaurada ao consumir recurso indesejado")]
        private float nonDesiredHungerRestored = 25f;
        [SerializeField, Tooltip("HP restaurado ao consumir recurso desejado")]
        private float desiredHealthRestored = 30f;
        [SerializeField, Tooltip("Dano causado ao morder um planeta")]
        private int biteDamage = 10;
        
        [Tooltip("Distância mínima para considerar que o Eater chegou no planeta.")]
        public float minimumChaseDistance = 1.5f;

        public int MaxRecentDesires => maxRecentDesires;
        public float DesireThreshold => desireThreshold;
        public float DelayTimer => delayTimer;
        public float DesireChangeInterval => desireChangeInterval;
        public float DesireDuration => desireChangeInterval;
        public float UnavailableDesireDurationMultiplier => unavailableDesireDurationMultiplier;
        public float AvailableDesireWeight => availableDesireWeight;
        public float PerPlanetAvailableWeight => perPlanetAvailableWeight;
        public float UnavailableDesireWeight => unavailableDesireWeight;
        public float RecentDesireWeightMultiplier => recentDesireWeightMultiplier;
        public SoundData DesireSelectedSound => desireSelectedSound;
        public float DirectionChangeInterval => directionChangeInterval;
        public float MinSpeed => minSpeed;
        public float MaxSpeed => maxSpeed;
        public int MultiplierChase => multiplierChase;
        public float RotationSpeed => rotationSpeed;
        public float WanderingDuration => wanderingDuration;
        public float WanderingMaxDistanceFromPlayer => wanderingMaxDistanceFromPlayer;
        public float WanderingReturnBias => wanderingReturnBias;
        public float HungryPlayerAttraction => hungryPlayerAttraction;
        public float MinimumChaseDistance => minimumChaseDistance;

        public ResourceType SatiationResourceType => satiationResourceType;
        
        public int BiteDamage => biteDamage;
        
        
        public float DesiredHungerRestored => desiredHungerRestored;
        public float NonDesiredHungerRestored => nonDesiredHungerRestored;
        public float DesiredHealthRestored => desiredHealthRestored;
    }
}