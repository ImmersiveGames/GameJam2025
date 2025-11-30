using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Descreve um conjunto de parâmetros de comportamento
    /// para um TIPO de minion de defesa.
    ///
    /// Nesta fase, é apenas um "container de dados", usado para:
    /// - Entrada (tempo / escala)
    /// - Tempo parado em órbita
    /// - Velocidade básica de perseguição
    ///
    /// No futuro podemos expandir com:
    /// - Estratégias de entrada/chase específicas
    /// - Configs por role (Player/Eater)
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseMinionBehaviorProfile",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Minions/Behavior Profile",
        order = 200)]
    public class DefenseMinionBehaviorProfile : ScriptableObject
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

        public string VariantId          => string.IsNullOrWhiteSpace(variantId) ? name : variantId;
        public float EntryDuration      => entryDurationSeconds;
        public float InitialScaleFactor => initialScaleFactor;
        public float OrbitIdleSeconds   => orbitIdleSeconds;
        public float ChaseSpeed         => chaseSpeed;
    }
}
