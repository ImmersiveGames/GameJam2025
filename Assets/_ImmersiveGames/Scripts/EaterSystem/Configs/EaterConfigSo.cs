using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using UnityEngine;
using UnityEngine.Serialization;
namespace _ImmersiveGames.Scripts.EaterSystem.Configs
{
    /// <summary>
    /// ScriptableObject central de configuraÃ§Ã£o do comportamento do Eater.
    /// Agrupa parÃ¢metros de desejos, movimento, perseguiÃ§Ã£o e alimentaÃ§Ã£o
    /// em um Ãºnico ponto de ajuste para designers.
    /// </summary>
    [CreateAssetMenu(fileName = "EaterDesireConfig", menuName = "ImmersiveGames/Legacy/Eater/Configs/EaterDesireConfig")]
    public class EaterConfigSo : ScriptableObject
    {
        [Header("ConfiguraÃ§Ãµes de Desejos do Eater")]
        [SerializeField, Tooltip("NÃºmero mÃ¡ximo de recursos recentes a evitar repetiÃ§Ã£o")]
        private int maxRecentDesires = 3;

        [FormerlySerializedAs("desireChangeInterval")]
        [FormerlySerializedAs("desireDuration")]
        [SerializeField, Tooltip("Tempo base (segundos) que cada desejo permanece ativo antes de ser trocado.")]
        private float desireDurationSeconds = 10f;

        [SerializeField, Tooltip("VariaÃ§Ã£o percentual aleatÃ³ria aplicada Ã  duraÃ§Ã£o do desejo.")]
        private float desireDurationRandomFactor = 0.25f;

        [SerializeField, Tooltip("Atraso inicial opcional antes de iniciar o primeiro desejo (em segundos).")]
        private float initialDesireDelay = 3f;

        [SerializeField, Tooltip("Tempo mÃ­nimo que o serviÃ§o permanece suspenso antes de poder retomar os desejos.")]
        private float suspendedMinDuration = 1f;

        [SerializeField, Tooltip("Tempo mÃ¡ximo que o serviÃ§o permanece suspenso antes de forÃ§ar retomada dos desejos.")]
        private float suspendedMaxDuration = 10f;

        [Header("Desejos - Pesos e Fallbacks")]
        [SerializeField, Tooltip("Peso base quando o recurso tem planetas disponÃ­veis.")]
        private float availableDesireWeight = 1.0f;

        [SerializeField, Tooltip("Peso adicional por planeta disponÃ­vel do recurso.")]
        private float perPlanetAvailableWeight = 0.25f;

        [SerializeField, Tooltip("Peso base quando o recurso nÃ£o tem planetas disponÃ­veis.")]
        private float unavailableDesireWeight = 0.5f;

        [SerializeField, Tooltip("Multiplicador de peso para desejos recentes (normalmente < 1 para penalizar).")]
        private float recentDesireWeightMultiplier = 0.35f;

        [SerializeField, Tooltip("Multiplicador de duraÃ§Ã£o quando o desejo nÃ£o estÃ¡ disponÃ­vel em nenhum planeta.")]
        private float unavailableDesireDurationMultiplier = 0.5f;

        [SerializeField, Tooltip("Som reproduzido quando um novo desejo Ã© selecionado.")]
        private SoundData desireSelectedSound;

        [Header("MovimentaÃ§Ã£o Geral")]
        [SerializeField, Tooltip("Velocidade mÃ­nima de roaming.")]
        private float minSpeed = 2f;

        [SerializeField, Tooltip("Velocidade mÃ¡xima de roaming.")]
        private float maxSpeed = 5f;

        [SerializeField, Tooltip("Multiplicador aplicado Ã  velocidade mÃ¡xima ao perseguir um planeta marcado.")]
        private float multiplierChase = 1.5f;

        [SerializeField, Tooltip("Velocidade de rotaÃ§Ã£o usada para orientar o eater em direÃ§Ã£o ao alvo.")]
        private float rotationSpeed = 5f;

        [SerializeField, Tooltip("Intervalo em segundos entre mudanÃ§as de direÃ§Ã£o no roaming.")]
        private float directionChangeInterval = 2f;

        [Header("DistÃ¢ncias em RelaÃ§Ã£o ao Jogador")]
        [SerializeField, Tooltip("DistÃ¢ncia mÃ­nima que o eater tenta manter em relaÃ§Ã£o ao jogador.")]
        private float wanderingMinDistanceFromPlayer = 10f;

