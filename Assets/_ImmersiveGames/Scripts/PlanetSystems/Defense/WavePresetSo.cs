using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Descreve um preset de wave contendo perfil, pool e comportamento
    /// obrigatório para os minions. Mantém SRP concentrando os dados
    /// necessários para spawn sem replicar lógica do planeta.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WavePreset",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Wave Preset")]
    public sealed class WavePresetSo : ScriptableObject
    {
        [Header("Wave Profile")]
        [Tooltip("Perfil de onda com contagem, intervalos, raio e altura de spawn.")]
        [SerializeField]
        private DefenseWaveProfileSo waveProfile;

        [Header("Pool Obrigatório")]
        [Tooltip("Pool que contém o prefab e ciclo de vida de cada minion desta wave.")]
        [SerializeField]
        private PoolData poolData;

        [Header("Comportamento do Minion")]
        [Tooltip("Configuração padrão de minion usada para instanciar e dirigir a wave.")]
        [SerializeField]
        private DefensesMinionData defaultMinionData;

        [Header("Estratégia de Defesa")]
        [Tooltip("Estratégia a ser aplicada para conduzir os minions desta wave.")]
        [SerializeField]
        private DefenseStrategySo defenseStrategy;

        /// <summary>
        /// Perfil de onda base com limites de spawn e cadência.
        /// </summary>
        public DefenseWaveProfileSo WaveProfile => waveProfile;

        /// <summary>
        /// Pool obrigatório de minions para evitar alocação em runtime.
        /// </summary>
        public PoolData PoolData => poolData;

        /// <summary>
        /// Minion padrão associado a esta wave.
        /// </summary>
        public DefensesMinionData DefaultMinionData => defaultMinionData;

        /// <summary>
        /// Estratégia opcional aplicada à wave.
        /// </summary>
        public DefenseStrategySo DefenseStrategy => defenseStrategy;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (waveProfile == null)
            {
                Debug.LogError("WaveProfile obrigatório — configure para evitar dados inválidos.", this);
            }

            if (poolData == null)
            {
                Debug.LogError("PoolData obrigatório — configure para habilitar spawn consistente.", this);
            }

            if (defaultMinionData == null)
            {
                Debug.LogError("DefaultMinionData obrigatório — configure para definir o comportamento do minion.", this);
            }
        }
#endif
    }
}
