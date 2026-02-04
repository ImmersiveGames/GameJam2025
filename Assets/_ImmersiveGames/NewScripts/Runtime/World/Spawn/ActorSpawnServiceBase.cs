using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Runtime.Actors;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Runtime.World.Spawn
{
    /// <summary>
    /// Base que concentra a lógica comum de spawn/despawn para os serviços de actor.
    /// Implementações concretas devem providenciar apenas os detalhes específicos
    /// (resolver o componente actor, garantir actor id e injeções de serviços).
    /// </summary>
    public abstract class ActorSpawnServiceBase : IWorldSpawnService
    {
        protected readonly IUniqueIdFactory uniqueIdFactory;
        private readonly IActorRegistry _actorRegistry;
        private readonly IWorldSpawnContext _context;
        private readonly GameObject _prefab;

        private IActor _spawnedActor;
        private GameObject _spawnedObject;

        protected ActorSpawnServiceBase(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            GameObject prefab)
        {
            this.uniqueIdFactory = uniqueIdFactory;
            _actorRegistry = actorRegistry;
            _context = context;
            _prefab = prefab;
        }

        public virtual string Name => GetType().Name;

        public Task SpawnAsync()
        {
            DebugUtility.LogVerbose(GetType(),
                $"SpawnAsync iniciado (scene={_context?.SceneName ?? "<unknown>"}).");

            if (uniqueIdFactory == null || _actorRegistry == null)
            {
                DebugUtility.LogError(GetType(),
                    "Dependências ausentes para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_context?.WorldRoot == null)
            {
                DebugUtility.LogError(GetType(),
                    "WorldSpawnContext inválido para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_prefab == null)
            {
                DebugUtility.LogError(GetType(),
                    "Prefab não configurado para serviço de spawn.");
                return Task.CompletedTask;
            }

            if (_spawnedActor != null)
            {
                DebugUtility.LogWarning(GetType(), "Spawn chamado mais de uma vez; ignorando.");
                return Task.CompletedTask;
            }

            var instance = Object.Instantiate(_prefab, _context.WorldRoot);

            if (instance == null)
            {
                DebugUtility.LogError(GetType(),
                    "Falha ao instanciar prefab para actor.");
                return Task.CompletedTask;
            }

            _spawnedObject = instance;
            _spawnedObject.name = _prefab.name;

            // hook para que implementações façam ajustes (movimento, input, injeções)
            OnPostInstantiate(_spawnedObject);

            var actor = ResolveActor(_spawnedObject);
            if (actor == null)
            {
                DebugUtility.LogError(GetType(),
                    "Prefab não contém IActor esperado. Objetos destruídos.");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                return Task.CompletedTask;
            }

            // garante que o actor possua ActorId válido
            if (!EnsureActorId(actor, _spawnedObject))
            {
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                return Task.CompletedTask;
            }

            _spawnedActor = actor;

            if (!_actorRegistry.Register(_spawnedActor))
            {
                DebugUtility.LogError(GetType(),
                    $"Falha ao registrar ator no registry. Destruindo instância. ActorId={_spawnedActor.ActorId}");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            string prefabName = _prefab != null ? _prefab.name : "<null>";
            string instanceName = _spawnedObject != null ? _spawnedObject.name : "<null>";
            DebugUtility.Log(GetType(),
                $"Actor spawned: {_spawnedActor.ActorId} (prefab={prefabName}, instance={instanceName}, root={_context.WorldRoot?.name}, scene={_context.SceneName})");
            DebugUtility.Log(GetType(), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        public Task DespawnAsync()
        {
            DebugUtility.LogVerbose(GetType(),
                $"DespawnAsync iniciado (scene={_context?.SceneName ?? "<unknown>"}).");

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(GetType(),
                    "Dependências ausentes para executar DespawnAsync.");
                return Task.CompletedTask;
            }

            if (_spawnedActor == null)
            {
                DebugUtility.LogVerbose(GetType(),
                    "Despawn ignorado (no actor).", "cyan");
                return Task.CompletedTask;
            }

            string actorId = _spawnedActor.ActorId;

            if (!_actorRegistry.Unregister(actorId))
            {
                DebugUtility.LogWarning(GetType(),
                    $"Falha ao remover ator do registry. ActorId={actorId}");
            }

            if (_spawnedObject != null)
            {
                Object.Destroy(_spawnedObject);
            }

            _spawnedActor = null;
            _spawnedObject = null;

            DebugUtility.Log(GetType(),
                $"Actor despawned: {actorId} (root={_context?.WorldRoot?.name}, scene={_context?.SceneName})");
            DebugUtility.Log(GetType(), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Resolve o componente IActor na instância. Deve retornar null se não houver.
        /// </summary>
        protected abstract IActor ResolveActor(GameObject instance);

        /// <summary>
        /// Garante que o actor possua ActorId válido. Retorna true se válido/gerado.
        /// Implementações concretas devem usar _uniqueIdFactory quando necessário.
        /// </summary>
        protected virtual bool EnsureActorId(IActor actor, GameObject instance)
        {
            // Default assume que o actor já tem id válido.
            if (actor == null)
            {
                return false;
            }
            return !string.IsNullOrWhiteSpace(actor.ActorId);
        }

        /// <summary>
        /// Hook chamado logo após a instanciação do prefab. Usado para garantir stack de movimento, injeção de serviços, etc.
        /// </summary>
        protected virtual void OnPostInstantiate(GameObject instance) { }

        /// <summary>
        /// Hook para injeção de serviços dependentes de estado.
        /// </summary>
        protected virtual void InjectStateService(GameObject instance) { }
    }
}

