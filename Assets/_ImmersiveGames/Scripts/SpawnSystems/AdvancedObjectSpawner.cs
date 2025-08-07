using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class AdvancedObjectSpawner : ObjectSpawner
    {
        [System.Serializable]
        public class SpawnAction
        {
            [SerializeField] public KeyCode keyCode;
            [SerializeField] public SpawnStrategyConfig strategyConfig;
            [SerializeField, Min(1)] public int spawnCount = 1;
            [SerializeField] public Transform target;
            [HideInInspector] public ISpawnStrategyInstance strategyInstance;
        }

        [SerializeField] private SpawnAction[] spawnActions;
        private List<(IPoolable poolable, string poolKey)> _spawnedObjects;

        protected override void Awake()
        {
            base.Awake();
            _spawnedObjects = new List<(IPoolable, string)>();

            foreach (var action in spawnActions)
            {
                if (action.strategyConfig != null)
                {
                    action.strategyInstance = action.strategyConfig.CreateInstance();
                    if (action.strategyInstance == null)
                    {
                        DebugUtility.LogError<AdvancedObjectSpawner>($"Falha ao criar instância para strategyConfig '{action.strategyConfig.name}' na ação com tecla '{action.keyCode}'.", this);
                    }
                    else if (action.target != null)
                    {
                        action.strategyInstance.SetTarget(action.target);
                    }
                }
                else
                {
                    DebugUtility.LogWarning<AdvancedObjectSpawner>($"Nenhuma estratégia configurada para a ação com tecla '{action.keyCode}'.", this);
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            foreach (var action in spawnActions)
            {
                if (action.strategyInstance != null && Input.GetKeyDown(action.keyCode))
                {
                    StartCoroutine(SpawnWithStrategy(action.strategyInstance, action.spawnCount));
                }
            }
        }

        public void SetSpawnTarget(int spawnActionIndex, Transform target)
        {
            if (spawnActionIndex < 0 || spawnActionIndex >= spawnActions.Length)
            {
                DebugUtility.LogError<AdvancedObjectSpawner>($"Índice de SpawnAction inválido: {spawnActionIndex}.", this);
                return;
            }

            var action = spawnActions[spawnActionIndex];
            if (action.strategyInstance == null)
            {
                DebugUtility.LogWarning<AdvancedObjectSpawner>($"Nenhuma instância de estratégia para a ação com tecla '{action.keyCode}'.", this);
                return;
            }

            action.target = target;
            action.strategyInstance.SetTarget(target);
            DebugUtility.Log<AdvancedObjectSpawner>($"Alvo atualizado para ação com tecla '{action.keyCode}' para {target?.name ?? "null"}.", "green", this);
        }

        public void SetSpawnPosition(int spawnActionIndex, Vector3 position)
        {
            if (spawnActionIndex < 0 || spawnActionIndex >= spawnActions.Length)
            {
                DebugUtility.LogError<AdvancedObjectSpawner>($"Índice de SpawnAction inválido: {spawnActionIndex}.", this);
                return;
            }

            var action = spawnActions[spawnActionIndex];
            if (action.strategyInstance == null)
            {
                DebugUtility.LogWarning<AdvancedObjectSpawner>($"Nenhuma instância de estratégia para a ação com tecla '{action.keyCode}'.", this);
                return;
            }

            action.target = null;
            action.strategyInstance.SetPosition(position);
            DebugUtility.Log<AdvancedObjectSpawner>($"Posição atualizada para ação com tecla '{action.keyCode}' para {position}.", "green", this);
        }

        public IReadOnlyCollection<IPoolable> GetActiveSpawnedObjects()
        {
            return _spawnedObjects
                .Where(x => x.poolable != null && x.poolable.GetGameObject() != null && x.poolable.GetGameObject().activeSelf)
                .Select(x => x.poolable)
                .ToList()
                .AsReadOnly();
        }

        public override void SpawnObject()
        {
            if (spawnActions.Length > 0 && spawnActions[0].strategyInstance != null)
            {
                StartCoroutine(SpawnWithStrategy(spawnActions[0].strategyInstance, spawnActions[0].spawnCount));
            }
            else
            {
                base.SpawnObject();
            }
        }

        public override void SpawnMultipleObjects(int count)
        {
            if (spawnActions.Length > 0 && spawnActions[0].strategyInstance != null)
            {
                StartCoroutine(SpawnWithStrategy(spawnActions[0].strategyInstance, count));
            }
            else
            {
                base.SpawnMultipleObjects(count);
            }
        }

        private IEnumerator SpawnWithStrategy(ISpawnStrategyInstance strategy, int count)
        {
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogWarning<AdvancedObjectSpawner>("Nenhum pool válido configurado para spawn.", this);
                yield break;
            }

            if (strategy == null)
            {
                DebugUtility.LogWarning<AdvancedObjectSpawner>("Estratégia de spawn não configurada.", this);
                yield break;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            var pool = _poolManager.GetPool(poolKey);
            if (pool == null || !pool.IsInitialized)
            {
                DebugUtility.LogError<AdvancedObjectSpawner>($"Pool '{poolKey}' não encontrado ou não inicializado.", this);
                _validPoolKeys.Remove(poolKey);
                yield break;
            }

            var positions = strategy.GetSpawnPositions(count).ToList();
            for (int i = 0; i < positions.Count; i++)
            {
                var (position, rotation) = positions[i];
                float delay = strategy.GetSpawnDelay(i);
                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }

                var poolable = pool.GetObject(position);
                if (poolable != null)
                {
                    poolable.GetGameObject().transform.SetParent(null);
                    poolable.GetGameObject().transform.rotation = rotation;
                    if (!poolable.GetGameObject().activeSelf)
                    {
                        DebugUtility.LogWarning<AdvancedObjectSpawner>($"Objeto '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) spawnado, mas não está ativo. Forçando ativação.", this);
                        poolable.Activate(position, null);
                    }
                    _spawnedObjects.Add((poolable, poolKey));
                    DebugUtility.Log<AdvancedObjectSpawner>($"Spawned object '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) at position {position}, Active: {poolable.GetGameObject().activeSelf}, Parent: {(poolable.GetGameObject().transform.parent != null ? poolable.GetGameObject().transform.parent.name : "None")} using strategy '{strategy.StrategyName}'.", "green", this);
                }
                else
                {
                    DebugUtility.LogWarning<AdvancedObjectSpawner>($"Falha ao obter objeto do pool '{poolKey}' usando estratégia '{strategy.StrategyName}'. Pool pode estar exausto.", this);
                }
            }
        }

        public override void ReturnAllObjects()
        {
            bool anyActiveObjects = false;
            foreach (var (poolable, poolKey) in _spawnedObjects.ToList())
            {
                if (poolable == null || poolable.GetGameObject() == null)
                {
                    _spawnedObjects.Remove((poolable, poolKey));
                    DebugUtility.LogWarning<AdvancedObjectSpawner>($"Objeto nulo encontrado na lista de spawnados para pool '{poolKey}'. Removido da lista.", this);
                    continue;
                }

                if (poolable.GetGameObject().activeSelf)
                {
                    var pool = _poolManager.GetPool(poolKey);
                    if (pool == null || !pool.IsInitialized)
                    {
                        DebugUtility.LogError<AdvancedObjectSpawner>($"Pool '{poolKey}' não encontrado ou não inicializado.", this);
                        _validPoolKeys.Remove(poolKey);
                        continue;
                    }

                    pool.ReturnObject(poolable);
                    DebugUtility.Log<AdvancedObjectSpawner>($"Returned object '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) to pool, Parent: {pool.gameObject.name}.", "blue", this);
                    anyActiveObjects = true;
                }
            }

            _spawnedObjects.RemoveAll(x => x.poolable == null || x.poolable.GetGameObject() == null || !x.poolable.GetGameObject().activeSelf);

            if (!anyActiveObjects)
            {
                DebugUtility.Log<AdvancedObjectSpawner>("Nenhum objeto ativo encontrado na lista de spawnados.", "yellow", this);
            }

            base.ReturnAllObjects();
        }

        public override void ListActiveObjects()
        {
            base.ListActiveObjects();

            DebugUtility.Log<AdvancedObjectSpawner>($"Objetos ativos spawnados por este spawner ({_spawnedObjects.Count(x => x.poolable != null && x.poolable.GetGameObject() != null && x.poolable.GetGameObject().activeSelf)}):", "cyan", this);
            foreach (var (poolable, poolKey) in _spawnedObjects)
            {
                if (poolable != null && poolable.GetGameObject() != null && poolable.GetGameObject().activeSelf)
                {
                    DebugUtility.Log<AdvancedObjectSpawner>($"- Spawned object ID: {poolable.GetGameObject().GetInstanceID()}, Pool: {poolKey}, Position: {poolable.GetGameObject().transform.position}, Active: {poolable.GetGameObject().activeSelf}, Parent: {(poolable.GetGameObject().transform.parent != null ? poolable.GetGameObject().transform.parent.name : "None")}", "cyan", this);
                }
            }
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = Color.green;
            foreach (var action in spawnActions)
            {
                if (action.strategyInstance == null) continue;

                var positions = action.strategyInstance.GetSpawnPositions(action.spawnCount);
                foreach (var pos in positions)
                {
                    Gizmos.DrawWireSphere(pos.position, 0.5f);
                }
            }
        }
    }
}