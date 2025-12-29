using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Eater.Movement;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn
{
    /// <summary>
    /// Serviço de spawn para instanciar o Eater no baseline do NewScripts.
    /// </summary>
    public sealed class EaterSpawnService : IWorldSpawnService
    {
        private readonly IUniqueIdFactory _uniqueIdFactory;
        private readonly IActorRegistry _actorRegistry;
        private readonly IWorldSpawnContext _context;
        private readonly EaterActor _prefab;
        private readonly IStateDependentService _stateService;

        private IActor _spawnedActor;
        private GameObject _spawnedObject;

        public EaterSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            EaterActor prefab,
            IStateDependentService stateService)
        {
            _uniqueIdFactory = uniqueIdFactory;
            _actorRegistry = actorRegistry;
            _context = context;
            _prefab = prefab;
            _stateService = stateService;
        }

        public string Name => nameof(EaterSpawnService);

        public Task SpawnAsync()
        {
            DebugUtility.LogVerbose(typeof(EaterSpawnService),
                $"SpawnAsync iniciado (scene={_context?.SceneName ?? "<unknown>"}).");

            if (_uniqueIdFactory == null || _actorRegistry == null)
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    "Dependências ausentes para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_context?.WorldRoot == null)
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    "WorldSpawnContext inválido para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_prefab == null)
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    "Prefab não configurado para EaterSpawnService.");
                return Task.CompletedTask;
            }

            if (_spawnedActor != null)
            {
                DebugUtility.LogWarning(typeof(EaterSpawnService), "Spawn chamado mais de uma vez; ignorando.");
                return Task.CompletedTask;
            }

            var instance = Object.Instantiate(_prefab, _context.WorldRoot);
            if (instance == null)
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    "Falha ao instanciar prefab para Eater.");
                return Task.CompletedTask;
            }

            _spawnedObject = instance.gameObject;
            _spawnedObject.name = _prefab.name;
            InjectStateService(_spawnedObject);

            if (!EnsureActorId(instance))
            {
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                return Task.CompletedTask;
            }

            _spawnedActor = instance;

            if (!_actorRegistry.Register(_spawnedActor))
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    $"Falha ao registrar ator no registry. Destruindo instância. ActorId={_spawnedActor.ActorId}");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            string prefabName = _prefab != null ? _prefab.name : "<null>";
            string instanceName = _spawnedObject != null ? _spawnedObject.name : "<null>";
            DebugUtility.Log(typeof(EaterSpawnService),
                $"Actor spawned: {_spawnedActor.ActorId} (prefab={prefabName}, instance={instanceName}, root={_context.WorldRoot?.name}, scene={_context.SceneName})");
            DebugUtility.Log(typeof(EaterSpawnService), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        public Task DespawnAsync()
        {
            DebugUtility.LogVerbose(typeof(EaterSpawnService),
                $"DespawnAsync iniciado (scene={_context?.SceneName ?? "<unknown>"}).");

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    "Dependências ausentes para executar DespawnAsync.");
                return Task.CompletedTask;
            }

            if (_spawnedActor == null)
            {
                DebugUtility.LogVerbose(typeof(EaterSpawnService),
                    "Despawn ignorado (no actor).", "cyan");
                return Task.CompletedTask;
            }

            string actorId = _spawnedActor.ActorId;

            if (!_actorRegistry.Unregister(actorId))
            {
                DebugUtility.LogWarning(typeof(EaterSpawnService),
                    $"Falha ao remover ator do registry. ActorId={actorId}");
            }

            if (_spawnedObject != null)
            {
                Object.Destroy(_spawnedObject);
            }

            _spawnedActor = null;
            _spawnedObject = null;

            DebugUtility.Log(typeof(EaterSpawnService),
                $"Actor despawned: {actorId} (root={_context?.WorldRoot?.name}, scene={_context?.SceneName})");
            DebugUtility.Log(typeof(EaterSpawnService), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        private bool EnsureActorId(EaterActor eater)
        {
            if (eater == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(eater.ActorId))
            {
                return true;
            }

            if (_uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    "IUniqueIdFactory ausente; não é possível gerar ActorId para Eater.");
                return false;
            }

            string actorId = _uniqueIdFactory.GenerateId(eater.gameObject);

            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(typeof(EaterSpawnService),
                    "IUniqueIdFactory retornou ActorId vazio; abortando spawn do Eater.");
                return false;
            }

            eater.Initialize(actorId);
            return true;
        }

        private void InjectStateService(GameObject instance)
        {
            if (_stateService == null || instance == null)
            {
                return;
            }

            if (instance.TryGetComponent(out NewEaterRandomMovementController controller))
            {
                controller.InjectStateService(_stateService);
            }
        }
    }
}
