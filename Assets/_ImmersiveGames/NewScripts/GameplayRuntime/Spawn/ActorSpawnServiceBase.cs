using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using UnityEngine;
using Object = UnityEngine.Object;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
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

        public Task SpawnAsync(ActorSpawnRequest request)
        {
            DebugUtility.LogVerbose(GetType(),
                $"SpawnAsync iniciado request='{request}' scene={_context?.SceneName ?? "<unknown>"}.");

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

            ValidateCanonicalRequestOrFail(request);

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

            RuntimeActorId runtimeActorId = new RuntimeActorId(_spawnedActor.ActorId);
            if (!runtimeActorId.IsValid)
            {
                DebugUtility.LogError(GetType(),
                    $"ActorId gerado invalido para runtime canonical; abortando spawn. ActorId={_spawnedActor.ActorId}");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            AxisActorId axisActorId = request.HasAxisActorId ? request.AxisActorId : AxisActorId.None;
            if (request.IsValid && request.HasAxisActorId && !axisActorId.IsValid)
            {
                DebugUtility.LogError(GetType(),
                    $"AxisActorId invalido no request canonico; abortando spawn. request='{request}'");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            string semanticParticipantId = ResolveSemanticParticipantId(_spawnedActor, request);
            ActorSpawnCompletedEvent completedEvent = new ActorSpawnCompletedEvent(
                _spawnedActor,
                SpawnedActorKind,
                axisActorId,
                runtimeActorId,
                _spawnedActor.ActorId,
                request.ActorSpecId,
                request.ActorSetRef,
                semanticParticipantId,
                request.OperationalRecipeKind,
                Name,
                _context.SceneName,
                request.Source,
                request.Reason,
                request.ExecutionSignature,
                IsRequiredForWorldReset);

            if (request.HasActorSpecId && string.IsNullOrWhiteSpace(completedEvent.ActorSpecId))
            {
                DebugUtility.LogError(GetType(),
                    $"ActorSpawnCompletedEvent canonico sem ActorSpecId apos spawn. request='{request}'");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            if (request.HasActorSetRef && string.IsNullOrWhiteSpace(completedEvent.ActorSetRef))
            {
                DebugUtility.LogError(GetType(),
                    $"ActorSpawnCompletedEvent canonico sem ActorSetRef apos spawn. request='{request}'");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            if (request.HasAxisActorId && !completedEvent.HasAxisActorId)
            {
                DebugUtility.LogError(GetType(),
                    $"ActorSpawnCompletedEvent canonico sem AxisActorId apos spawn. request='{request}'");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            if (request.HasSemanticParticipantId && !completedEvent.HasSemanticParticipantId)
            {
                DebugUtility.LogError(GetType(),
                    $"ActorSpawnCompletedEvent canonico sem SemanticParticipantId apos spawn. request='{request}'");
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            EventBus<ActorSpawnCompletedEvent>.Raise(
                completedEvent);

            DebugUtility.Log(GetType(),
                $"[OBS][Gameplay][SpawnBridge] ActorSpawnCompletedEvent published actorSpecId='{completedEvent.ActorSpecId}' actorSetRef='{completedEvent.ActorSetRef}' axisActorId='{completedEvent.AxisActorId}' runtimeActorId='{completedEvent.RuntimeActorId}' semanticParticipantId='{AsText(completedEvent.SemanticParticipantId)}' source='{AsText(completedEvent.Source)}' executionSignature='{AsText(completedEvent.ExecutionSignature)}'.",
                DebugUtility.Colors.Info);

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
        protected virtual string ResolveSemanticParticipantId(IActor actor, in ActorSpawnRequest request)
        {
            _ = actor;
            if (request.HasSemanticParticipantId)
            {
                return request.SemanticParticipantId;
            }

            return string.Empty;
        }

        private void ValidateCanonicalRequestOrFail(in ActorSpawnRequest request)
        {
            if (!request.IsValid)
            {
                HardFailFastH1.Trigger(GetType(),
                    "[FATAL][H1][Spawn] ActorSpawnRequest invalido recebido pelo servico de spawn.");
            }

            if (request.ActorKind != SpawnedActorKind)
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest com actorKind divergente recebido pelo servico de spawn. requestActorKind='{request.ActorKind}' serviceActorKind='{SpawnedActorKind}' request='{request}'.");
            }

            if (!request.HasAxisActorId || !request.HasActorSpecId || !request.HasActorSetRef)
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest canonico incompleto; axisActorId='{request.AxisActorId}' actorSpecId='{request.ActorSpecId}' actorSetRef='{request.ActorSetRef}' actorKind='{request.ActorKind}'.");
            }

            if (SpawnedActorKind == ActorKind.Player && !request.HasSemanticParticipantId)
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest canonico de Player sem SemanticParticipantId. axisActorId='{request.AxisActorId}' actorSpecId='{request.ActorSpecId}' actorSetRef='{request.ActorSetRef}' source='{request.Source}'.");
            }
        }

        protected virtual void OnPostInstantiate(GameObject instance) { }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}

