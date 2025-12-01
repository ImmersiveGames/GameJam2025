using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Pacote completo de comportamento para um minion de defesa.
    /// Reúne o conjunto de ajustes que antes estavam espalhados em
    /// prefab + profile, incluindo as estratégias de entrada/perseguição.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseMinionBehaviorProfileV2",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Minions/Behavior Profile V2",
        order = 201)]
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

        [Header("Perseguição básica")]
        [Tooltip("Velocidade base da perseguição do minion.")]
        [SerializeField, Min(0.1f)]
        private float chaseSpeed = 3f;

        [Header("Estratégias")]
        [Tooltip("Define como o minion sai do planeta e chega na órbita.")]
        [SerializeField]
        private MinionEntryStrategySo entryStrategy;

        [Tooltip("Estratégia de perseguição do minion (zigzag, reto, etc.).")]
        [SerializeField]
        private MinionChaseStrategySo chaseStrategy;

        public string VariantId          => string.IsNullOrWhiteSpace(variantId) ? name : variantId;
        public float EntryDuration      => entryDurationSeconds;
        public float InitialScaleFactor => initialScaleFactor;
        public float OrbitIdleSeconds   => orbitIdleSeconds;
        public float ChaseSpeed         => chaseSpeed;
        public MinionEntryStrategySo EntryStrategy => entryStrategy;
        public MinionChaseStrategySo ChaseStrategy => chaseStrategy;
    }
}
