using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.Scripts.Utils;
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
        private readonly GameObject _prefab;

        private DummyActor _spawnedActor;

        public DummyActorSpawnService(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            GameObject prefab)
        {
            _uniqueIdFactory = uniqueIdFactory;
            _actorRegistry = actorRegistry;
            _prefab = prefab;
        }

        public string Name => nameof(DummyActorSpawnService);

        public Task SpawnAsync()
        {
            if (_uniqueIdFactory == null || _actorRegistry == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "Dependências ausentes para executar SpawnAsync.");
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

            var actorGo = Object.Instantiate(_prefab);

            if (actorGo == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "Falha ao instanciar prefab para DummyActor.");
                return Task.CompletedTask;
            }

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

            _actorRegistry.Register(_spawnedActor);

            var prefabName = _prefab != null ? _prefab.name : "<null>";
            DebugUtility.Log(typeof(DummyActorSpawnService), $"Actor spawned: {actorId} (prefab={prefabName})");
            DebugUtility.Log(typeof(DummyActorSpawnService), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        public Task DespawnAsync()
        {
            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(DummyActorSpawnService),
                    "Dependências ausentes para executar DespawnAsync.");
                return Task.CompletedTask;
            }

            if (_spawnedActor == null)
            {
                DebugUtility.LogVerbose(typeof(DummyActorSpawnService),
                    "Despawn ignorado (no actor).", _actorRegistry as Object);
                return Task.CompletedTask;
            }

            string actorId = _spawnedActor.ActorId;

            _actorRegistry.Unregister(actorId);
            Object.Destroy(_spawnedActor.gameObject);
            _spawnedActor = null;

            DebugUtility.Log(typeof(DummyActorSpawnService), $"Actor despawned: {actorId}");
            DebugUtility.Log(typeof(DummyActorSpawnService), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }
    }
}
