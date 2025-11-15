using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [CreateAssetMenu(fileName = "EaterDesireConfig", menuName = "ImmersiveGames/EaterDesireConfig")]
    public class EaterConfigSo : ScriptableObject
    {
        [Header("Configurações de Desejos do Eater")]
        [SerializeField, Tooltip("Número máximo de recursos recentes a evitar repetição")]
        private int maxRecentDesires = 3;
        [FormerlySerializedAs("desireChangeInterval"), SerializeField,
         Tooltip("Tempo base (segundos) que cada desejo permanece ativo antes de ser trocado.")]
        private float desireDurationSeconds = 10f;
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
        [FormerlySerializedAs("delayTimer"), SerializeField,
         Tooltip("Atraso inicial (segundos) antes do primeiro desejo ser sorteado.")]
        private float initialDesireDelaySeconds = 2f;

        [Header("Áudio")]
        [SerializeField, Tooltip("Som reproduzido sempre que um novo desejo é sorteado.")]
        private SoundData desireSelectedSound;

        [Header("Configuração de Movimento")]
        [SerializeField, Tooltip("Intervalo para mudar de direção (segundos)")]
        private float directionChangeInterval = 1f;
        [SerializeField, Tooltip("Mínimo de velocidade")]  private float minSpeed;
        [SerializeField, Tooltip("Máximo de velocidade")]  private float maxSpeed;
        [SerializeField, Tooltip("Multiplicador da velocidade para perseguição")]  private int multiplierChase = 2;
        [SerializeField, Tooltip("Velocidade de Rotação")]  private float rotationSpeed = 5f;

        [Header("Comportamento de Fome")]
        [SerializeField, Tooltip("Distância mínima em relação ao jogador mais próximo enquanto o Eater vaga satisfeito.")]
        private float wanderingMinDistanceFromPlayer = 5f;
        [SerializeField, Tooltip("Distância máxima em relação ao jogador mais próximo enquanto o Eater vaga satisfeito.")]
        private float wanderingMaxDistanceFromPlayer = 25f;
        [SerializeField, Tooltip("Influência usada para puxar a direção do movimento de volta para o jogador quando próximo do limite.")]
        private float wanderingReturnBias = 0.35f;
        [SerializeField, Tooltip("Tempo (segundos) que o Eater permanece vagando antes de ficar com fome novamente.")]
        private float wanderingHungryDelay = 20f;
        [SerializeField, Tooltip("Influência usada para puxar o movimento em direção aos jogadores quando o Eater está com fome.")]
        private float hungryPlayerAttraction = 0.75f;

        [Header("Órbita")]
        [SerializeField, Tooltip("Tempo em segundos para completar uma volta ao orbitar um planeta.")]
        private float orbitDuration = 4f;
        [SerializeField, Tooltip("Tempo em segundos para se aproximar da distância de órbita ao iniciar o estado de alimentação.")]
        private float orbitApproachDuration = 0.5f;

        [FormerlySerializedAs("minimumChaseDistance"), FormerlySerializedAs("orbitDistance"), SerializeField,
         Tooltip("Distância mínima entre o eater e a superfície do planeta marcado. Também define o raio da órbita ao comer.")]
        private float minimumSurfaceDistance = 1.5f;

        [Header("Comportamento de Alimentação")]
        [FormerlySerializedAs("biteDamage"), SerializeField,
         Tooltip("Quantidade de dano aplicada em cada mordida enquanto o eater está no estado de alimentação.")]
        private float eatingDamageAmount = 10f;
        [SerializeField, Tooltip("Intervalo (segundos) entre cada aplicação de dano no planeta.")]
        private float eatingDamageInterval = 1f;
        [SerializeField, Tooltip("Recurso alvo que receberá o dano das mordidas do eater.")]
        private ResourceType eatingDamageResource = ResourceType.Health;
        [SerializeField, Tooltip("Tipo de dano utilizado ao consumir um planeta.")]
        private DamageType eatingDamageType = DamageType.Physical;
        [SerializeField, Tooltip("Som reproduzido sempre que o eater aplica uma mordida no planeta.")]
        private SoundData eatingBiteSound;

        [Header("Recuperação Durante Alimentação")]
        [SerializeField, Tooltip("Recurso próprio do eater que será recuperado enquanto ele devora um planeta.")]
        private ResourceType eatingRecoveryResource = ResourceType.Health;
        [SerializeField, Tooltip("Quantidade recuperada em cada tique enquanto o eater se alimenta.")]
        private float eatingRecoveryAmount = 5f;
        [SerializeField, Tooltip("Intervalo (segundos) entre cada recuperação de recurso durante a alimentação.")]
        private float eatingRecoveryInterval = 1f;

        public int MaxRecentDesires => Mathf.Max(0, maxRecentDesires);
        public float InitialDesireDelay => Mathf.Max(0f, initialDesireDelaySeconds);
        public float DesireDuration => Mathf.Max(desireDurationSeconds, 0.1f);
        public float UnavailableDesireDurationMultiplier => Mathf.Clamp(unavailableDesireDurationMultiplier, 0.05f, 1f);
        public float AvailableDesireWeight => Mathf.Max(0f, availableDesireWeight);
        public float PerPlanetAvailableWeight => Mathf.Max(0f, perPlanetAvailableWeight);
        public float UnavailableDesireWeight => Mathf.Max(0f, unavailableDesireWeight);
        public float RecentDesireWeightMultiplier => Mathf.Clamp01(recentDesireWeightMultiplier);
        public SoundData DesireSelectedSound => desireSelectedSound;
        public float DirectionChangeInterval => Mathf.Max(0.1f, directionChangeInterval);
        public float MinSpeed => Mathf.Max(0f, minSpeed);
        public float MaxSpeed => Mathf.Max(MinSpeed, maxSpeed);
        public int MultiplierChase => Mathf.Max(1, multiplierChase);
        public float RotationSpeed => Mathf.Max(0f, rotationSpeed);
        public float WanderingMinDistanceFromPlayer => Mathf.Max(0f, wanderingMinDistanceFromPlayer);
        public float WanderingMaxDistanceFromPlayer => Mathf.Max(WanderingMinDistanceFromPlayer, wanderingMaxDistanceFromPlayer);
        public float WanderingReturnBias => Mathf.Clamp01(wanderingReturnBias);
        public float WanderingHungryDelay => Mathf.Max(0f, wanderingHungryDelay);
        public float HungryPlayerAttraction => Mathf.Clamp01(hungryPlayerAttraction);
        public float MinimumChaseDistance => Mathf.Max(0f, minimumSurfaceDistance);
        public float OrbitDuration => Mathf.Max(0.25f, orbitDuration);
        public float OrbitApproachDuration => Mathf.Min(Mathf.Max(0.1f, orbitApproachDuration), OrbitDuration);
        public float EatingDamageAmount => Mathf.Max(0f, eatingDamageAmount);
        public float EatingDamageInterval => Mathf.Max(0.05f, eatingDamageInterval);
        public ResourceType EatingDamageResource => eatingDamageResource;
        public DamageType EatingDamageType => eatingDamageType;
        public SoundData EatingBiteSound => eatingBiteSound;
        public ResourceType EatingRecoveryResource => eatingRecoveryResource;
        public float EatingRecoveryAmount => Mathf.Max(0f, eatingRecoveryAmount);
        public float EatingRecoveryInterval => Mathf.Max(0.05f, eatingRecoveryInterval);
    }
}
