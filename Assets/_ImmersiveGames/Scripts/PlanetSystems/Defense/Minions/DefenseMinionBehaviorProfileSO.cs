using System;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Perfil de comportamento completo para minions de defesa.
    /// É o ponto central de composição das estratégias e parâmetros numéricos
    /// usados pelo <see cref="DefenseMinionController"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseMinionBehaviorProfile",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Minions/Behavior Profile",
        order = 200)]
    public class DefenseMinionBehaviorProfileSO : ScriptableObject
    {
        [Header("Identidade (opcional, para debug/organização)")]
        [SerializeField]
        private string variantId = "Default";

        [Header("Entrada / Órbita")]
        [Tooltip("Duração da animação de saída do centro do planeta até a órbita.")]
        [SerializeField, Min(0.1f)]
        private float entryDurationSeconds = 0.75f;

        [Tooltip("Fator de escala inicial ao surgir no centro do planeta (0..1).")]
        [SerializeField, Range(0.05f, 1f)]
        private float initialScaleFactor = 0.2f;

        [Tooltip("Tempo parado em órbita antes de iniciar a perseguição (Entry -> OrbitWait -> Chase).")]
        [SerializeField, Min(0f)]
        private float orbitIdleSeconds = 0.75f;

        [Tooltip("Estratégia de entrada (como sai do planeta e chega na órbita).")]
        [SerializeField]
        private MinionEntryStrategySo entryStrategy;

        [Header("Perseguição")]
        [Tooltip("Velocidade base da perseguição do minion.")]
        [SerializeField, Min(0.1f)]
        private float chaseSpeed = 3f;

        [Tooltip("Estratégia de perseguição (como o minion se move até o alvo).")]
        [SerializeField]
        private MinionChaseStrategySo chaseStrategy;

        [Header("Curvas opcionais para fallbacks internos")]
        [SerializeField]
        private AnimationCurve customScaleCurve;

        [SerializeField]
        private AnimationCurve customSpeedCurve;

        public string VariantId => string.IsNullOrWhiteSpace(variantId) ? name : variantId;
        public float EntryDuration => entryDurationSeconds;
        public float InitialScaleFactor => initialScaleFactor;
        public float OrbitIdleSeconds => orbitIdleSeconds;
        public float ChaseSpeed => chaseSpeed;
        public MinionEntryStrategySo EntryStrategy => entryStrategy;
        public MinionChaseStrategySo ChaseStrategy => chaseStrategy;
        public AnimationCurve CustomScaleCurve => customScaleCurve;
        public AnimationCurve CustomSpeedCurve => customSpeedCurve;
    }
}
