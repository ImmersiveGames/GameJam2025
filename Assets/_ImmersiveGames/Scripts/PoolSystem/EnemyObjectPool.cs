using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.EnemySystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.PoolSystem
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

        private new void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform); // Filho do pool
                obj.SetActive(false);
                ConfigureObject(obj); // Configurar na inicialização
                _pool.Add(obj);
            }
        }

        protected override void ConfigureObject(GameObject obj)
        {
            // Setup inicial básico sem logs
            var pooledEnemy = obj.GetComponent<EnemyPooledObject>();
            if (pooledEnemy != null)
            {
                pooledEnemy.SetPool(this);
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
                    // Configuração completa apenas quando vai ser usado
                    EnemyData selectedData = _enemyDatas[Random.Range(0, _enemyDatas.Count)];
                
                    enemy.Setup(selectedData);
                    enemy.Configure(target.transform);

                    DebugUtility.LogVerbose<EnemyObjectPool>(
                        $"Inimigo {enemyObj.name} configurado com {selectedData.name}.",
                        "cyan",
                        this
                    );
                }
            }
            return enemyObj;
        }
    }
}