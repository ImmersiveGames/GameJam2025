using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Serviço de spawn que cria um único DummyActor para validar o pipeline.
    /// </summary>
    public sealed class DummyActorSpawnService : IWorldSpawnService
    {
        private readonly IUniqueIdFactory _uniqueIdFactory;
        private readonly IActorRegistry _actorRegistry;
        private readonly IWorldSpawnContext _context;
        private readonly GameObject _prefab;

        private DummyActor _spawnedActor;

        public DummyActorSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            GameObject prefab)
        {
            _uniqueIdFactory = uniqueIdFactory;
            _actorRegistry = actorRegistry;
            _context = context;
            _prefab = prefab;
        }

        public string Name => nameof(DummyActorSpawnService);

        public Task SpawnAsync()
        {
            DebugUtility.LogVerbose(typeof(DummyActorSpawnService),
                $"SpawnAsync iniciado (scene={_context?.SceneName ?? "<unknown>"}).");

            if (_uniqueIdFactory == null || _actorRegistry == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "Dependências ausentes para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_context == null || _context.WorldRoot == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "WorldSpawnContext inválido para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_prefab == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "Prefab não configurado para DummyActorSpawnService.");
                return Task.CompletedTask;
            }

            if (_spawnedActor != null)
            {
                DebugUtility.LogWarning(typeof(DummyActorSpawnService), "Spawn chamado mais de uma vez; ignorando.");
                return Task.CompletedTask;
            }

            var actorGo = Object.Instantiate(_prefab, _context.WorldRoot);

            if (actorGo == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "Falha ao instanciar prefab para DummyActor.");
                return Task.CompletedTask;
            }

            actorGo.name = _prefab.name;

            _spawnedActor = actorGo.GetComponent<DummyActor>();

            if (_spawnedActor == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "Prefab de DummyActor não contém componente DummyActor. Objetos destruídos.");
                Object.Destroy(actorGo);
                return Task.CompletedTask;
            }

            string actorId = _uniqueIdFactory.GenerateId(actorGo);
            _spawnedActor.Initialize(actorId);

            var registered = _actorRegistry.Register(_spawnedActor);

            if (!registered)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    $"Falha ao registrar ator no registry. Destruindo instância. ActorId={actorId}");
                Object.Destroy(actorGo);
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            var prefabName = _prefab != null ? _prefab.name : "<null>";
            var instanceName = actorGo != null ? actorGo.name : "<null>";
            DebugUtility.Log(typeof(DummyActorSpawnService),
                $"Actor spawned: {actorId} (prefab={prefabName}, instance={instanceName}, root={_context.WorldRoot?.name}, scene={_context.SceneName})");
            DebugUtility.Log(typeof(DummyActorSpawnService), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        public Task DespawnAsync()
        {
            DebugUtility.LogVerbose(typeof(DummyActorSpawnService),
                $"DespawnAsync iniciado (scene={_context?.SceneName ?? "<unknown>"}).");

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "Dependências ausentes para executar DespawnAsync.");
                return Task.CompletedTask;
            }

            if (_spawnedActor == null)
            {
                DebugUtility.LogVerbose(typeof(DummyActorSpawnService),
                    "Despawn ignorado (no actor).", "cyan");
                return Task.CompletedTask;
            }

            string actorId = _spawnedActor.ActorId;

            _actorRegistry.Unregister(actorId);
            Object.Destroy(_spawnedActor.gameObject);
            _spawnedActor = null;

            DebugUtility.Log(typeof(DummyActorSpawnService),
                $"Actor despawned: {actorId} (root={_context?.WorldRoot?.name}, scene={_context?.SceneName})");
            DebugUtility.Log(typeof(DummyActorSpawnService), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }
    }
}
