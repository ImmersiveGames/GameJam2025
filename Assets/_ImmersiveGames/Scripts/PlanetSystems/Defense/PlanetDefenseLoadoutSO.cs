using _ImmersiveGames.Scripts.Utils.DebugSystems;
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
        [SerializeField]
        [FormerlySerializedAs("defensePreset")]
        private PlanetDefensePresetSo planetDefensePreset;

        [Header("Pool de defesas (por planeta)")]
        [Tooltip("PoolData usado para spawnar os minions defensivos deste planeta.")]
        [FormerlySerializedAs("defensePoolData")]
        [SerializeField, HideInInspector]
        private PoolData planetDefensePoolData;

        [Header("Perfil de ondas (por planeta)")]
        [Tooltip("WaveProfile específico deste planeta. Se nulo, o controller pode cair no profile padrão configurado nele.")]
        [FormerlySerializedAs("waveProfileOverride")]
        [SerializeField, HideInInspector]
        private DefenseWaveProfileSo planetWaveProfileOverride;

        [Header("Estratégia de defesa (por planeta)")]
        [Tooltip("Estratégia opcional que customiza comportamento de waves e minions para este planeta.")]
        [FormerlySerializedAs("defenseStrategy")]
        [SerializeField, HideInInspector]
        private DefenseStrategySo planetDefenseStrategyOverride;

        /// <summary>
        /// Preset principal de defesa, preferido pelos serviços de orquestração.
        /// Mantém compatibilidade com SRP ao compor dados de ondas, estratégia e alvo.
        /// </summary>
        public PlanetDefensePresetSo DefensePreset => planetDefensePreset;

        /// <summary>
        /// PoolData que o planeta quer usar para suas defesas.
        /// Pode ser nulo; nesse caso o sistema pode cair em um Default configurado no próprio planeta/serviço.
        /// </summary>
        public PoolData DefensePoolData => planetDefensePoolData;

        /// <summary>
        /// WaveProfile específico deste planeta (pode ser nulo).
        /// </summary>
        public DefenseWaveProfileSo WaveProfileOverride => planetWaveProfileOverride;

        /// <summary>
        /// Estratégia defensiva opcional deste planeta.
        /// </summary>
        public DefenseStrategySo DefenseStrategy => planetDefenseStrategyOverride;

        private void OnValidate()
        {
            if (planetDefensePreset == null)
            {
                DebugUtility.LogError<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} está sem PlanetDefensePreset definido. Configure um preset para evitar fallbacks silenciosos.");
            }

            if (planetDefensePoolData != null)
            {
                DebugUtility.LogWarning<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} ainda referencia PoolData direto. Use o preset para consolidar configuração e evitar duplicação.");
            }

            if (planetWaveProfileOverride != null || planetDefenseStrategyOverride != null)
            {
                DebugUtility.LogWarning<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} possui overrides legados (wave/strategy). Centralize essas escolhas no preset para manter SRP.");
            }
        }
    }
}