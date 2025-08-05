using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SpawnSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray; // Array de dados para criar pools
        [SerializeField] private float spawnInterval = 1f; // Intervalo entre spawns automáticos
        [SerializeField] private KeyCode spawnKey = KeyCode.Space; // Tecla para spawn manual
        [SerializeField] private KeyCode returnAllKey = KeyCode.R; // Tecla para retornar todos os objetos
        [SerializeField] private KeyCode listActiveKey = KeyCode.L; // Tecla para listar objetos ativos
        [SerializeField] private KeyCode exhaustPoolKey = KeyCode.E; // Tecla para testar exaustão do pool
        [SerializeField] private KeyCode spawnMultipleKey = KeyCode.M; // Tecla para spawnar múltiplos objetos
        [SerializeField, Min(1)] private int multipleSpawnCount = 3; // Quantidade de objetos a spawnar com spawn múltiplo
        [SerializeField] private Vector3 spawnAreaSize = Vector3.zero; // Área XZ para spawn (0, 0, 0) se zero
        [SerializeField] private bool enableAutoSpawn = true; // Controla se o spawn automático está ativo

        private PoolManager _poolManager;
        private float _timer;
        private EventBinding<PoolExhaustedEvent> _poolExhaustedBinding;
        private EventBinding<PoolRestoredEvent> _poolRestoredBinding;
        private List<string> _validPoolKeys; // Lista de chaves de pools válidos

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

        private void Awake()
        {
            _validPoolKeys = new List<string>();

            // Obter referência ao PoolManager
            _poolManager = PoolManager.Instance;
            if (_poolManager == null)
            {
                DebugUtility.LogError<ObjectSpawner>("PoolManager não encontrado na cena. Certifique-se de que está inicializado.", this);
                enabled = false;
                return;
            }

            // Verificar se LifetimeManager está presente
            if (LifetimeManager.Instance == null)
            {
                DebugUtility.LogError<ObjectSpawner>("LifetimeManager não encontrado na cena. Adicione um GameObject com LifetimeManager.", this);
                enabled = false;
                return;
            }

            // Verificar se poolDataArray está configurado
            if (poolDataArray == null || poolDataArray.Length == 0)
            {
                DebugUtility.LogError<ObjectSpawner>("poolDataArray está vazio ou não configurado.", this);
                enabled = false;
                return;
            }

            // Registrar pools no PoolManager
            foreach (var data in poolDataArray)
            {
                if (data == null)
                {
                    DebugUtility.LogError<ObjectSpawner>("PoolData nulo encontrado em poolDataArray.", this);
                    continue;
                }

                // Registrar mesmo se a validação falhar, para suportar ProjectilesData
                _poolManager.RegisterPool(data);
                var pool = _poolManager.GetPool(data.ObjectName);
                if (pool != null && pool.IsInitialized)
                {
                    _validPoolKeys.Add(data.ObjectName);
                    DebugUtility.Log<ObjectSpawner>($"Registrado pool '{data.ObjectName}' com tamanho inicial {data.InitialPoolSize}.", "green", this);
                }
                else
                {
                    DebugUtility.LogWarning<ObjectSpawner>($"Pool '{data.ObjectName}' não inicializado após registro, possivelmente devido a validação de SkinPoolableObjectData.", this);
                }
            }

            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<ObjectSpawner>("Nenhum pool válido registrado. Desativando ObjectSpawner.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Spawn automático a cada intervalo, se ativado
            if (enableAutoSpawn && _validPoolKeys.Count > 0)
            {
                _timer += Time.deltaTime;
                if (_timer >= spawnInterval)
                {
                    SpawnObject();
                    _timer = 0f;
                }
            }

            // Spawn manual com tecla
            if (Input.GetKeyDown(spawnKey))
            {
                SpawnObject();
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
                SpawnMultipleObjects(multipleSpawnCount);
            }
        }

        public void SpawnObject()
        {
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogWarning<ObjectSpawner>("Nenhum pool válido configurado para spawn.", this);
                return;
            }

            // Selecionar um pool aleatoriamente
            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            var pool = _poolManager.GetPool(poolKey);
            if (pool == null || !pool.IsInitialized)
            {
                DebugUtility.LogError<ObjectSpawner>($"Pool '{poolKey}' não encontrado ou não inicializado.", this);
                _validPoolKeys.Remove(poolKey); // Remover pool inválido
                return;
            }

            // Usar posição (0, 0, 0) ou área aleatória
            Vector3 position = spawnAreaSize == Vector3.zero
                ? Vector3.zero
                : new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    0f,
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );

            var poolable = pool.GetObject(position);
            if (poolable != null)
            {
                // Garantir que o objeto não tenha parent (evitar interferência do SkinModelBuild)
                poolable.GetGameObject().transform.SetParent(null);
                // Verificar visibilidade
                if (!poolable.GetGameObject().activeSelf)
                {
                    DebugUtility.LogWarning<ObjectSpawner>($"Objeto '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) spawnado, mas não está ativo. Forçando ativação.", this);
                    poolable.Activate(position, null);
                }
                DebugUtility.Log<ObjectSpawner>($"Spawned object '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) at position {position}, Active: {poolable.GetGameObject().activeSelf}, Parent: {(poolable.GetGameObject().transform.parent != null ? poolable.GetGameObject().transform.parent.name : "None")}.", "green", this);
            }
            else
            {
                DebugUtility.LogWarning<ObjectSpawner>($"Falha ao obter objeto do pool '{poolKey}'. Pool pode estar exausto.", this);
            }
        }

        public void SpawnMultipleObjects(int count)
        {
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogWarning<ObjectSpawner>("Nenhum pool válido configurado para spawn múltiplo.", this);
                return;
            }

            // Selecionar um pool aleatoriamente
            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            var pool = _poolManager.GetPool(poolKey);
            if (pool == null || !pool.IsInitialized)
            {
                DebugUtility.LogError<ObjectSpawner>($"Pool '{poolKey}' não encontrado ou não inicializado para spawn múltiplo.", this);
                _validPoolKeys.Remove(poolKey); // Remover pool inválido
                return;
            }

            // Usar posição (0, 0, 0) ou área aleatória como base
            Vector3 basePosition = spawnAreaSize == Vector3.zero
                ? Vector3.zero
                : new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    0f,
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );

            var poolables = pool.GetMultipleObjects(count, basePosition);
            DebugUtility.Log<ObjectSpawner>($"Tentativa de spawn múltiplo: Solicitados {count}, obtidos {poolables.Count} objetos do pool '{poolKey}'.", "green", this);

            // Ajustar posições em um círculo
            for (int i = 0; i < poolables.Count; i++)
            {
                if (poolables[i] != null && poolables[i].GetGameObject() != null)
                {
                    float angle = i * (360f / poolables.Count);
                    Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * 1f;
                    Vector3 position = basePosition + offset;
                    poolables[i].GetGameObject().transform.position = position;
                    poolables[i].GetGameObject().transform.SetParent(null); // Garantir que não tenha parent
                    if (!poolables[i].GetGameObject().activeSelf)
                    {
                        DebugUtility.LogWarning<ObjectSpawner>($"Objeto '{poolKey}' (ID: {poolables[i].GetGameObject().GetInstanceID()}) spawnado, mas não está ativo. Forçando ativação.", this);
                        poolables[i].Activate(position, null);
                    }
                    DebugUtility.Log<ObjectSpawner>($"Spawned object '{poolKey}' (ID: {poolables[i].GetGameObject().GetInstanceID()}) at position {position}, Active: {poolables[i].GetGameObject().activeSelf}, Parent: {(poolables[i].GetGameObject().transform.parent != null ? poolables[i].GetGameObject().transform.parent.name : "None")}.", "green", this);
                }
            }
        }

        public void ReturnAllObjects()
        {
            bool anyActiveObjects = false;
            foreach (var poolKey in _validPoolKeys.ToList())
            {
                var pool = _poolManager.GetPool(poolKey);
                if (pool == null || !pool.IsInitialized)
                {
                    DebugUtility.LogError<ObjectSpawner>($"Pool '{poolKey}' não encontrado ou não inicializado.", this);
                    _validPoolKeys.Remove(poolKey); // Remover pool inválido
                    continue;
                }

                var activeObjects = pool.GetActiveObjects().ToList();
                if (activeObjects.Count == 0)
                {
                    DebugUtility.Log<ObjectSpawner>($"Nenhum objeto ativo no pool '{poolKey}'.", "yellow", this);
                }
                else
                {
                    anyActiveObjects = true;
                    foreach (var obj in activeObjects)
                    {
                        if (obj != null && obj.GetGameObject() != null)
                        {
                            obj.Deactivate();
                            DebugUtility.Log<ObjectSpawner>($"Returned object '{poolKey}' (ID: {obj.GetGameObject().GetInstanceID()}) to pool.", "blue", this);
                        }
                        else
                        {
                            DebugUtility.LogWarning<ObjectSpawner>($"Objeto nulo encontrado no pool '{poolKey}'.", this);
                        }
                    }
                }
            }

            if (!anyActiveObjects)
            {
                DebugUtility.Log<ObjectSpawner>("Nenhum objeto ativo encontrado em nenhum pool.", "yellow", this);
            }
        }

        public void ListActiveObjects()
        {
            DebugUtility.Log<ObjectSpawner>("Listando objetos ativos e disponíveis:", "cyan", this);

            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogWarning<ObjectSpawner>("Nenhum pool válido configurado para listar.", this);
                return;
            }

            bool anyPoolListed = false;
            foreach (var poolKey in _validPoolKeys.ToList())
            {
                var pool = _poolManager.GetPool(poolKey);
                if (pool == null || !pool.IsInitialized)
                {
                    DebugUtility.LogError<ObjectSpawner>($"Pool '{poolKey}' não encontrado ou não inicializado.", this);
                    _validPoolKeys.Remove(poolKey); // Remover pool inválido
                    continue;
                }

                anyPoolListed = true;
                var activeObjects = pool.GetActiveObjects();
                var availableCount = pool.GetAvailableCount();
                DebugUtility.Log<ObjectSpawner>($"Pool '{poolKey}': {activeObjects.Count} active objects, {availableCount} available.", "cyan", this);

                if (activeObjects.Count == 0)
                {
                    DebugUtility.Log<ObjectSpawner>($"Nenhum objeto ativo no pool '{poolKey}'.", "yellow", this);
                }
                else
                {
                    foreach (var obj in activeObjects)
                    {
                        if (obj != null && obj.GetGameObject() != null)
                        {
                            var objData = obj.Data;
                            DebugUtility.Log<ObjectSpawner>($"- Active object ID: {obj.GetGameObject().GetInstanceID()}, Position: {obj.GetGameObject().transform.position}, Type: {objData.GetType().Name}, Active: {obj.GetGameObject().activeSelf}, Parent: {(obj.GetGameObject().transform.parent != null ? obj.GetGameObject().transform.parent.name : "None")}", "cyan", this);
                        }
                        else
                        {
                            DebugUtility.LogWarning<ObjectSpawner>($"Objeto nulo encontrado no pool '{poolKey}'.", this);
                        }
                    }
                }
            }

            if (!anyPoolListed)
            {
                DebugUtility.LogWarning<ObjectSpawner>("Nenhum pool válido encontrado para listar.", this);
            }
        }

        public void ExhaustPool()
        {
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogWarning<ObjectSpawner>("Nenhum pool válido configurado para testar exaustão.", this);
                return;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            var pool = _poolManager.GetPool(poolKey);
            if (pool == null || !pool.IsInitialized)
            {
                DebugUtility.LogError<ObjectSpawner>($"Pool '{poolKey}' não encontrado ou não inicializado para teste de exaustão.", this);
                _validPoolKeys.Remove(poolKey); // Remover pool inválido
                return;
            }

            var data = poolDataArray.FirstOrDefault(d => d.ObjectName == poolKey);
            if (data == null)
            {
                DebugUtility.LogError<ObjectSpawner>($"PoolData para '{poolKey}' não encontrado.", this);
                return;
            }

            int attempts = data.InitialPoolSize + 5;
            DebugUtility.Log<ObjectSpawner>($"Testando exaustão do pool '{poolKey}' com {attempts} tentativas.", "yellow", this);
            for (int i = 0; i < attempts; i++)
            {
                Vector3 position = spawnAreaSize == Vector3.zero
                    ? Vector3.zero
                    : new Vector3(
                        Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                        0f,
                        Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                    );
                var poolable = pool.GetObject(position);
                if (poolable != null)
                {
                    poolable.GetGameObject().transform.SetParent(null); // Garantir que não tenha parent
                    if (!poolable.GetGameObject().activeSelf)
                    {
                        DebugUtility.LogWarning<ObjectSpawner>($"Objeto '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) spawnado, mas não está ativo. Forçando ativação.", this);
                        poolable.Activate(position, null);
                    }
                    DebugUtility.Log<ObjectSpawner>($"Spawned object '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) for exhaust test, Active: {poolable.GetGameObject().activeSelf}, Parent: {(poolable.GetGameObject().transform.parent != null ? poolable.GetGameObject().transform.parent.name : "None")}.", "green", this);
                }
                else
                {
                    DebugUtility.LogWarning<ObjectSpawner>($"Nenhum objeto disponível no pool '{poolKey}' na tentativa {i + 1}.", this);
                }
            }
        }

        private void OnPoolExhausted(PoolExhaustedEvent evt)
        {
            DebugUtility.LogWarning<ObjectSpawner>($"Pool '{evt.PoolKey}' exausto. Considere aumentar InitialPoolSize ou ativar CanExpand.", this);
        }

        private void OnPoolRestored(PoolRestoredEvent evt)
        {
            DebugUtility.Log<ObjectSpawner>($"Pool '{evt.PoolKey}' restaurado. Objetos disponíveis novamente.", "green", this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.z));
        }
    }
}