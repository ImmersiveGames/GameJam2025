using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public enum ActorOperationalRecipeKind
    {
        Unknown = 0,
        Player = 1,
        Dummy = 2,
        Eater = 3
    }

    public readonly struct ActorDefinitionRecord : IEquatable<ActorDefinitionRecord>
    {
        public ActorDefinitionRecord(
            AxisActorId axisActorId,
            ActorRole role,
            bool isRequired,
            string semanticParticipantId,
            ActorOperationalRecipeKind operationalRecipeKind,
            RuntimeActorId preferredRuntimeActorId,
            string actorSpecId,
            string actorSetRef,
            string source)
        {
            AxisActorId = axisActorId;
            Role = role;
            IsRequired = isRequired;
            SemanticParticipantId = Normalize(semanticParticipantId);
            OperationalRecipeKind = operationalRecipeKind;
            PreferredRuntimeActorId = preferredRuntimeActorId;
            ActorSpecId = Normalize(actorSpecId);
            ActorSetRef = Normalize(actorSetRef);
            Source = Normalize(source);
        }

        public AxisActorId AxisActorId { get; }
        public ActorRole Role { get; }
        public bool IsRequired { get; }
        public string SemanticParticipantId { get; }
        public ActorOperationalRecipeKind OperationalRecipeKind { get; }
        public RuntimeActorId PreferredRuntimeActorId { get; }
        public string ActorSpecId { get; }
        public string ActorSetRef { get; }
        public string Source { get; }

        public bool IsValid => AxisActorId.IsValid && Role != ActorRole.Unknown;
        public bool HasSemanticParticipantId => !string.IsNullOrWhiteSpace(SemanticParticipantId);
        public bool HasPreferredRuntimeActorId => PreferredRuntimeActorId.IsValid;

        public bool Equals(ActorDefinitionRecord other)
        {
            return AxisActorId.Equals(other.AxisActorId) &&
                   Role == other.Role &&
                   IsRequired == other.IsRequired &&
                   string.Equals(SemanticParticipantId, other.SemanticParticipantId, StringComparison.Ordinal) &&
                   OperationalRecipeKind == other.OperationalRecipeKind &&
                   PreferredRuntimeActorId.Equals(other.PreferredRuntimeActorId) &&
                   string.Equals(ActorSpecId, other.ActorSpecId, StringComparison.Ordinal) &&
                   string.Equals(ActorSetRef, other.ActorSetRef, StringComparison.Ordinal) &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorDefinitionRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Role;
                hashCode = (hashCode * 397) ^ IsRequired.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)OperationalRecipeKind;
                hashCode = (hashCode * 397) ^ PreferredRuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSpecId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSetRef ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"axisActorId='{AxisActorId}', role='{Role}', required='{IsRequired}', semanticParticipantId='{AsText(SemanticParticipantId)}', operationalRecipeKind='{OperationalRecipeKind}', preferredRuntimeActorId='{PreferredRuntimeActorId}', actorSpecId='{AsText(ActorSpecId)}', actorSetRef='{AsText(ActorSetRef)}', source='{AsText(Source)}'";
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

    public readonly struct ActorsDefinitionsSnapshot : IEquatable<ActorsDefinitionsSnapshot>
    {
        public ActorsDefinitionsSnapshot(string signature, ActorDefinitionRecord[] entries)
        {
            Signature = Normalize(signature);
            Entries = entries == null ? Array.Empty<ActorDefinitionRecord>() : (ActorDefinitionRecord[])entries.Clone();
        }

        public string Signature { get; }
        public ActorDefinitionRecord[] Entries { get; }

        public int Count => Entries?.Length ?? 0;
        public bool HasEntries => Count > 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(Signature);

        public static ActorsDefinitionsSnapshot Empty => new(string.Empty, Array.Empty<ActorDefinitionRecord>());

        public bool Equals(ActorsDefinitionsSnapshot other)
        {
            return string.Equals(Signature, other.Signature, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsDefinitionsSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(Signature ?? string.Empty) * 397) ^ Count;
            }
        }

        public override string ToString()
        {
            return $"signature='{AsText(Signature)}', count='{Count}'";
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
