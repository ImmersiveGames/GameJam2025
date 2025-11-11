using _ImmersiveGames.Scripts.AudioSystem.Configs;
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
        [SerializeField, Tooltip("Atraso para iniciar a escolha de desejos (segundos)")]
        private float delayTimer = 2;

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
        [SerializeField, Tooltip("Distância base utilizada para orbitar planetas durante o estado de alimentação.")]
        private float orbitDistance = 3f;
        [SerializeField, Tooltip("Tempo em segundos para completar uma volta ao orbitar um planeta.")]
        private float orbitDuration = 4f;
        [SerializeField, Tooltip("Tempo em segundos para se aproximar da distância de órbita ao iniciar o estado de alimentação.")]
        private float orbitApproachDuration = 0.5f;

        [Tooltip("Distância mínima para considerar que o Eater chegou no planeta.")]
        public float minimumChaseDistance = 1.5f;

        public int MaxRecentDesires => maxRecentDesires;
        public float DelayTimer => delayTimer;
        public float DesireChangeInterval => desireChangeInterval;
        public float DesireDuration => desireChangeInterval;
        public float UnavailableDesireDurationMultiplier => unavailableDesireDurationMultiplier;
        public float AvailableDesireWeight => availableDesireWeight;
        public float PerPlanetAvailableWeight => perPlanetAvailableWeight;
        public float UnavailableDesireWeight => unavailableDesireWeight;
        public float RecentDesireWeightMultiplier => recentDesireWeightMultiplier;
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
        public float MinimumChaseDistance => Mathf.Max(0f, minimumChaseDistance);
        public float OrbitDistance => Mathf.Max(0.1f, orbitDistance);
        public float OrbitDuration => Mathf.Max(0.25f, orbitDuration);
        public float OrbitApproachDuration => Mathf.Min(Mathf.Max(0.1f, orbitApproachDuration), OrbitDuration);
    }
}
