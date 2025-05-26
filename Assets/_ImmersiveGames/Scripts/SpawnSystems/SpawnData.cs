using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public enum SpawnPattern { Single, Wave, Random, Burst }

    [CreateAssetMenu(fileName = "SpawnData", menuName = "SpawnSystem/SpawnData")]
    public class SpawnData : ScriptableObject
    {
        [SerializeField] private PoolableObjectData poolableData;
        [SerializeField] private int spawnCount = 1;
        [SerializeField] private SpawnPattern pattern = SpawnPattern.Single;

        [Header("Single Spawn Settings")]
        [SerializeField] private Vector3 spawnDirection = Vector3.forward;
        [SerializeField] private float spawnSpeed = 5f;

        [Header("Wave Spawn Settings")]
        [SerializeField] private float waveInterval = 1f;
        [SerializeField] private int waveCount = 3;

        [Header("Random Spawn Settings")]
        [SerializeField] private Vector2 spawnArea = new Vector2(5f, 5f); // Área em X, Z (top-down)

        [Header("Burst Spawn Settings")]
        [SerializeField] private float radius = 2f; // Raio do círculo

        // Propriedades públicas
        public PoolableObjectData PoolableData => poolableData;
        public int SpawnCount => spawnCount;
        public SpawnPattern Pattern => pattern;
        public Vector3 SpawnDirection => spawnDirection;
        public float SpawnSpeed => spawnSpeed;
        public float WaveInterval => waveInterval;
        public int WaveCount => waveCount;
        public Vector2 SpawnArea => spawnArea;
        public float Radius => radius;
    }
}