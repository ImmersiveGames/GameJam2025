using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using LegacyActor = _ImmersiveGames.Scripts.ActorSystems.IActor;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Serviço de spawn para instanciar o Player real no baseline, substituindo o DummyActor.
    /// </summary>
    public sealed class PlayerSpawnService : IWorldSpawnService
    {
        private readonly IUniqueIdFactory _uniqueIdFactory;
        private readonly IActorRegistry _actorRegistry;
        private readonly IWorldSpawnContext _context;
        private readonly GameObject _prefab;

        private IActor _spawnedActor;
        private GameObject _spawnedObject;

        public PlayerSpawnService(
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

        public string Name => nameof(PlayerSpawnService);

        public Task SpawnAsync()
        {
            DebugUtility.LogVerbose(typeof(PlayerSpawnService),
                $"SpawnAsync iniciado (scene={_context?.SceneName ?? "<unknown>"}).");

            if (_uniqueIdFactory == null || _actorRegistry == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "Dependências ausentes para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_context?.WorldRoot == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "WorldSpawnContext inválido para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_prefab == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "Prefab não configurado para PlayerSpawnService.");
                return Task.CompletedTask;
            }

            if (_spawnedActor != null)
            {
                DebugUtility.LogWarning(typeof(PlayerSpawnService), "Spawn chamado mais de uma vez; ignorando.");
                return Task.CompletedTask;
            }

            _spawnedObject = Object.Instantiate(_prefab, _context.WorldRoot);

            if (_spawnedObject == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "Falha ao instanciar prefab para Player.");
                return Task.CompletedTask;
            }

            _spawnedObject.name = _prefab.name;

            if (!TryResolveActor(_spawnedObject, out _spawnedActor))
            {
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                return Task.CompletedTask;
            }

            if (!_actorRegistry.Register(_spawnedActor))
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    $"Falha ao registrar ator no registry. Destruindo instância. ActorId={_spawnedActor.ActorId}");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            var prefabName = _prefab != null ? _prefab.name : "<null>";
            var instanceName = _spawnedObject != null ? _spawnedObject.name : "<null>";
            DebugUtility.Log(typeof(PlayerSpawnService),
                $"Actor spawned: {_spawnedActor.ActorId} (prefab={prefabName}, instance={instanceName}, root={_context.WorldRoot?.name}, scene={_context.SceneName})");
            DebugUtility.Log(typeof(PlayerSpawnService), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        public Task DespawnAsync()
        {
            DebugUtility.LogVerbose(typeof(PlayerSpawnService),
                $"DespawnAsync iniciado (scene={_context?.SceneName ?? "<unknown>"}).");

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "Dependências ausentes para executar DespawnAsync.");
                return Task.CompletedTask;
            }

            if (_spawnedActor == null)
            {
                DebugUtility.LogVerbose(typeof(PlayerSpawnService),
                    "Despawn ignorado (no actor).", "cyan");
                return Task.CompletedTask;
            }

            var actorId = _spawnedActor.ActorId;

            if (!_actorRegistry.Unregister(actorId))
            {
                DebugUtility.LogWarning(typeof(PlayerSpawnService),
                    $"Falha ao remover ator do registry. ActorId={actorId}");
            }

            if (_spawnedObject != null)
            {
                Object.Destroy(_spawnedObject);
            }

            _spawnedActor = null;
            _spawnedObject = null;

            DebugUtility.Log(typeof(PlayerSpawnService),
                $"Actor despawned: {actorId} (root={_context?.WorldRoot?.name}, scene={_context?.SceneName})");
            DebugUtility.Log(typeof(PlayerSpawnService), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        private bool TryResolveActor(GameObject instance, out IActor actor)
        {
            actor = instance.GetComponent<IActor>();

            if (actor == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "Prefab não possui componente IActor. Player não será instanciado.");
                return false;
            }

            if (!EnsureActorId(instance, actor))
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "ActorId inválido para Player. Instância será destruída.");
                actor = null;
                return false;
            }

            return true;
        }

        private bool EnsureActorId(GameObject instance, IActor actor)
        {
            if (actor == null)
            {
                return false;
            }

            if (actor is PlayerActor playerActor && !string.IsNullOrWhiteSpace(playerActor.ActorId))
            {
                playerActor.Initialize(playerActor.ActorId);
                return true;
            }

            if (!string.IsNullOrWhiteSpace(actor.ActorId))
            {
                return true;
            }

            var legacyActor = instance.GetComponent<LegacyActor>();
            var actorId = legacyActor?.ActorId;

            if (string.IsNullOrWhiteSpace(actorId))
            {
                actorId = _uniqueIdFactory?.GenerateId(instance);
            }

            if (string.IsNullOrWhiteSpace(actorId))
            {
                return false;
            }

            if (actor is PlayerActor player)
            {
                player.Initialize(actorId);
                return true;
            }

            DebugUtility.LogWarning(typeof(PlayerSpawnService),
                $"ActorId não pôde ser aplicado em '{actor.GetType().Name}'. Valor calculado: '{actorId}'.");
            return !string.IsNullOrWhiteSpace(actor.ActorId);
        }
    }
}
