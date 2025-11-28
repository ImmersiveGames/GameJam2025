using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Defense/Wave Profile")]
    public sealed class DefenseWaveProfileSO : ScriptableObject
    {
        [Header("Wave Timing")]
        public float waveIntervalSeconds = 5f;

        [Header("Spawn Settings")]
        public int minionsPerWave = 6;
        public float spawnRadius = 4f;
        public float spawnHeightOffset = 0.5f;
    }
}
