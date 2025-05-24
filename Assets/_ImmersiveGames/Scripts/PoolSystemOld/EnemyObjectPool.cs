using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.EnemySystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.PoolSystemOld
{
    public class EnemyObjectPool : ObjectPoolBase
    {
        private List<EnemyData> _enemyDatas;

        public void Initialize(GameObject enemyPrefab, List<EnemyData> enemyDatas, int maxEnemies)
        {
            prefab = enemyPrefab;
            initialPoolSize = maxEnemies;
            _enemyDatas = enemyDatas;

            if (prefab == null)
            {
                DebugUtility.LogError<EnemyObjectPool>("Prefab não configurado.", this);
                return;
            }
            if (_enemyDatas == null || _enemyDatas.Count == 0)
            {
                DebugUtility.LogError<EnemyObjectPool>("Lista de EnemyData vazia ou nula.", this);
                return;
            }

            // Filtrar EnemyData inválidos
            _enemyDatas = _enemyDatas.Where(data => data != null && data.modelPrefab != null).ToList();
            if (_enemyDatas.Count == 0)
            {
                DebugUtility.LogError<EnemyObjectPool>("Nenhum EnemyData válido encontrado.", this);
                return;
            }

            InitializePool();
        }

        protected override void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                obj.SetActive(false);
                ConfigureObject(obj);
                _pool.Add(obj);
                _inactiveObjects.Enqueue(obj);
            }
        }

        protected override void ConfigureObject(GameObject obj)
        {
            var pooledEnemy = obj.GetComponent<EnemyPooledObject>();
            if (pooledEnemy != null)
            {
                pooledEnemy.SetPool(this);
            }
            else
            {
                DebugUtility.LogError<EnemyObjectPool>($"Objeto {obj.name} não tem EnemyPooledObject.", this);
            }
        }

        protected override void ResetObject(GameObject obj)
        {
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            var enemy = obj.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.ResetState(); // Método personalizado para resetar estados do inimigo
            }
        }

        public GameObject GetEnemy(Vector3 position, Quaternion rotation, PlayerInput target, int maxEnemies)
        {
            GameObject enemyObj = GetObject(position, rotation, maxEnemies);
            if (enemyObj != null)
            {
                var enemy = enemyObj.GetComponent<Enemy>();
                if (enemy != null)
                {
                    EnemyData selectedData = _enemyDatas[Random.Range(0, _enemyDatas.Count)];
                    enemy.Setup(selectedData);
                    enemy.Configure(target.transform);

                    // Ajustar rotação para top-down (eixo XZ)
                    Vector3 direction = (target.transform.position - position).normalized;
                    direction.y = 0;
                    enemyObj.transform.rotation = Quaternion.LookRotation(direction);

                    DebugUtility.LogVerbose<EnemyObjectPool>(
                        $"Inimigo {enemyObj.name} configurado com {selectedData.name}.",
                        "cyan",
                        this
                    );
                }
                else
                {
                    DebugUtility.LogError<EnemyObjectPool>($"Objeto {enemyObj.name} não tem componente Enemy.", this);
                    ReturnToPool(enemyObj);
                    return null;
                }
            }
            return enemyObj;
        }
    }
}