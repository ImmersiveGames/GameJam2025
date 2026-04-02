using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.Spawn
{
    /// <summary>
    /// Base que concentra a logica comum de spawn/despawn para os servicos de actor.
    /// Implementacoes concretas devem providenciar apenas os detalhes especificos
    /// (resolver o componente actor, garantir actor id e injecoes de servicos).
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

        /// <summary>
        /// Kind canonico do actor criado por este servico.
        /// </summary>
        public abstract ActorKind SpawnedActorKind { get; }

        /// <summary>
        /// Indica se o actor deste servico deve existir apos o hard reset macro.
        /// </summary>
        public virtual bool IsRequiredForWorldReset => false;

        public Task SpawnAsync()
        {
            DebugUtility.LogVerbose(GetType(),
                $"SpawnAsync iniciado (scene={_context?.SceneName ?? "<unknown>"}).");

            if (uniqueIdFactory == null || _actorRegistry == null)
            {
                DebugUtility.LogError(GetType(),
                    "Dependencias ausentes para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_context?.WorldRoot == null)
            {
                DebugUtility.LogError(GetType(),
                    "WorldSpawnContext invalido para executar SpawnAsync.");
                return Task.CompletedTask;
            }

            if (_prefab == null)
            {
                DebugUtility.LogError(GetType(),
                    "Prefab nao configurado para servico de spawn.");
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

            OnPostInstantiate(_spawnedObject);

            var actor = ResolveActor(_spawnedObject);
            if (actor == null)
            {
                DebugUtility.LogError(GetType(),
                    "Prefab nao contem IActor esperado. Objetos destruidos.");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                return Task.CompletedTask;
            }

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
                    $"Falha ao registrar ator no registry. Destruindo instancia. ActorId={_spawnedActor.ActorId}");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            EventBus<ActorSpawnCompletedEvent>.Raise(
                new ActorSpawnCompletedEvent(
                    _spawnedActor,
                    SpawnedActorKind,
                    _spawnedActor.ActorId,
                    Name,
                    _context.SceneName,
                    IsRequiredForWorldReset));

            string prefabName = _prefab != null ? _prefab.name : "<null>";
            string instanceName = _spawnedObject != null ? _spawnedObject.name : "<null>";
            DebugUtility.Log(GetType(),
                $"Actor spawned: {_spawnedActor.ActorId} (kind={SpawnedActorKind}, requiredForWorldReset={IsRequiredForWorldReset}, prefab={prefabName}, instance={instanceName}, root={_context.WorldRoot?.name}, scene={_context.SceneName})");
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
                    "Dependencias ausentes para executar DespawnAsync.");
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
                $"Actor despawned: {actorId} (kind={SpawnedActorKind}, root={_context?.WorldRoot?.name}, scene={_context?.SceneName})");
            DebugUtility.Log(GetType(), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Resolve o componente IActor na instancia. Deve retornar null se nao houver.
        /// </summary>
        protected abstract IActor ResolveActor(GameObject instance);

        /// <summary>
        /// Garante que o actor possua ActorId valido.
        /// A geracao ocorre aqui, no trilho de Spawn; o actor apenas recebe a identidade.
        /// </summary>
        protected virtual bool EnsureActorId(IActor actor, GameObject instance)
        {
            if (actor == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(actor.ActorId))
            {
                return true;
            }

            string actorLabel = actor is IActorKindProvider kindProvider
                ? kindProvider.Kind.ToString()
                : actor.GetType().Name;

            string actorId = uniqueIdFactory.GenerateId(instance);
            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(GetType(),
                    $"IUniqueIdFactory retornou ActorId vazio; abortando spawn de {actorLabel}.");
                return false;
            }

            actor.Initialize(actorId);

            if (string.IsNullOrWhiteSpace(actor.ActorId))
            {
                DebugUtility.LogError(GetType(),
                    $"Actor nao aceitou ActorId gerado; abortando spawn de {actorLabel}.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Hook chamado logo apos a instanciação do prefab. Usado para garantir stack de movimento, injecao de servicos, etc.
        /// </summary>
        protected virtual void OnPostInstantiate(GameObject instance) { }
    }
}
