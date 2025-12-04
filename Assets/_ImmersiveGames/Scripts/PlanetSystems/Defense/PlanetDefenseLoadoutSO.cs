using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Configuração de defesa por planeta:
    /// - Novo fluxo: DefenseEntryConfigSo + DefenseMinionConfigSo por Role
    /// - LEGACY: PoolData + WaveProfile + Strategy para compatibilidade
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
        [Header("Config simplificada por Role")]
        [Tooltip("Configuração única de entrada de defesa por planeta (entrada + wave por Role).")]
        [SerializeField]
        private DefenseEntryConfigSo defenseEntryConfig;

        [Header("Preset principal")]
        [Tooltip("Primary defense preset for this planet; centralizes wave, target and strategy setup.")]
        [SerializeField]
        private PlanetDefensePresetSo defensePreset;

        [Header("Pool de defesas (por planeta)")]
        [Tooltip("PoolData usado para spawnar os minions defensivos deste planeta.")]
        [SerializeField, HideInInspector]
        private PoolData defensePoolData;

        [Header("Perfil de ondas (por planeta)")]
        [Tooltip("WaveProfile específico deste planeta. Se nulo, o controller pode cair no profile padrão configurado nele.")]
        [SerializeField, HideInInspector]
        private DefenseWaveProfileSo waveProfileOverride;

        [Header("Estratégia de defesa (por planeta)")]
        [Tooltip("Estratégia opcional que customiza comportamento de waves e minions para este planeta.")]
        [SerializeField, HideInInspector]
        private DefenseStrategySo defenseStrategy;

        /// <summary>
        /// Preset principal de defesa, preferido pelos serviços de orquestração.
        /// Mantém compatibilidade com SRP ao compor dados de ondas, estratégia e alvo.
        /// </summary>
        public PlanetDefensePresetSo DefensePreset => defensePreset;

        /// <summary>
        /// Configuração simplificada de defesa por Role (entrada + wave), preferida no fluxo novo.
        /// </summary>
        public DefenseEntryConfigSo DefenseEntryConfig => defenseEntryConfig;

        /// <summary>
        /// PoolData que o planeta quer usar para suas defesas.
        /// Pode ser nulo; nesse caso o sistema pode cair em um Default configurado no próprio planeta/serviço.
        /// </summary>
        public PoolData DefensePoolData => defensePoolData;

        /// <summary>
        /// WaveProfile específico deste planeta (pode ser nulo).
        /// </summary>
        public DefenseWaveProfileSo WaveProfileOverride => waveProfileOverride;

        /// <summary>
        /// Estratégia defensiva opcional deste planeta.
        /// </summary>
        public DefenseStrategySo DefenseStrategy => defenseStrategy;
    }
}