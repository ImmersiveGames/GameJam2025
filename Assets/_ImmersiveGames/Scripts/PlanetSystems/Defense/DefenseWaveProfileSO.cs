using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Defense/Wave Profile")]
    public sealed class DefenseWaveProfileSO : ScriptableObject
    {
        [Header("Wave Timing (seconds between waves)")]
        [Tooltip("Intervalo em segundos entre waves (cadência do spawn).")]
        public int secondsBetweenWaves = 5;

        [Header("Spawn Settings")]
        [Tooltip("Quantos inimigos aparecem em cada wave (spawn simultâneo).")]
        public int enemiesPerWave = 6;
        public float spawnRadius = 4f;
        public float spawnHeightOffset = 0.5f;

        private void OnValidate()
        {
            // Garantir cadência e tamanho mínimos válidos.
            secondsBetweenWaves = Mathf.Max(1, secondsBetweenWaves);
            enemiesPerWave = Mathf.Max(1, enemiesPerWave);
        }
    }
}
