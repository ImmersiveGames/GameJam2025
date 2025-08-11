using System.Collections;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
using UnityEngine.Events;
namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    [DebugLevel(DebugLevel.Verbose)]
    public class SpawnSystem : MonoBehaviour
    {
        [SerializeField] private string poolKey = "PoolBullet";
        [SerializeField] private float spawnRate = 1f; // Spawns por segundo
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);
        [SerializeField] private Vector3 basePosition = Vector3.zero;
        [SerializeField] private int maxActiveObjects = 10; // Limite de objetos ativos
        [SerializeField] private bool autoStart = true; // Iniciar spawn automaticamente

        public UnityEvent<IPoolable> OnObjectSpawned { get; } = new UnityEvent<IPoolable>();

        private ObjectPool _pool;
        private ISpawnPattern _spawnPattern;
        private bool _isSpawning;
        private IActor _spawner; // Para futura integração com IActor

        private void Awake()
        {
            _spawnPattern = new RandomSpawnPattern(); // Padrão inicial
        }

        private void Start()
        {
            Initialize();
            if (autoStart)
            {
                StartSpawning();
            }
        }

        private void Initialize()
        {
            if (PoolManager.Instance == null)
            {
                DebugUtility.LogError<SpawnSystem>("PoolManager not found in scene.", this);
                enabled = false;
                return;
            }

            _pool = PoolManager.Instance.GetPool(poolKey);
            if (_pool == null)
            {
                DebugUtility.LogError<SpawnSystem>($"Pool '{poolKey}' not found or not initialized.", this);
                enabled = false;
                return;
            }

            DebugUtility.Log<SpawnSystem>($"SpawnSystem initialized with pool '{poolKey}'.", "green", this);
        }

        public void StartSpawning()
        {
            if (_isSpawning)
            {
                DebugUtility.LogWarning<SpawnSystem>("SpawnSystem already spawning.", this);
                return;
            }

            _isSpawning = true;
            StartCoroutine(SpawnRoutine());
            DebugUtility.Log<SpawnSystem>("Started spawning objects from pool '{poolKey}'.", "cyan", this);
        }

        public void StopSpawning()
        {
            if (!_isSpawning)
            {
                DebugUtility.LogWarning<SpawnSystem>("SpawnSystem not spawning.", this);
                return;
            }

            _isSpawning = false;
            StopCoroutine(SpawnRoutine());
            DebugUtility.Log<SpawnSystem>("Stopped spawning objects from pool '{poolKey}'.", "cyan", this);
        }

        private IEnumerator SpawnRoutine()
        {
            while (_isSpawning)
            {
                if (_pool.GetActiveObjects().Count < maxActiveObjects)
                {
                    Vector3 position = _spawnPattern.GetSpawnPosition(basePosition, spawnAreaSize);
                    var poolable = _pool.GetObject(position, _spawner);
                    if (poolable != null)
                    {
                        OnObjectSpawned.Invoke(poolable);
                        DebugUtility.Log<SpawnSystem>($"Spawned object '{poolable.GetGameObject().name}' at {position}. Active: {poolable.GetGameObject().activeSelf}", "green", this);
                    }
                    else
                    {
                        DebugUtility.LogWarning<SpawnSystem>($"Failed to spawn object from pool '{poolKey}'.", this);
                    }
                }
                else
                {
                    DebugUtility.LogVerbose<SpawnSystem>($"Max active objects ({maxActiveObjects}) reached for pool '{poolKey}'.", "yellow", this);
                }
                yield return new WaitForSeconds(1f / spawnRate);
            }
        }

        public void SetSpawnPattern(ISpawnPattern pattern)
        {
            _spawnPattern = pattern ?? new RandomSpawnPattern();
            DebugUtility.Log<SpawnSystem>($"Spawn pattern set to {pattern.GetType().Name}.", "cyan", this);
        }

        public void SetSpawner(IActor spawner)
        {
            _spawner = spawner;
            DebugUtility.Log<SpawnSystem>($"Spawner set to {spawner?.GetType().Name ?? "null"}.", "cyan", this);
        }

        private void OnDestroy()
        {
            StopSpawning();
        }
    }
}