using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Configs;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.Scripts.EaterSystem.Configs
{
    /// <summary>
    /// ScriptableObject central de configuração do comportamento do Eater.
    /// Agrupa parâmetros de desejos, movimento, perseguição e alimentação
    /// em um único ponto de ajuste para designers.
    /// </summary>
    [CreateAssetMenu(fileName = "EaterDesireConfig", menuName = "ImmersiveGames/EaterDesireConfig")]
    public class EaterConfigSo : ScriptableObject
    {
        [Header("Configurações de Desejos do Eater")]
        [SerializeField, Tooltip("Número máximo de recursos recentes a evitar repetição")]
        private int maxRecentDesires = 3;

        [FormerlySerializedAs("desireChangeInterval")]
        [FormerlySerializedAs("desireDuration")]
        [SerializeField, Tooltip("Tempo base (segundos) que cada desejo permanece ativo antes de ser trocado.")]
        private float desireDurationSeconds = 10f;

        [SerializeField, Tooltip("Variação percentual aleatória aplicada à duração do desejo.")]
        private float desireDurationRandomFactor = 0.25f;

        [SerializeField, Tooltip("Atraso inicial opcional antes de iniciar o primeiro desejo (em segundos).")]
        private float initialDesireDelay = 3f;

        [SerializeField, Tooltip("Tempo mínimo que o serviço permanece suspenso antes de poder retomar os desejos.")]
        private float suspendedMinDuration = 1f;

        [SerializeField, Tooltip("Tempo máximo que o serviço permanece suspenso antes de forçar retomada dos desejos.")]
        private float suspendedMaxDuration = 10f;

        [Header("Desejos - Pesos e Fallbacks")]
        [SerializeField, Tooltip("Peso base quando o recurso tem planetas disponíveis.")]
        private float availableDesireWeight = 1.0f;

        [SerializeField, Tooltip("Peso adicional por planeta disponível do recurso.")]
        private float perPlanetAvailableWeight = 0.25f;

        [SerializeField, Tooltip("Peso base quando o recurso não tem planetas disponíveis.")]
        private float unavailableDesireWeight = 0.5f;

        [SerializeField, Tooltip("Multiplicador de peso para desejos recentes (normalmente < 1 para penalizar).")]
        private float recentDesireWeightMultiplier = 0.35f;

        [SerializeField, Tooltip("Multiplicador de duração quando o desejo não está disponível em nenhum planeta.")]
        private float unavailableDesireDurationMultiplier = 0.5f;

        [SerializeField, Tooltip("Som reproduzido quando um novo desejo é selecionado.")]
        private SoundData desireSelectedSound;

        [Header("Movimentação Geral")]
        [SerializeField, Tooltip("Velocidade mínima de roaming.")]
        private float minSpeed = 2f;

        [SerializeField, Tooltip("Velocidade máxima de roaming.")]
        private float maxSpeed = 5f;

        [SerializeField, Tooltip("Multiplicador aplicado à velocidade máxima ao perseguir um planeta marcado.")]
        private float multiplierChase = 1.5f;

        [SerializeField, Tooltip("Velocidade de rotação usada para orientar o eater em direção ao alvo.")]
        private float rotationSpeed = 5f;

        [SerializeField, Tooltip("Intervalo em segundos entre mudanças de direção no roaming.")]
        private float directionChangeInterval = 2f;

        [Header("Distâncias em Relação ao Jogador")]
        [SerializeField, Tooltip("Distância mínima que o eater tenta manter em relação ao jogador.")]
        private float wanderingMinDistanceFromPlayer = 10f;

        [SerializeField, Tooltip("Distância máxima que o eater pode se afastar do jogador ao vagar.")]
        private float wanderingMaxDistanceFromPlayer = 40f;

        [SerializeField, Tooltip("Tendência do eater retornar para perto do jogador durante o vagar (0-1).")]
        private float wanderingReturnBias = 0.5f;

        [FormerlySerializedAs("minDistanceToPlayerWhenHungry")]
        [SerializeField, Tooltip("Distância mínima ao jogador que influencia o comportamento faminto.")]
        private float hungryMinDistanceFromPlayer = 8f;

        [FormerlySerializedAs("maxDistanceToPlayerWhenHungry")]
        [SerializeField, Tooltip("Distância máxima ao jogador para o estado faminto.")]
        private float hungryMaxDistanceFromPlayer = 35f;

        [Header("Transição de Fome")]
        [SerializeField, Tooltip("Tempo em segundos para o eater passar de vagando para faminto.")]
        private float wanderingHungryDelay = 30f;

        [SerializeField, Tooltip("Bias de atração ao jogador no estado faminto (0-1).")]
        private float hungryPlayerAttraction = 0.75f;

        [Header("Perseguição e Interação com Planetas")]
        [SerializeField, Tooltip("Distância mínima da superfície do planeta para iniciar estado de alimentação.")]
        private float minimumSurfaceDistance = 2f;

        [SerializeField, Tooltip("Duração de uma volta completa na órbita durante alimentação.")]
        private float orbitDuration = 4f;

        [SerializeField, Tooltip("Tempo de aproximação inicial até entrar na órbita de alimentação.")]
        private float orbitApproachDuration = 0.5f;

        [Header("Dano de Alimentação")]
        [SerializeField, Tooltip("Quantidade de dano aplicada por mordida durante alimentação.")]
        private float eatingDamageAmount = 10f;

        [SerializeField, Tooltip("Intervalo entre mordidas (segundos).")]
        private float eatingDamageInterval = 1f;

        [SerializeField, Tooltip("Recurso alvo do dano de alimentação (ex.: Health).")]
        private RuntimeAttributeType eatingDamageRuntimeAttribute = RuntimeAttributeType.Health;

        [SerializeField, Tooltip("Tipo de dano aplicado durante alimentação.")]
        private DamageType eatingDamageType = DamageType.Physical;

        [SerializeField, Tooltip("Som reproduzido em cada mordida durante alimentação.")]
        private SoundData eatingBiteSound;

        [Header("Recuperação do Eater Durante Alimentação")]
        [SerializeField, Tooltip("Recurso que o eater recupera enquanto se alimenta.")]
        private RuntimeAttributeType eatingRecoveryRuntimeAttribute = RuntimeAttributeType.Health;

        [SerializeField, Tooltip("Quantidade recuperada por ciclo de recuperação.")]
        private float eatingRecoveryAmount = 5f;

        [SerializeField, Tooltip("Intervalo entre ciclos de recuperação (segundos).")]
        private float eatingRecoveryInterval = 1f;

        [SerializeField, Tooltip("Cura adicional aplicada quando o planeta devorado é compatível com o desejo.")]
        private float eatingCompatibleDevourHealAmount = 25f;

        // ====== Propriedades atuais (mantidas) ======
        public int MaxRecentDesires => Mathf.Max(0, maxRecentDesires);

        public float DesireDurationSeconds => Mathf.Max(0.1f, desireDurationSeconds);

        public float DesireDurationRandomFactor => Mathf.Clamp01(desireDurationRandomFactor);

        public float InitialDesireDelay => Mathf.Max(0f, initialDesireDelay);

        public float SuspendedMinDuration => Mathf.Max(0f, suspendedMinDuration);

        public float SuspendedMaxDuration => Mathf.Max(SuspendedMinDuration, suspendedMaxDuration);

        public float MinSpeed => Mathf.Max(0f, minSpeed);

        public float MaxSpeed => Mathf.Max(MinSpeed, maxSpeed);

        public float MultiplierChase => Mathf.Max(1f, multiplierChase);

        public float RotationSpeed => Mathf.Max(0f, rotationSpeed);

        public float DirectionChangeInterval => Mathf.Max(0.1f, directionChangeInterval);

        public float WanderingMinDistanceFromPlayer => Mathf.Max(0f, wanderingMinDistanceFromPlayer);

        public float WanderingMaxDistanceFromPlayer => Mathf.Max(WanderingMinDistanceFromPlayer, wanderingMaxDistanceFromPlayer);

        public float WanderingReturnBias => Mathf.Clamp01(wanderingReturnBias);

        public float HungryMinDistanceFromPlayer => Mathf.Max(0f, hungryMinDistanceFromPlayer);

        public float HungryMaxDistanceFromPlayer => Mathf.Max(HungryMinDistanceFromPlayer, hungryMaxDistanceFromPlayer);

        public float WanderingHungryDelay => Mathf.Max(0f, wanderingHungryDelay);

        public float HungryPlayerAttraction => Mathf.Clamp01(hungryPlayerAttraction);

        public float MinimumChaseDistance => Mathf.Max(0f, minimumSurfaceDistance);

        public float OrbitDuration => Mathf.Max(0.25f, orbitDuration);

        public float OrbitApproachDuration => Mathf.Min(Mathf.Max(0.1f, orbitApproachDuration), OrbitDuration);

        public float EatingDamageAmount => Mathf.Max(0f, eatingDamageAmount);

        public float EatingDamageInterval => Mathf.Max(0.05f, eatingDamageInterval);

        public RuntimeAttributeType EatingDamageRuntimeAttribute => eatingDamageRuntimeAttribute;

        public DamageType EatingDamageType => eatingDamageType;

        public SoundData EatingBiteSound => eatingBiteSound;

        public RuntimeAttributeType EatingRecoveryRuntimeAttribute => eatingRecoveryRuntimeAttribute;

        public float EatingRecoveryAmount => Mathf.Max(0f, eatingRecoveryAmount);

        public float EatingRecoveryInterval => Mathf.Max(0.05f, eatingRecoveryInterval);

        public float EatingCompatibleDevourHealAmount => Mathf.Max(0f, eatingCompatibleDevourHealAmount);

        // ====== Propriedades esperadas pelo EaterDesireService (aliases) ======
        // O service usa _config.DesireDuration, então mantemos este nome como alias.
        public float DesireDuration => DesireDurationSeconds;

        public float UnavailableDesireDurationMultiplier => Mathf.Max(0.05f, unavailableDesireDurationMultiplier);

        public float AvailableDesireWeight => Mathf.Max(0f, availableDesireWeight);

        public float PerPlanetAvailableWeight => Mathf.Max(0f, perPlanetAvailableWeight);

        public float UnavailableDesireWeight => Mathf.Max(0f, unavailableDesireWeight);

        public float RecentDesireWeightMultiplier => Mathf.Max(0f, recentDesireWeightMultiplier);

        public SoundData DesireSelectedSound => desireSelectedSound;
    }
}
