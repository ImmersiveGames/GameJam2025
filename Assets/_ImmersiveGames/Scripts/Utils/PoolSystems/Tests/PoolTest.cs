using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray; // Array de dados para criar pools
        [SerializeField] private float spawnInterval = 1f; // Intervalo entre spawns automáticos
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f); // Área XZ para spawn
        [SerializeField] private KeyCode spawnKey = KeyCode.Space; // Tecla para spawn manual
        [SerializeField] private KeyCode returnAllKey = KeyCode.R; // Tecla para retornar todos os objetos
        [SerializeField] private KeyCode listActiveKey = KeyCode.L; // Tecla para listar objetos ativos
        [SerializeField] private KeyCode exhaustPoolKey = KeyCode.E; // Tecla para testar exaustão do pool
        [SerializeField] private KeyCode spawnMultipleKey = KeyCode.M; // Tecla para spawnar múltiplos objetos
        [SerializeField, Min(1)] private int multipleSpawnCount = 3; // Quantidade de objetos a spawnar com M

        private float _timer;
        private EventBinding<PoolExhaustedEvent> _poolExhaustedBinding;
        private EventBinding<PoolRestoredEvent> _poolRestoredBinding;

        private void OnEnable()
        {
            _poolExhaustedBinding = new EventBinding<PoolExhaustedEvent>(OnPoolExhausted);
            EventBus<PoolExhaustedEvent>.Register(_poolExhaustedBinding);
            _poolRestoredBinding = new EventBinding<PoolRestoredEvent>(OnPoolRestored);
            EventBus<PoolRestoredEvent>.Register(_poolRestoredBinding);
        }

        private void OnDisable()
        {
            EventBus<PoolExhaustedEvent>.Unregister(_poolExhaustedBinding);
            EventBus<PoolRestoredEvent>.Unregister(_poolRestoredBinding);
        }

        private void Start()
        {
            // Verificar se LifetimeManager está presente
            if (LifetimeManager.Instance == null)
            {
                DebugUtility.LogError<PoolTest>("LifetimeManager não encontrado na cena. Adicione um GameObject com LifetimeManager.", this);
                enabled = false;
                return;
            }

            // Verificar se PoolManager está presente
            if (PoolManager.Instance == null)
            {
                DebugUtility.LogError<PoolTest>("PoolManager não encontrado na cena.", this);
                enabled = false;
                return;
            }

            // Verificar se poolDataArray está configurado
            if (poolDataArray == null || poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolTest>("poolDataArray está vazio ou não configurado.", this);
                enabled = false;
                return;
            }

            // Registrar pools no PoolManager
            foreach (var data in poolDataArray)
            {
                if (data == null)
                {
                    DebugUtility.LogError<PoolTest>("PoolData nulo encontrado em poolDataArray.", this);
                    continue;
                }

                if (PoolValidationUtility.ValidatePoolData(data, this))
                {
                    PoolManager.Instance.RegisterPool(data);
                    DebugUtility.Log<PoolTest>($"Registrado pool '{data.ObjectName}' com tamanho inicial {data.InitialPoolSize}.", "green", this);
                }
                else
                {
                    DebugUtility.LogError<PoolTest>($"Falha ao validar PoolData '{data.name}'.", this);
                }
            }
        }

        private void Update()
        {
            // Spawn automático a cada intervalo
            _timer += Time.deltaTime;
            if (_timer >= spawnInterval)
            {
                SpawnRandomObject();
                _timer = 0f;
            }

            // Spawn manual com tecla
            if (Input.GetKeyDown(spawnKey))
            {
                SpawnRandomObject();
            }

            // Retornar todos os objetos ativos com tecla
            if (Input.GetKeyDown(returnAllKey))
            {
                ReturnAllObjects();
            }

            // Listar objetos ativos com tecla
            if (Input.GetKeyDown(listActiveKey))
            {
                ListActiveObjects();
            }

            // Testar exaustão do pool com tecla
            if (Input.GetKeyDown(exhaustPoolKey))
            {
                ExhaustPool();
            }

            // Spawnar múltiplos objetos com tecla
            if (Input.GetKeyDown(spawnMultipleKey))
            {
                SpawnMultipleObjects();
            }
        }

        private void SpawnRandomObject()
        {
            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogWarning<PoolTest>("Nenhum PoolData configurado.", this);
                return;
            }

            // Selecionar um PoolData aleatoriamente
            var data = poolDataArray[Random.Range(0, poolDataArray.Length)];
            if (data == null)
            {
                DebugUtility.LogError<PoolTest>("PoolData selecionado é nulo.", this);
                return;
            }

            var pool = PoolManager.Instance.GetPool(data.ObjectName);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{data.ObjectName}' não encontrado. Verifique o registro no PoolManager.", this);
                return;
            }

            // Gerar posição aleatória (exemplo para teste)
            Vector3 position = new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                0f, // Y pode ser ajustado
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );

            var poolable = pool.GetObject(position);
            if (poolable != null)
            {
                DebugUtility.Log<PoolTest>($"Spawned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) at position {position}.", "green", this);
            }
            else
            {
                DebugUtility.LogWarning<PoolTest>($"Falha ao obter objeto do pool '{data.ObjectName}'. Verifique InitialPoolSize ou CanExpand.", this);
            }
        }

        private void SpawnMultipleObjects()
        {
            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogWarning<PoolTest>("Nenhum PoolData configurado para spawn múltiplo.", this);
                return;
            }

            // Selecionar um PoolData aleatoriamente
            var data = poolDataArray[Random.Range(0, poolDataArray.Length)];
            if (data == null)
            {
                DebugUtility.LogError<PoolTest>("PoolData selecionado é nulo.", this);
                return;
            }

            var pool = PoolManager.Instance.GetPool(data.ObjectName);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{data.ObjectName}' não encontrado para spawn múltiplo.", this);
                return;
            }

            // Gerar posição base (exemplo para teste)
            Vector3 basePosition = new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                0f, // Y pode ser ajustado
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );

            var poolables = pool.GetMultipleObjects(multipleSpawnCount, basePosition);
            DebugUtility.Log<PoolTest>($"Tentativa de spawn múltiplo: Solicitados {multipleSpawnCount}, obtidos {poolables.Count} objetos do pool '{data.ObjectName}'.", "green", this);

            // Ajustar posições em um círculo
            for (int i = 0; i < poolables.Count; i++)
            {
                if (poolables[i] != null && poolables[i].GetGameObject() != null)
                {
                    float angle = i * (360f / poolables.Count);
                    Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * 1f;
                    Vector3 position = basePosition + offset;
                    poolables[i].GetGameObject().transform.position = position;
                    DebugUtility.Log<PoolTest>($"Spawned object '{data.ObjectName}' (ID: {poolables[i].GetGameObject().GetInstanceID()}) at position {position}.", "green", this);
                }
            }
        }

        private void ReturnAllObjects()
        {
            foreach (var data in poolDataArray)
            {
                if (data == null) continue;

                var pool = PoolManager.Instance.GetPool(data.ObjectName);
                if (pool == null)
                {
                    DebugUtility.LogError<PoolTest>($"Pool '{data.ObjectName}' não encontrado.", this);
                    continue;
                }

                var activeObjects = pool.GetActiveObjects().ToList();
                if (activeObjects.Count == 0)
                {
                    DebugUtility.Log<PoolTest>($"Nenhum objeto ativo no pool '{data.ObjectName}'.", "yellow", this);
                }

                foreach (var obj in activeObjects)
                {
                    if (obj != null && obj.GetGameObject() != null)
                    {
                        obj.Deactivate();
                        DebugUtility.Log<PoolTest>($"Returned object '{data.ObjectName}' (ID: {obj.GetGameObject().GetInstanceID()}) to pool.", "blue", this);
                    }
                    else
                    {
                        DebugUtility.LogWarning<PoolTest>($"Objeto nulo encontrado no pool '{data.ObjectName}'.", this);
                    }
                }
            }
        }

        private void ListActiveObjects()
        {
            DebugUtility.Log<PoolTest>("Listando objetos ativos e disponíveis:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogWarning<PoolTest>("Nenhum PoolData configurado para listar.", this);
                return;
            }

            bool anyPoolListed = false;
            foreach (var data in poolDataArray)
            {
                if (data == null)
                {
                    DebugUtility.LogError<PoolTest>("PoolData nulo encontrado em poolDataArray.", this);
                    continue;
                }

                var pool = PoolManager.Instance.GetPool(data.ObjectName);
                if (pool == null)
                {
                    DebugUtility.LogError<PoolTest>($"Pool '{data.ObjectName}' não encontrado.", this);
                    continue;
                }

                anyPoolListed = true;
                var activeObjects = pool.GetActiveObjects();
                var availableCount = pool.GetAvailableCount();
                DebugUtility.Log<PoolTest>($"Pool '{data.ObjectName}': {activeObjects.Count} active objects, {availableCount} available.", "cyan", this);

                if (activeObjects.Count == 0)
                {
                    DebugUtility.Log<PoolTest>($"Nenhum objeto ativo no pool '{data.ObjectName}'.", "yellow", this);
                }
                else
                {
                    foreach (var obj in activeObjects)
                    {
                        if (obj != null && obj.GetGameObject() != null)
                        {
                            var objData = obj.Data;
                            DebugUtility.Log<PoolTest>($"- Active object ID: {obj.GetGameObject().GetInstanceID()}, Position: {obj.GetGameObject().transform.position}, Type: {objData.GetType().Name}", "cyan", this);
                        }
                        else
                        {
                            DebugUtility.LogWarning<PoolTest>($"Objeto nulo encontrado no pool '{data.ObjectName}'.", this);
                        }
                    }
                }
            }

            if (!anyPoolListed)
            {
                DebugUtility.LogWarning<PoolTest>("Nenhum pool válido encontrado para listar.", this);
            }
        }

        private void ExhaustPool()
        {
            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogWarning<PoolTest>("Nenhum PoolData configurado para testar exaustão.", this);
                return;
            }

            var data = poolDataArray[Random.Range(0, poolDataArray.Length)];
            if (data == null)
            {
                DebugUtility.LogError<PoolTest>("PoolData selecionado é nulo.", this);
                return;
            }

            var pool = PoolManager.Instance.GetPool(data.ObjectName);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{data.ObjectName}' não encontrado para teste de exaustão.", this);
                return;
            }

            int attempts = data.InitialPoolSize + 5;
            DebugUtility.Log<PoolTest>($"Testando exaustão do pool '{data.ObjectName}' com {attempts} tentativas.", "yellow", this);
            for (int i = 0; i < attempts; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    0f,
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );
                var poolable = pool.GetObject(position);
                if (poolable != null)
                {
                    DebugUtility.Log<PoolTest>($"Spawned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) for exhaust test.", "green", this);
                }
                else
                {
                    DebugUtility.LogWarning<PoolTest>($"Nenhum objeto disponível no pool '{data.ObjectName}' na tentativa {i + 1}.", this);
                }
            }
        }

        private void OnPoolExhausted(PoolExhaustedEvent evt)
        {
            DebugUtility.LogWarning<PoolTest>($"Pool '{evt.PoolKey}' exausto. Considere aumentar InitialPoolSize ou ativar CanExpand.", this);
        }

        private void OnPoolRestored(PoolRestoredEvent evt)
        {
            DebugUtility.Log<PoolTest>($"Pool '{evt.PoolKey}' restaurado. Objetos disponíveis novamente.", "green", this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.z));
        }
    }
}