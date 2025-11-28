using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Defense/Wave Profile")]
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

        private void OnValidate()
        {
            secondsBetweenWaves = Mathf.Max(1, secondsBetweenWaves);
            enemiesPerWave = Mathf.Max(1, enemiesPerWave);
        }
    }
}
