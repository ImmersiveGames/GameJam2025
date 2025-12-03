using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(
        fileName = "DefenseWaveProfile",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Config/Wave Profile")]

    public sealed class DefenseWaveProfileSo : ScriptableObject
    {
        [Header("Wave Timing")]
        [Tooltip("Intervalo em segundos entre waves.")]
        public int secondsBetweenWaves = 5;

        [Header("Spawn Settings")]
        [Tooltip("Quantos inimigos aparecem em cada wave.")]
        public int enemiesPerWave = 6;
        public float spawnRadius = 4f;
        public float spawnHeightOffset = 0.5f;

        [Header("Minion Defaults")]
        [Tooltip("Profile padrão aplicado a todos os minions desta wave.")]
        public DefenseMinionBehaviorProfileSO defaultMinionProfile;

        [Header("Spawn Pattern (opcional)")]
        [Tooltip("Padrão de spawn em órbita. Se nulo, usa distribuição aleatória padrão.")]
        public DefenseSpawnPatternSo spawnPattern;

        private void OnValidate()
        {
            secondsBetweenWaves = Mathf.Max(1, secondsBetweenWaves);
            enemiesPerWave = Mathf.Max(1, enemiesPerWave);

            if (defaultMinionProfile == null)
            {
                Debug.LogError($"[DefenseWaveProfileSo] {name} sem defaultMinionProfile. Configure um profile padrão para evitar comportamento inconsistente.");
            }

            if (spawnRadius < 0f)
            {
                Debug.LogWarning($"[DefenseWaveProfileSo] {name} possui spawnRadius negativo. Normalizando para 0 para evitar posições inválidas.");
                spawnRadius = Mathf.Max(0f, spawnRadius);
            }
        }
    }
}
