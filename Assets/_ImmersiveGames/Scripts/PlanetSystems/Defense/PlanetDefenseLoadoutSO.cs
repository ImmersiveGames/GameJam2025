using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Configuração de defesa por planeta.
    ///
    /// Agora prioriza o uso de um <see cref="PlanetDefensePresetSo"/> simples,
    /// mantendo campos antigos para compatibilidade.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefenseLoadout",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Loadout")]
    public sealed class PlanetDefenseLoadoutSo : ScriptableObject
    {
        [Header("Preset simplificado (recomendado)")]
        [Tooltip("Config única para waves, alvo e minion. Permite override avançado opcional.")]
        [SerializeField]
        private PlanetDefensePresetSo defensePreset;

        [Header("Pool de defesas (por planeta)")]
        [Tooltip("PoolData usado para spawnar os minions defensivos deste planeta.")]
        [SerializeField]
        private PoolData defensePoolData;

        [Header("Perfil de ondas (por planeta)")]
        [Tooltip("WaveProfile específico deste planeta. Se nulo, o controller pode cair no profile padrão configurado nele.")]
        [SerializeField]
        private DefenseWaveProfileSo waveProfileOverride;

        [Header("Estratégia de defesa (por planeta)")]
        [Tooltip("Estratégia opcional que customiza comportamento de waves e minions para este planeta.")]
        [SerializeField]
        private DefenseStrategySo defenseStrategy;

        /// <summary>
        /// Preset simplificado recomendado para 99% dos planetas.
        /// </summary>
        public PlanetDefensePresetSo DefensePreset => defensePreset;

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