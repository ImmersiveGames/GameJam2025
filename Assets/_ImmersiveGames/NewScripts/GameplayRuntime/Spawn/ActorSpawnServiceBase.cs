using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
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
        private readonly ActorSpecRecord _actorSpec;
        private readonly GameObject _prefab;

        private IActor _spawnedActor;
        private GameObject _spawnedObject;

        protected ActorSpawnServiceBase(
            IUniqueIdFactory uniqueIdFactory,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context,
            ActorSpecRecord actorSpec,
            GameObject prefab)
        {
            if (!actorSpec.IsValid)
            {
                HardFailFastH1.Trigger(GetType(),
                    "[FATAL][Config][Spawn] ActorSpec invalido recebido pelo servico de spawn.");
            }

            this.uniqueIdFactory = uniqueIdFactory;
            _actorRegistry = actorRegistry;
            _context = context;
            _actorSpec = actorSpec;
            _prefab = prefab;
        }

        public virtual string Name => GetType().Name;

        /// <summary>
        /// Kind canonico do actor criado por este servico.
        /// </summary>
        public abstract ActorKind SpawnedActorKind { get; }
        public string SpawnArchetypeId => _actorSpec.SpawnArchetypeId;

        /// <summary>
        /// Indica se o actor deste servico deve existir apos o hard reset macro.
        /// </summary>
        public virtual bool IsRequiredForLifecycle => false;

        public Task SpawnAsync(ActorSpawnRequest request)
        {
            string traceId = request.ExecutionSignature;
            DebugUtility.LogVerbose(GetType(),
                ObservabilityTraceFormatter.BuildCompactLogMessage(
                    "SpawnAsync iniciado",
                    ("traceId", traceId),
                    ("actorKind", request.ActorKind),
                    ("recipe", request.OperationalRecipeKind),
                    ("axisActorId", request.AxisActorId),
                    ("runtimeActorId", request.RuntimeActorId),
                    ("actorSpecId", request.ActorSpecId),
                    ("spawnArchetypeId", request.SpawnArchetypeId),
                    ("actorSetMemberId", request.ActorSetMemberId),
                    ("occurrenceIndex", request.OccurrenceIndex),
                    ("actorSetRef", request.ActorSetRef),
                    ("semanticParticipantId", request.SemanticParticipantId),
                    ("spawnServiceName", request.SpawnServiceName),
                    ("scene", _context?.SceneName),
                    ("requiresLifecycleParticipation", request.RequiresLifecycleParticipation),
                    ("source", request.Source),
                    ("reason", request.Reason)));

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
                    $"Prefab canonico nao configurado para servico de spawn actorSpecId='{AsText(_actorSpec.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' recipe='{request.OperationalRecipeKind}' source='{AsText(request.Source)}'.");
                return Task.CompletedTask;
            }

            if (_spawnedActor != null)
            {
                DebugUtility.LogWarning(GetType(),
                    ObservabilityTraceFormatter.BuildCompactLogMessage(
                        "Spawn chamado mais de uma vez; ignorando.",
                        ("traceId", traceId),
                        ("actorKind", request.ActorKind),
                        ("runtimeActorId", TryReadActorId(_spawnedActor)),
                        ("scene", _context?.SceneName),
                        ("reason", "already_spawned")));
                return Task.CompletedTask;
            }

            ValidateCanonicalRequestOrFail(request);

            if (!ReferenceEquals(_prefab, _actorSpec.PlaceholderBodyPrefab))
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] Prefab canonico divergente do ActorSpec. actorSpecId='{AsText(_actorSpec.ActorSpecId)}' requestedActorSpecId='{AsText(request.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' recipe='{request.OperationalRecipeKind}' source='{AsText(request.Source)}'.");
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
                    ObservabilityTraceFormatter.BuildCompactLogMessage(
                        "[OBS][Gameplay][SpawnBridge] AxisActorId invalido no request canonico",
                        ("traceId", traceId),
                        ("actorSpecId", request.ActorSpecId),
                        ("actorSetRef", request.ActorSetRef),
                        ("axisActorId", request.AxisActorId),
                        ("recipe", request.OperationalRecipeKind),
                        ("source", request.Source),
                        ("reason", request.Reason)));
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
                request.SpawnArchetypeId,
                request.ActorSetMemberId,
                request.OccurrenceIndex,
                request.ActorSetRef,
                semanticParticipantId,
                request.OperationalRecipeKind,
                request.RuntimeReplacementCause,
                Name,
                _context.SceneName,
                request.Source,
                request.Reason,
                request.ExecutionSignature,
                IsRequiredForLifecycle);

            if (request.HasActorSpecId && string.IsNullOrWhiteSpace(completedEvent.ActorSpecId))
            {
                DebugUtility.LogError(GetType(),
                    ObservabilityTraceFormatter.BuildCompactLogMessage(
                        "[OBS][Gameplay][SpawnBridge] ActorSpawnCompletedEvent canonico sem ActorSpecId apos spawn",
                        ("traceId", traceId),
                        ("actorSetRef", request.ActorSetRef),
                        ("axisActorId", request.AxisActorId),
                        ("recipe", request.OperationalRecipeKind),
                        ("source", request.Source),
                        ("reason", request.Reason)));
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            if (request.HasActorSetRef && string.IsNullOrWhiteSpace(completedEvent.ActorSetRef))
            {
                DebugUtility.LogError(GetType(),
                    ObservabilityTraceFormatter.BuildCompactLogMessage(
                        "[OBS][Gameplay][SpawnBridge] ActorSpawnCompletedEvent canonico sem ActorSetRef apos spawn",
                        ("traceId", traceId),
                        ("actorSpecId", request.ActorSpecId),
                        ("axisActorId", request.AxisActorId),
                        ("recipe", request.OperationalRecipeKind),
                        ("source", request.Source),
                        ("reason", request.Reason)));
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            if (request.HasAxisActorId && !completedEvent.HasAxisActorId)
            {
                DebugUtility.LogError(GetType(),
                    ObservabilityTraceFormatter.BuildCompactLogMessage(
                        "[OBS][Gameplay][SpawnBridge] ActorSpawnCompletedEvent canonico sem AxisActorId apos spawn",
                        ("traceId", traceId),
                        ("actorSpecId", request.ActorSpecId),
                        ("actorSetRef", request.ActorSetRef),
                        ("recipe", request.OperationalRecipeKind),
                        ("source", request.Source),
                        ("reason", request.Reason)));
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            if (request.HasSemanticParticipantId && !completedEvent.HasSemanticParticipantId)
            {
                DebugUtility.LogError(GetType(),
                    ObservabilityTraceFormatter.BuildCompactLogMessage(
                        "[OBS][Gameplay][SpawnBridge] ActorSpawnCompletedEvent canonico sem SemanticParticipantId apos spawn",
                        ("traceId", traceId),
                        ("actorSpecId", request.ActorSpecId),
                        ("actorSetRef", request.ActorSetRef),
                        ("recipe", request.OperationalRecipeKind),
                        ("source", request.Source),
                        ("reason", request.Reason)));
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
                _spawnedActor = null;
                return Task.CompletedTask;
            }

            EventBus<ActorSpawnCompletedEvent>.Raise(
                completedEvent);

            DebugUtility.Log(GetType(),
                ObservabilityTraceFormatter.BuildCompactLogMessage(
                    "[OBS][Gameplay][SpawnBridge] ActorSpawnCompletedEvent published",
                    ("traceId", completedEvent.ExecutionSignature),
                    ("actorSpecId", completedEvent.ActorSpecId),
                    ("actorSetRef", completedEvent.ActorSetRef),
                    ("axisActorId", completedEvent.AxisActorId),
                    ("runtimeActorId", completedEvent.RuntimeActorId),
                    ("semanticParticipantId", completedEvent.SemanticParticipantId),
                    ("source", completedEvent.Source),
                    ("reason", completedEvent.Reason),
                    ("scene", completedEvent.SceneName)),
                DebugUtility.Colors.Info);

            string prefabName = _prefab != null ? _prefab.name : "<null>";
            string instanceName = _spawnedObject != null ? _spawnedObject.name : "<null>";
            DebugUtility.Log(GetType(),
                $"Actor spawned: {_spawnedActor.ActorId} (kind={SpawnedActorKind}, requiresLifecycleParticipation={IsRequiredForLifecycle}, prefab={prefabName}, instance={instanceName}, root={_context.WorldRoot?.name}, scene={_context.SceneName})");
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

            if (_spawnedActor == null && _spawnedObject == null)
            {
                DebugUtility.LogVerbose(GetType(),
                    "Despawn ignorado (no actor).", "cyan");
                return Task.CompletedTask;
            }

            string actorId = TryReadActorId(_spawnedActor);
            if (!string.IsNullOrWhiteSpace(actorId) && !_actorRegistry.Unregister(actorId))
            {
                DebugUtility.LogWarning(GetType(),
                    $"Falha ao remover ator do registry. ActorId={actorId}");
            }

            Object.Destroy(_spawnedObject);

            _spawnedActor = null;
            _spawnedObject = null;

            DebugUtility.Log(GetType(),
                $"Actor despawned: {AsText(actorId)} (kind={SpawnedActorKind}, root={_context?.WorldRoot?.name}, scene={_context?.SceneName})");
            DebugUtility.Log(GetType(), $"Registry count: {_actorRegistry.Count}");

            return Task.CompletedTask;
        }

        public bool TryGetCurrentRuntimeActorId(out RuntimeActorId runtimeActorId)
        {
            runtimeActorId = RuntimeActorId.None;
            if (_spawnedActor == null || string.IsNullOrWhiteSpace(_spawnedActor.ActorId))
            {
                return false;
            }

            runtimeActorId = new RuntimeActorId(_spawnedActor.ActorId);
            return runtimeActorId.IsValid;
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
                    $"[FATAL][H1][Spawn] ActorSpawnRequest invalido recebido pelo servico de spawn. actorSpecId='{AsText(request.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' recipe='{request.OperationalRecipeKind}' source='{AsText(request.Source)}'.");
            }

            if (request.ActorKind != SpawnedActorKind)
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest com actorKind divergente recebido pelo servico de spawn. requestActorKind='{request.ActorKind}' serviceActorKind='{SpawnedActorKind}' actorSpecId='{AsText(request.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' recipe='{request.OperationalRecipeKind}' source='{AsText(request.Source)}'.");
            }

            if (!request.HasAxisActorId || !request.HasActorSpecId || !request.HasActorSetRef)
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest canonico incompleto; axisActorId='{request.AxisActorId}' actorSpecId='{AsText(request.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' recipe='{request.OperationalRecipeKind}' source='{AsText(request.Source)}' actorKind='{request.ActorKind}'.");
            }
            if (string.IsNullOrWhiteSpace(request.SpawnArchetypeId) ||
                string.IsNullOrWhiteSpace(request.ActorSetMemberId) ||
                request.OccurrenceIndex < 0)
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest canonico sem identidade member/archetype completa; axisActorId='{request.AxisActorId}' actorSpecId='{AsText(request.ActorSpecId)}' spawnArchetypeId='{AsText(request.SpawnArchetypeId)}' actorSetMemberId='{AsText(request.ActorSetMemberId)}' occurrenceIndex='{request.OccurrenceIndex}' actorSetRef='{AsText(request.ActorSetRef)}' source='{AsText(request.Source)}'.");
            }

            if (request.OperationalRecipeKind == ActorOperationalRecipeKind.Unknown)
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest sem recipe operacional canonica. actorSpecId='{AsText(request.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' source='{AsText(request.Source)}'.");
            }

            if (!string.Equals(request.ActorSpecId, _actorSpec.ActorSpecId, StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest com actorSpecId divergente do ActorSpec canonico. requestActorSpecId='{AsText(request.ActorSpecId)}' canonicalActorSpecId='{AsText(_actorSpec.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' recipe='{request.OperationalRecipeKind}' source='{AsText(request.Source)}'.");
            }
            if (!string.Equals(request.SpawnArchetypeId, _actorSpec.SpawnArchetypeId, StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest com spawnArchetypeId divergente do ActorSpec canonico. requestSpawnArchetypeId='{AsText(request.SpawnArchetypeId)}' canonicalSpawnArchetypeId='{AsText(_actorSpec.SpawnArchetypeId)}' actorSpecId='{AsText(request.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' source='{AsText(request.Source)}'.");
            }

            if (request.OperationalRecipeKind != _actorSpec.OperationalRecipeKind)
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest com recipe divergente do ActorSpec canonico. requestRecipe='{request.OperationalRecipeKind}' canonicalRecipe='{_actorSpec.OperationalRecipeKind}' actorSpecId='{AsText(request.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' source='{AsText(request.Source)}'.");
            }

            if (SpawnedActorKind == ActorKind.Player && !request.HasSemanticParticipantId)
            {
                HardFailFastH1.Trigger(GetType(),
                    $"[FATAL][H1][Spawn] ActorSpawnRequest canonico de Player sem SemanticParticipantId. axisActorId='{request.AxisActorId}' actorSpecId='{AsText(request.ActorSpecId)}' actorSetRef='{AsText(request.ActorSetRef)}' recipe='{request.OperationalRecipeKind}' source='{AsText(request.Source)}'.");
            }
        }

        protected virtual void OnPostInstantiate(GameObject instance) { }

        private static string TryReadActorId(IActor actor)
        {
            return actor == null || string.IsNullOrWhiteSpace(actor.ActorId)
                ? string.Empty
                : actor.ActorId.Trim();
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
