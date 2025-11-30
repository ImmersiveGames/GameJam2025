using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Configuração de defesa por planeta:
    /// - Qual PoolData usar para as defesas
    /// - Qual perfil de onda (wave profile)
    ///
    /// A ideia é que o planeta use isso como "preset" planetário.
    /// O comportamento do minion (velocidade, entry, etc.) continua
    /// centralizado no DefensesMinionData + DefenseMinionBehaviorProfile.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefenseLoadout",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Loadout")]
    public sealed class PlanetDefenseLoadoutSo : ScriptableObject
    {
        [Header("Pool de defesas (por planeta)")]
        [Tooltip("PoolData usado para spawnar os minions defensivos deste planeta.")]
        [SerializeField]
        private PoolData defensePoolData;

        [Header("Perfil de ondas (por planeta)")]
        [Tooltip("WaveProfile específico deste planeta. Se nulo, o controller pode cair no profile padrão configurado nele.")]
        [SerializeField]
        private DefenseWaveProfileSo waveProfileOverride;

        /// <summary>
        /// PoolData que o planeta quer usar para suas defesas.
        /// Pode ser nulo; nesse caso o sistema pode cair em um Default configurado no próprio planeta/serviço.
        /// </summary>
        public PoolData DefensePoolData => defensePoolData;

        /// <summary>
        /// WaveProfile específico deste planeta (pode ser nulo).
        /// </summary>
        public DefenseWaveProfileSo WaveProfileOverride => waveProfileOverride;
    }
}