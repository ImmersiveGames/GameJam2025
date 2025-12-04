using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// LEGACY: Preset de Wave separado. Mantido para compatibilidade, mas substituído
    /// por <see cref="DefenseEntryConfigSo"/> que agrega entrada + wave em um único asset.
    /// Define apenas quantidade e cadência de spawn por wave, sem alterar
    /// comportamento dos minions (apenas parâmetros de tempo e distribuição).
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseWaveProfile",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Config/Wave Profile")]

    public sealed class DefenseWaveProfileSo : ScriptableObject
    {
        [Header("Wave Timing")]
        [Tooltip("Intervalo em segundos entre waves (cada instância de spawn da defesa).")]
        public int secondsBetweenWaves = 5;

        [Header("Spawn Settings")]
        [Tooltip("Quantos minions entram em cada wave deste preset.")]
        public int enemiesPerWave = 6;
        public float spawnRadius = 4f;
        public float spawnHeightOffset = 0.5f;

        [Header("Minion Defaults")]
        [Tooltip("Profile padrão aplicado aos minions da wave; não define comportamento global do minion.")]
        public DefenseMinionBehaviorProfileSO defaultMinionProfile;

        [Header("Spawn Pattern (opcional)")]
        [Tooltip("Padrão de spawn em órbita. Se nulo, usa distribuição aleatória padrão.")]
        public DefenseSpawnPatternSo spawnPattern;

        private void OnValidate()
        {
            secondsBetweenWaves = Mathf.Max(1, secondsBetweenWaves);
            enemiesPerWave = Mathf.Max(1, enemiesPerWave);
        }
    }
}
