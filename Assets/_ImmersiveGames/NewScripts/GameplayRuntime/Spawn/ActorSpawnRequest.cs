using System;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using GameActorKind = _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core.ActorKind;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Spawn
{
    public readonly struct ActorSpawnRequest : IEquatable<ActorSpawnRequest>
    {
        public ActorSpawnRequest(
            GameActorKind actorKind,
            ActorOperationalRecipeKind operationalRecipeKind,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            string actorSpecId,
            string actorSetRef,
            string semanticParticipantId,
            string spawnServiceName,
            string sceneName,
            bool requiredForWorldReset,
            string source,
            string reason,
            string executionSignature)
        {
            ActorKind = actorKind;
            OperationalRecipeKind = operationalRecipeKind;
            AxisActorId = axisActorId;
            RuntimeActorId = runtimeActorId;
            ActorSpecId = Normalize(actorSpecId);
            ActorSetRef = Normalize(actorSetRef);
            SemanticParticipantId = Normalize(semanticParticipantId);
            SpawnServiceName = Normalize(spawnServiceName);
            SceneName = Normalize(sceneName);
            RequiredForWorldReset = requiredForWorldReset;
            Source = Normalize(source);
            Reason = Normalize(reason);
            ExecutionSignature = Normalize(executionSignature);
        }

        public GameActorKind ActorKind { get; }
        public ActorOperationalRecipeKind OperationalRecipeKind { get; }
        public AxisActorId AxisActorId { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public string ActorSpecId { get; }
        public string ActorSetRef { get; }
        public string SemanticParticipantId { get; }
        public string SpawnServiceName { get; }
        public string SceneName { get; }
        public bool RequiredForWorldReset { get; }
        public string Source { get; }
        public string Reason { get; }
        public string ExecutionSignature { get; }

        public bool HasAxisActorId => AxisActorId.IsValid;
        public bool HasRuntimeActorId => RuntimeActorId.IsValid;
        public bool HasSemanticParticipantId => !string.IsNullOrWhiteSpace(SemanticParticipantId);
        public bool HasActorSpecId => !string.IsNullOrWhiteSpace(ActorSpecId);
        public bool HasActorSetRef => !string.IsNullOrWhiteSpace(ActorSetRef);
        public bool IsValid => ActorKind != GameActorKind.Unknown;

        public static ActorSpawnRequest FromExecutionEntry(
            ActorsMaterializationExecutionEntry entry,
            GameActorKind actorKind,
            string spawnServiceName,
            string sceneName,
            bool requiredForWorldReset,
            string source,
            string executionSignature)
        {
            return new ActorSpawnRequest(
                actorKind,
                entry.OperationalRecipeKind,
                entry.AxisActorId,
                entry.RuntimeActorId,
                entry.ActorSpecId,
                entry.ActorSetRef,
                entry.SemanticParticipantId,
                spawnServiceName,
                sceneName,
                requiredForWorldReset,
                source,
                entry.Reason,
                executionSignature);
        }

        public bool Equals(ActorSpawnRequest other)
        {
            return ActorKind == other.ActorKind &&
                   OperationalRecipeKind == other.OperationalRecipeKind &&
                   AxisActorId.Equals(other.AxisActorId) &&
                   RuntimeActorId.Equals(other.RuntimeActorId) &&
                   string.Equals(ActorSpecId, other.ActorSpecId, StringComparison.Ordinal) &&
                   string.Equals(ActorSetRef, other.ActorSetRef, StringComparison.Ordinal) &&
                   string.Equals(SemanticParticipantId, other.SemanticParticipantId, StringComparison.Ordinal) &&
                   string.Equals(SpawnServiceName, other.SpawnServiceName, StringComparison.Ordinal) &&
                   string.Equals(SceneName, other.SceneName, StringComparison.Ordinal) &&
                   RequiredForWorldReset == other.RequiredForWorldReset &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   string.Equals(ExecutionSignature, other.ExecutionSignature, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorSpawnRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)ActorKind;
                hashCode = (hashCode * 397) ^ (int)OperationalRecipeKind;
                hashCode = (hashCode * 397) ^ AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSpecId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSetRef ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SpawnServiceName ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SceneName ?? string.Empty);
                hashCode = (hashCode * 397) ^ RequiredForWorldReset.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ExecutionSignature ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"actorKind='{ActorKind}', recipe='{OperationalRecipeKind}', axisActorId='{AxisActorId}', runtimeActorId='{RuntimeActorId}', actorSpecId='{AsText(ActorSpecId)}', actorSetRef='{AsText(ActorSetRef)}', semanticParticipantId='{AsText(SemanticParticipantId)}', spawnServiceName='{AsText(SpawnServiceName)}', sceneName='{AsText(SceneName)}', requiredForWorldReset='{RequiredForWorldReset}', source='{AsText(Source)}', reason='{AsText(Reason)}', executionSignature='{AsText(ExecutionSignature)}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
        }
    }
}
