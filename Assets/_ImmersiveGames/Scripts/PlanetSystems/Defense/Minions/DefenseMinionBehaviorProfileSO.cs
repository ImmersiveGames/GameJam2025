using _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions.Strategy;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions
{
    /// <summary>
    /// Pacote completo de comportamento para um minion de defesa.
    /// ReÃºne o conjunto de ajustes que antes estavam espalhados em
    /// prefab + profile, incluindo as estratÃ©gias de entrada/perseguiÃ§Ã£o.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseMinionBehaviorProfileV2",
        menuName = "ImmersiveGames/Legacy/PlanetSystems/Defense/Minions/Behavior Profile V2",
        order = 201)]
    public class DefenseMinionBehaviorProfileSo : ScriptableObject
    {
        [Header("Identidade (opcional, para debug/organizaÃ§Ã£o)")]
        [SerializeField]
        private string variantId = "Default";

        [Header("Entrada / Ã“rbita")]
        [Tooltip("DuraÃ§Ã£o da animaÃ§Ã£o de saÃ­da do centro do planeta atÃ© a Ã³rbita.")]
        [SerializeField, Min(0.1f)]
        private float entryDurationSeconds = 0.75f;

        [Tooltip("Fator de escala inicial ao surgir no centro do planeta (0..1).")]
        [SerializeField, Range(0.05f, 1f)]
        private float initialScaleFactor = 0.2f;

        [Tooltip("Tempo parado em Ã³rbita antes de iniciar a perseguiÃ§Ã£o (Entry -> OrbitWait -> Chase).")]
        [SerializeField, Min(0f)]
        private float orbitIdleSeconds = 0.75f;

        [Header("PerseguiÃ§Ã£o bÃ¡sica")]
        [Tooltip("Velocidade base da perseguiÃ§Ã£o do minion.")]
        [SerializeField, Min(0.1f)]
        private float chaseSpeed = 3f;

        [Header("RotaÃ§Ã£o na perseguiÃ§Ã£o")]
        [Tooltip("Se verdadeiro, quando a perseguiÃ§Ã£o comeÃ§a o minion jÃ¡ alinha o forward diretamente para o alvo.")]
        [SerializeField]
        private bool snapFacingOnChaseStart = true;

        [Tooltip("Fator de interpolaÃ§Ã£o da rotaÃ§Ã£o durante a perseguiÃ§Ã£o (0 = nÃ£o gira, 1 = vira instantaneamente).")]
        [SerializeField, Range(0f, 1f)]
        private float chaseRotationLerpFactor = 0.2f;

        [Header("EstratÃ©gias")]
        [Tooltip("Define como o minion sai do planeta e chega na Ã³rbita.")]
        [SerializeField]
        private MinionEntryStrategySo entryStrategy;

        [Tooltip("EstratÃ©gia de perseguiÃ§Ã£o do minion (zigzag, reto, etc.).")]
        [SerializeField]
        private MinionChaseStrategySo chaseStrategy;

        public string VariantId => string.IsNullOrWhiteSpace(variantId) ? name : variantId;
        public float EntryDuration => entryDurationSeconds;
        public float InitialScaleFactor => initialScaleFactor;
        public float OrbitIdleSeconds => orbitIdleSeconds;
        public float ChaseSpeed => chaseSpeed;
        public bool SnapFacingOnChaseStart => snapFacingOnChaseStart;
        public float ChaseRotationLerpFactor => chaseRotationLerpFactor;
        public MinionEntryStrategySo EntryStrategy => entryStrategy;
        public MinionChaseStrategySo ChaseStrategy => chaseStrategy;
    }
}
