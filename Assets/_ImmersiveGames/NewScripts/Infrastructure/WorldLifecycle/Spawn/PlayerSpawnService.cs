using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Player.Movement;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn
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
            EnsureMovementStack(_spawnedObject);

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

            string prefabName = _prefab != null ? _prefab.name : "<null>";
            string instanceName = _spawnedObject != null ? _spawnedObject.name : "<null>";
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

            string actorId = _spawnedActor.ActorId;

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
            actor = null;

            if (instance == null)
            {
                return false;
            }

            if (instance.TryGetComponent(out PlayerActor playerActor))
            {
                if (!EnsureActorId(instance, playerActor))
                {
                    DebugUtility.LogError(typeof(PlayerSpawnService),
                        "ActorId inválido para PlayerActor. Instância será destruída.");
                    return false;
                }

                actor = playerActor;
                return true;
            }

            if (instance.TryGetComponent(out PlayerActorAdapter adapter))
            {
                if (!EnsureActorId(adapter, instance))
                {
                    DebugUtility.LogError(typeof(PlayerSpawnService),
                        "ActorId inválido para PlayerActorAdapter. Instância será destruída.");
                    return false;
                }

                actor = adapter;
                return true;
            }

            if (instance.TryGetComponent(out IActor existingActor) && existingActor != null)
            {
                if (string.IsNullOrWhiteSpace(existingActor.ActorId))
                {
                    DebugUtility.LogError(typeof(PlayerSpawnService),
                        "IActor encontrado no prefab, mas ActorId está vazio. Instância será destruída.");
                    return false;
                }

                actor = existingActor;
                return true;
            }

            DebugUtility.LogWarning(typeof(PlayerSpawnService),
                "Prefab sem IActor. Adicionando PlayerActor como fallback.");

            var fallbackActor = instance.AddComponent<PlayerActor>();
            if (!EnsureActorId(instance, fallbackActor))
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "Falha ao criar ActorId para PlayerActor fallback. Instância será destruída.");
                return false;
            }

            actor = fallbackActor;
            return true;
        }

        private bool EnsureActorId(GameObject instance, PlayerActor player)
        {
            if (player == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(player.ActorId))
            {
                return true;
            }

            if (_uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "IUniqueIdFactory ausente; não é possível gerar ActorId para Player.");
                return false;
            }

            string actorId = _uniqueIdFactory.GenerateId(instance);

            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "IUniqueIdFactory retornou ActorId vazio; abortando spawn do Player.");
                return false;
            }

            player.Initialize(actorId);
            return true;
        }

        private bool EnsureActorId(PlayerActorAdapter adapter, GameObject instance)
        {
            if (adapter == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(adapter.ActorId))
            {
                return true;
            }

            if (_uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "IUniqueIdFactory ausente; não é possível gerar ActorId para PlayerActorAdapter.");
                return false;
            }

            string actorId = _uniqueIdFactory.GenerateId(instance);

            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(typeof(PlayerSpawnService),
                    "IUniqueIdFactory retornou ActorId vazio; abortando spawn do PlayerActorAdapter.");
                return false;
            }

            adapter.Initialize(actorId);
            return true;
        }

        private static void EnsureMovementStack(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var input = instance.GetComponent<NewPlayerInputReader>() ?? instance.AddComponent<NewPlayerInputReader>();
            var controller = instance.GetComponent<NewPlayerMovementController>() ?? instance.AddComponent<NewPlayerMovementController>();

            if (controller != null && input != null)
            {
                controller.SetInputReader(input);
            }
        }
    }
}
