using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Configuração de defesa por planeta:
    /// - PoolData usado para as defesas
    /// - Perfil de onda (wave profile)
    /// - Estratégia defensiva (próxima etapa)
    ///
    /// A ideia é que cada planeta possua um pacote completo de defesa
    /// exclusivamente via dados, sem depender de variáveis de prefab ou
    /// configurações globais compartilhadas. Esta é a "fonte única" por
    /// planeta, evitando campos duplicados em controllers.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefenseLoadout",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Loadout")]
    public sealed class PlanetDefenseLoadoutSo : ScriptableObject
    {
        [Header("Preset principal")]
        [Tooltip("Primary defense preset for this planet; centralizes wave, target and strategy setup.")]
        [SerializeField, FormerlySerializedAs("defensePreset")]
        private PlanetDefensePresetSo primaryDefensePreset;

        [Header("Pool de defesas (por planeta)")]
        [Tooltip("PoolData usado para spawnar os minions defensivos deste planeta.")]
        [SerializeField, HideInInspector, FormerlySerializedAs("defensePoolData")]
        private PoolData defensePoolDataPerPlanet;

        [Header("Perfil de ondas (por planeta)")]
        [Tooltip("WaveProfile específico deste planeta. Se nulo, o controller pode cair no profile padrão configurado nele.")]
        [SerializeField, HideInInspector, FormerlySerializedAs("waveProfileOverride")]
        private DefenseWaveProfileSo planetWaveProfileOverride;

        [Header("Estratégia de defesa (por planeta)")]
        [Tooltip("Estratégia opcional que customiza comportamento de waves e minions para este planeta.")]
        [SerializeField, HideInInspector, FormerlySerializedAs("defenseStrategy")]
        private DefenseStrategySo planetDefenseStrategy;

        /// <summary>
        /// Preset principal de defesa, preferido pelos serviços de orquestração.
        /// Mantém compatibilidade com SRP ao compor dados de ondas, estratégia e alvo.
        /// </summary>
        public PlanetDefensePresetSo DefensePreset => primaryDefensePreset;

        /// <summary>
        /// PoolData que o planeta quer usar para suas defesas.
        /// Pode ser nulo; nesse caso o sistema pode cair em um Default configurado no próprio planeta/serviço.
        /// </summary>
        public PoolData DefensePoolData => defensePoolDataPerPlanet;

        /// <summary>
        /// WaveProfile específico deste planeta (pode ser nulo).
        /// </summary>
        public DefenseWaveProfileSo WaveProfileOverride => planetWaveProfileOverride;

        /// <summary>
        /// Estratégia defensiva opcional deste planeta.
        /// </summary>
        public DefenseStrategySo DefenseStrategy => planetDefenseStrategy;

        private void OnValidate()
        {
            if (primaryDefensePreset == null)
            {
                Debug.LogError(
                    $"[{nameof(PlanetDefenseLoadoutSo)}] {name} requer um {nameof(PlanetDefensePresetSo)} atribuído para orquestrar a defesa do planeta. Configure-o para evitar fallbacks silenciosos.",
                    this);
            }
        }
    }
}