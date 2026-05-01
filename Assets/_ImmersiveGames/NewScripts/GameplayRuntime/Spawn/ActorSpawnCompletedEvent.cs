using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    /// <summary>
    /// Marco canônico de que o spawn terminou com sucesso.
    /// Neste ponto, a identidade já foi atribuída e o actor já foi registrado no runtime.
    /// </summary>
    public readonly struct ActorSpawnCompletedEvent : IEvent
    {
        public ActorSpawnCompletedEvent(
            IActor actor,
            ActorKind actorKind,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            string actorId,
            string actorSpecId,
            string spawnArchetypeId,
            string actorSetMemberId,
            int occurrenceIndex,
            string actorSetRef,
            string semanticParticipantId,
            ActorOperationalRecipeKind operationalRecipeKind,
            ActorsRuntimeReplacementCause runtimeReplacementCause,
            string spawnServiceName,
            string sceneName,
            string source,
            string reason,
            string executionSignature,
            bool requiredForWorldReset)
        {
            Actor = actor;
            ActorKind = actorKind;
            AxisActorId = axisActorId;
            RuntimeActorId = runtimeActorId;
            ActorId = string.IsNullOrWhiteSpace(actorId) ? string.Empty : actorId.Trim();
            ActorSpecId = string.IsNullOrWhiteSpace(actorSpecId) ? string.Empty : actorSpecId.Trim();
            SpawnArchetypeId = string.IsNullOrWhiteSpace(spawnArchetypeId) ? string.Empty : spawnArchetypeId.Trim();
            ActorSetMemberId = string.IsNullOrWhiteSpace(actorSetMemberId) ? string.Empty : actorSetMemberId.Trim();
            OccurrenceIndex = occurrenceIndex < 0 ? 0 : occurrenceIndex;
            ActorSetRef = string.IsNullOrWhiteSpace(actorSetRef) ? string.Empty : actorSetRef.Trim();
            SemanticParticipantId = string.IsNullOrWhiteSpace(semanticParticipantId) ? string.Empty : semanticParticipantId.Trim();
            OperationalRecipeKind = operationalRecipeKind;
            RuntimeReplacementCause = runtimeReplacementCause;
            SpawnServiceName = string.IsNullOrWhiteSpace(spawnServiceName) ? string.Empty : spawnServiceName.Trim();
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            ExecutionSignature = string.IsNullOrWhiteSpace(executionSignature) ? string.Empty : executionSignature.Trim();
            RequiredForWorldReset = requiredForWorldReset;
        }

        public IActor Actor { get; }

        public ActorKind ActorKind { get; }

        public AxisActorId AxisActorId { get; }

        public RuntimeActorId RuntimeActorId { get; }

        public string ActorId { get; }

        public string ActorSpecId { get; }
        public string SpawnArchetypeId { get; }
        public string ActorSetMemberId { get; }
        public int OccurrenceIndex { get; }

        public string ActorSetRef { get; }

        public string SemanticParticipantId { get; }

        public ActorOperationalRecipeKind OperationalRecipeKind { get; }
        public ActorsRuntimeReplacementCause RuntimeReplacementCause { get; }

        public string SpawnServiceName { get; }

        public string SceneName { get; }

        public string Source { get; }

        public string Reason { get; }

        public string ExecutionSignature { get; }

        public bool RequiredForWorldReset { get; }

        public bool HasActor => Actor != null;
        public bool HasAxisActorId => AxisActorId.IsValid;
        public bool HasRuntimeActorId => RuntimeActorId.IsValid;
        public bool HasActorSpecId => !string.IsNullOrWhiteSpace(ActorSpecId);
        public bool HasSpawnArchetypeId => !string.IsNullOrWhiteSpace(SpawnArchetypeId);
        public bool HasActorSetMemberId => !string.IsNullOrWhiteSpace(ActorSetMemberId);
        public bool HasActorSetRef => !string.IsNullOrWhiteSpace(ActorSetRef);
        public bool HasSemanticParticipantId => !string.IsNullOrWhiteSpace(SemanticParticipantId);
        public bool HasCanonicalPayload =>
            HasAxisActorId &&
            HasRuntimeActorId &&
            HasActorSpecId &&
            HasSpawnArchetypeId &&
            HasActorSetMemberId &&
            OccurrenceIndex >= 0 &&
            HasActorSetRef &&
            !string.IsNullOrWhiteSpace(SpawnServiceName) &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(ExecutionSignature) &&
            (ActorKind != ActorKind.Player || HasSemanticParticipantId);
    }
}
