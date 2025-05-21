using UnityEngine;
using System.Collections;

namespace _ImmersiveGames.Scripts.EnemySystem
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private int _maxEnemies = 5;
        [SerializeField] private float _spawnInterval = 3f;
        [SerializeField] private float _spawnRadius = 10f;
        [SerializeField] private Transform _player;
        [SerializeField] private bool _spawnOnStart = true;
        
        private int _currentEnemies;
        private Coroutine _spawnCoroutine;
        
        private void Start()
        {
            if (_player == null)
            {
                _player = GameObject.FindGameObjectWithTag("Player")?.transform;
                
                if (_player == null)
                {
                    Debug.LogWarning("EnemySpawner não conseguiu encontrar um objeto com a tag 'Player'!");
                }
            }
            
            if (_spawnOnStart)
            {
                StartSpawning();
            }
        }
        
        public void StartSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
            }
            
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
        
        public void StopSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }
        
        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                if (_currentEnemies < _maxEnemies)
                {
                    SpawnEnemy();
                }
                
                yield return new WaitForSeconds(_spawnInterval);
            }
        }
        
        private void SpawnEnemy()
        {
            if (_enemyPrefab == null)
                return;
                
            // Posição de spawn aleatória em um círculo ao redor do jogador
            Vector2 randomCircle = Random.insideUnitCircle.normalized * _spawnRadius;
            Vector3 spawnPosition;
            
            if (_player != null)
            {
                // Spawn relativo ao jogador
                spawnPosition = _player.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            }
            else
            {
                // Spawn relativo ao spawner
                spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            }
            
            // Ajusta a altura Y se necessário
            spawnPosition.y = transform.position.y;
            
            // Instancia o inimigo
            GameObject newEnemy = Instantiate(_enemyPrefab, spawnPosition, Quaternion.identity);
            _currentEnemies++;
            
            // Configura um listener para quando o inimigo morrer
            Enemy enemyComponent = newEnemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.OnEnemyDeath += HandleEnemyDeath;
            }
        }
        
        private void HandleEnemyDeath(Enemy enemy)
        {
            // Remove o listener para evitar vazamentos de memória
            enemy.OnEnemyDeath -= HandleEnemyDeath;
            
            // Decrementa o contador de inimigos ativos
            _currentEnemies--;
        }
    }
}
