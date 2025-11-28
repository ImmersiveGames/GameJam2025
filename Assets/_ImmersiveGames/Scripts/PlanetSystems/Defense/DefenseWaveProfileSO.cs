using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Defense/Wave Profile")]
    public sealed class DefenseWaveProfileSO : ScriptableObject
    {
        [Header("Wave Timing")]
        [Tooltip("Intervalo em segundos entre waves.")]
        [Min(1)]
        public int secondsBetweenWaves = 5;

        [Header("Spawn Settings")]
        [Tooltip("Quantos inimigos aparecem em cada wave.")]
        [Min(1)]
        public int enemiesPerWave = 6;

        [Tooltip("Raio ao redor do planeta para spawn dos inimigos.")]
        public float spawnRadius = 4f;

        [Tooltip("Offset vertical aplicado ao spawn.")]
        public float spawnHeightOffset = 0.5f;

        private void OnEnable()
        {
            ApplySafetyDefaults();
        }

        private void OnValidate()
        {
            ApplySafetyDefaults();
        }

        private void ApplySafetyDefaults()
        {
            secondsBetweenWaves = Mathf.Max(1, secondsBetweenWaves);
            enemiesPerWave = Mathf.Max(1, enemiesPerWave);
        }
    }
}
