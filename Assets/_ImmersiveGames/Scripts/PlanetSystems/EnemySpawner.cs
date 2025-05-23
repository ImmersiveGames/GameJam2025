using UnityEngine;
using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.EnemySystem;
using _ImmersiveGames.Scripts.PoolSystem;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using Random = UnityEngine.Random;
using System.Linq;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Configurações do Spawner")]
        [SerializeField, Tooltip("Cadência de spawn (segundos entre spawns)")]
        private float spawnRate = 2f;

        [SerializeField, Tooltip("Ativar logs e visualizações para depuração")]
        private bool debugMode;

        [SerializeField, Tooltip("Prefab do efeito de partícula para spawn (opcional)")]
        private ParticleSystem spawnEffectPrefab;

        private DetectionSensor _detectionSensor;
        private Transform _cachedTransform;
        private PlayerInput _currentTarget;
        private float _spawnTimer;
        private float _spawnRate;  // Movido para variável local
        private bool _isSpawning;
        private PlanetData _planetData;
        private int _maxEnemies;
        private EnemyObjectPool _enemyPool;
        private Planets _planet;

        private void Awake()
        {
            _cachedTransform = transform;
            _detectionSensor = GetComponent<DetectionSensor>();
            _planet = GetComponent<Planets>();
        }

        private void OnEnable()
        {
            var planet = GetComponent<Planets>();
            if (planet != null)
            {
                planet.OnPlanetCreated += Initialize;
                planet.OnPlanetDestroyed += OnPlanetDestroyed;
            }
            if (_detectionSensor != null)
            {
                _detectionSensor.OnTargetEnterDetection += StartSpawning;
                _detectionSensor.OnTargetExitDetection += StopSpawning;
            }
        }

        private void OnDisable()
        {
            var planet = GetComponent<Planets>();
            if (planet != null)
            {
                planet.OnPlanetCreated -= Initialize;
                planet.OnPlanetDestroyed -= OnPlanetDestroyed;
            }
            if (_detectionSensor != null)
            {
                _detectionSensor.OnTargetEnterDetection -= StartSpawning;
                _detectionSensor.OnTargetExitDetection -= StopSpawning;
            }
        }

        private void Initialize(PlanetData planetData)
        {
            _planetData = planetData;
            _maxEnemies = _planetData.maxEnemies;
            _spawnRate = _planetData.spawnRate;

            if (_detectionSensor == null)
            {
                DebugUtility.LogError<EnemySpawner>("DetectionSensor não encontrado.", this);
                enabled = false;
                return;
            }
            if (_planetData.enemyPrefab == null)
            {
                DebugUtility.LogError<EnemySpawner>("EnemyPrefab não configurado em PlanetData.", this);
                enabled = false;
                return;
            }
            if (_planetData.enemyDatas == null || _planetData.enemyDatas.Count == 0)
            {
                DebugUtility.LogError<EnemySpawner>("Lista de EnemyData vazia.", this);
                enabled = false;
                return;
            }
            var validEnemyDatas = _planetData.enemyDatas
                .Where(data => data != null && data.modelPrefab != null)
                .ToList();
            if (validEnemyDatas.Count == 0)
            {
                DebugUtility.LogError<EnemySpawner>("Nenhum EnemyData válido encontrado (todos nulos ou sem modelPrefab).", this);
                enabled = false;
                return;
            }
            if (validEnemyDatas.Count < _planetData.enemyDatas.Count)
            {
                DebugUtility.LogWarning<EnemySpawner>($"Encontrados {_planetData.enemyDatas.Count - validEnemyDatas.Count} EnemyData inválidos (nulos ou sem modelPrefab). Usando apenas {validEnemyDatas.Count} válidos.", this);
            }
            if (spawnRate <= 0f)
            {
                DebugUtility.LogError<EnemySpawner>("Spawn rate inválido.", this);
                spawnRate = 0.1f;
            }
            if (_maxEnemies <= 0)
            {
                DebugUtility.LogError<EnemySpawner>("Máximo de inimigos inválido.", this);
                _maxEnemies = 1;
            }
            if (_planetData.size <= 0f)
            {
                DebugUtility.LogError<EnemySpawner>("Tamanho do planeta inválido.", this);
                _planetData.size = 1f;
            }

            GameObject poolObj = new GameObject("EnemyPool");
            poolObj.transform.SetParent(transform);
            _enemyPool = poolObj.AddComponent<EnemyObjectPool>();
            _enemyPool.Initialize(_planetData.enemyPrefab, validEnemyDatas, _maxEnemies);

            if (debugMode)
            {
                DebugUtility.LogVerbose<EnemySpawner>($"Spawner inicializado com {_maxEnemies} inimigos máximos em {gameObject.name}.", "cyan");
            }
        }

        private void Update()
        {
            if (!_isSpawning || _currentTarget == null || !_planet.IsActive) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= spawnRate)
            {
                SpawnEnemy();
                _spawnTimer = 0f;
            }
        }

        private void StartSpawning(PlayerInput target)
        {
            if (!_planet.IsActive || (_isSpawning && _currentTarget == target)) return;

            _currentTarget = target;
            _isSpawning = true;
            _spawnTimer = spawnRate;
        
            if (debugMode)
            {
                DebugUtility.LogVerbose<EnemySpawner>($"Iniciando spawn para jogador {target.name} em {gameObject.name}.", "green");
            }
        }

        private void StopSpawning(PlayerInput target)
        {
            if (!_isSpawning) return;

            _isSpawning = false;
            _currentTarget = null;
            if (debugMode)
            {
                DebugUtility.LogVerbose<EnemySpawner>($"Parando spawn para jogador {target.name} em {gameObject.name}.", "red");
            }
        }

        private void OnPlanetDestroyed(PlanetData planetData)
        {
            _isSpawning = false;
            _currentTarget = null;
            if (_enemyPool != null)
            {
                foreach (var enemyObj in _enemyPool.GetComponentsInChildren<Enemy>(true))
                {
                    if (enemyObj.gameObject.activeSelf)
                    {
                        _enemyPool.ReturnToPool(enemyObj.gameObject);
                    }
                }
                Destroy(_enemyPool.gameObject);
            }
            if (debugMode)
            {
                DebugUtility.LogVerbose<EnemySpawner>($"Planeta destruído, pool limpo em {gameObject.name}.", "red");
            }
        }

        private void SpawnEnemy()
        {
            if (_planetData == null || _enemyPool == null)
            {
                DebugUtility.LogError<EnemySpawner>("Dados necessários não inicializados.", this);
                return;
            }

            Vector2 spawnOffset = Random.insideUnitCircle.normalized * _planetData.size;
            Vector3 spawnPosition = _cachedTransform.position + new Vector3(spawnOffset.x, 0f, spawnOffset.y);

            GameObject enemyObj = _enemyPool.GetEnemy(spawnPosition, Quaternion.identity, _currentTarget, _maxEnemies);
        
            if (enemyObj != null && spawnEffectPrefab != null)
            {
                ParticleSystem effect = Instantiate(spawnEffectPrefab, spawnPosition, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }

            if (debugMode)
            {
                DebugUtility.LogVerbose<EnemySpawner>($"Inimigo {enemyObj?.name} spawnado em {spawnPosition}.", "blue", this);
            }
        }

        private void OnDrawGizmos()
        {
            if (!debugMode || _cachedTransform == null) return;

            if (_isSpawning && _currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(_cachedTransform.position, _currentTarget.transform.position);
            }
        }
    }
}