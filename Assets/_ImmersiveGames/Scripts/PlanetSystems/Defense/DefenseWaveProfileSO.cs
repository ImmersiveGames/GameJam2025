using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Defense/Wave Profile")]
    public sealed class DefenseWaveProfileSO : ScriptableObject
    {
        [Header("Wave Timing (seconds between waves)")]
        public int secondsBetweenWaves = 5;

        [Header("Spawn Settings")]
        public int enemiesPerWave = 6;
        public float spawnRadius = 4f;
        public float spawnHeightOffset = 0.5f;
    }
}