        [SerializeField, Tooltip("DistÃ¢ncia mÃ¡xima que o eater pode se afastar do jogador ao vagar.")]
        private float wanderingMaxDistanceFromPlayer = 40f;

        [SerializeField, Tooltip("TendÃªncia do eater retornar para perto do jogador durante o vagar (0-1).")]
        private float wanderingReturnBias = 0.5f;

        [FormerlySerializedAs("minDistanceToPlayerWhenHungry")]
        [SerializeField, Tooltip("DistÃ¢ncia mÃ­nima ao jogador que influencia o comportamento faminto.")]
        private float hungryMinDistanceFromPlayer = 8f;

        [FormerlySerializedAs("maxDistanceToPlayerWhenHungry")]
        [SerializeField, Tooltip("DistÃ¢ncia mÃ¡xima ao jogador para o estado faminto.")]
        private float hungryMaxDistanceFromPlayer = 35f;

        [Header("TransiÃ§Ã£o de Fome")]
        [SerializeField, Tooltip("Tempo em segundos para o eater passar de vagando para faminto.")]
        private float wanderingHungryDelay = 30f;

        [SerializeField, Tooltip("Bias de atraÃ§Ã£o ao jogador no estado faminto (0-1).")]
        private float hungryPlayerAttraction = 0.75f;

        [Header("PerseguiÃ§Ã£o e InteraÃ§Ã£o com Planetas")]
        [SerializeField, Tooltip("DistÃ¢ncia mÃ­nima da superfÃ­cie do planeta para iniciar estado de alimentaÃ§Ã£o.")]
        private float minimumSurfaceDistance = 2f;

        [SerializeField, Tooltip("DuraÃ§Ã£o de uma volta completa na Ã³rbita durante alimentaÃ§Ã£o.")]
        private float orbitDuration = 4f;

        [SerializeField, Tooltip("Tempo de aproximaÃ§Ã£o inicial atÃ© entrar na Ã³rbita de alimentaÃ§Ã£o.")]
        private float orbitApproachDuration = 0.5f;

        [Header("Dano de AlimentaÃ§Ã£o")]
        [SerializeField, Tooltip("Quantidade de dano aplicada por mordida durante alimentaÃ§Ã£o.")]
        private float eatingDamageAmount = 10f;

        [SerializeField, Tooltip("Intervalo entre mordidas (segundos).")]
        private float eatingDamageInterval = 1f;

        [SerializeField, Tooltip("Recurso alvo do dano de alimentaÃ§Ã£o (ex.: Health).")]
        private RuntimeAttributeType eatingDamageRuntimeAttribute = RuntimeAttributeType.Health;

        [SerializeField, Tooltip("Tipo de dano aplicado durante alimentaÃ§Ã£o.")]
        private DamageType eatingDamageType = DamageType.Physical;

        [SerializeField, Tooltip("Som reproduzido em cada mordida durante alimentaÃ§Ã£o.")]
        private SoundData eatingBiteSound;

        [Header("RecuperaÃ§Ã£o do Eater Durante AlimentaÃ§Ã£o")]
        [SerializeField, Tooltip("Recurso que o eater recupera enquanto se alimenta.")]
        private RuntimeAttributeType eatingRecoveryRuntimeAttribute = RuntimeAttributeType.Health;

        [SerializeField, Tooltip("Quantidade recuperada por ciclo de recuperaÃ§Ã£o.")]
        private float eatingRecoveryAmount = 5f;

        [SerializeField, Tooltip("Intervalo entre ciclos de recuperaÃ§Ã£o (segundos).")]
        private float eatingRecoveryInterval = 1f;

        [SerializeField, Tooltip("Cura adicional aplicada quando o planeta devorado Ã© compatÃ­vel com o desejo.")]
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
        // O service usa _config.DesireDuration, entÃ£o mantemos este nome como alias.
        public float DesireDuration => DesireDurationSeconds;

        public float UnavailableDesireDurationMultiplier => Mathf.Max(0.05f, unavailableDesireDurationMultiplier);

        public float AvailableDesireWeight => Mathf.Max(0f, availableDesireWeight);

        public float PerPlanetAvailableWeight => Mathf.Max(0f, perPlanetAvailableWeight);

        public float UnavailableDesireWeight => Mathf.Max(0f, unavailableDesireWeight);

        public float RecentDesireWeightMultiplier => Mathf.Max(0f, recentDesireWeightMultiplier);

        public SoundData DesireSelectedSound => desireSelectedSound;
    }
}
