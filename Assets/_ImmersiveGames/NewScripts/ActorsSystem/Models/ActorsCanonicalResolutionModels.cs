using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public readonly struct CanonicalActorOccurrence : IEquatable<CanonicalActorOccurrence>
    {
        public CanonicalActorOccurrence(
            AxisActorId axisActorId,
            int instanceIndex,
            string semanticParticipantId,
            bool isRequired)
        {
            AxisActorId = axisActorId;
            InstanceIndex = instanceIndex < 0 ? 0 : instanceIndex;
            SemanticParticipantId = CanonicalResolutionText.Normalize(semanticParticipantId);
            IsRequired = isRequired;
        }

        public AxisActorId AxisActorId { get; }
        public int InstanceIndex { get; }
        public string SemanticParticipantId { get; }
        public bool IsRequired { get; }
        public bool HasSemanticParticipantId => !string.IsNullOrWhiteSpace(SemanticParticipantId);
        public bool IsValid => AxisActorId.IsValid && InstanceIndex >= 0;

        public bool Equals(CanonicalActorOccurrence other)
        {
            return AxisActorId.Equals(other.AxisActorId) &&
                   InstanceIndex == other.InstanceIndex &&
                   string.Equals(SemanticParticipantId, other.SemanticParticipantId, StringComparison.Ordinal) &&
                   IsRequired == other.IsRequired;
        }

        public override bool Equals(object obj)
        {
            return obj is CanonicalActorOccurrence other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ InstanceIndex;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ IsRequired.GetHashCode();
                return hashCode;
            }
        }
    }

    public readonly struct CanonicalActorResolution : IEquatable<CanonicalActorResolution>
    {
        public CanonicalActorResolution(
            ActorSetRef actorSetRef,
            ActorSetMemberId actorSetMemberId,
            string actorSpecId,
            string spawnArchetypeId,
            ActorRole role,
            ActorOperationalRecipeKind operationalRecipeKind,
            ActorSpecRealizationMode realizationMode,
            ActorSpecContinuityResetPolicy continuityResetPolicy,
            CanonicalActorOccurrence[] occurrences,
            string source)
        {
            ActorSetRef = actorSetRef;
            ActorSetMemberId = actorSetMemberId;
            ActorSpecId = CanonicalResolutionText.Normalize(actorSpecId);
            SpawnArchetypeId = CanonicalResolutionText.Normalize(spawnArchetypeId);
            Role = role;
            OperationalRecipeKind = operationalRecipeKind;
            RealizationMode = realizationMode;
            ContinuityResetPolicy = continuityResetPolicy;
            Occurrences = occurrences == null ? Array.Empty<CanonicalActorOccurrence>() : (CanonicalActorOccurrence[])occurrences.Clone();
            Source = CanonicalResolutionText.Normalize(source);
        }

        public ActorSetRef ActorSetRef { get; }
        public ActorSetMemberId ActorSetMemberId { get; }
        public string ActorSpecId { get; }
        public string SpawnArchetypeId { get; }
        public ActorRole Role { get; }
        public ActorOperationalRecipeKind OperationalRecipeKind { get; }
        public ActorSpecRealizationMode RealizationMode { get; }
        public ActorSpecContinuityResetPolicy ContinuityResetPolicy { get; }
        public CanonicalActorOccurrence[] Occurrences { get; }
        public string Source { get; }
        public int OccurrenceCount => Occurrences?.Length ?? 0;
        public bool HasOccurrences => OccurrenceCount > 0;
        public bool IsValid =>
            ActorSetRef.IsValid &&
            ActorSetMemberId.IsValid &&
            !string.IsNullOrWhiteSpace(ActorSpecId) &&
            !string.IsNullOrWhiteSpace(SpawnArchetypeId) &&
            Role != ActorRole.Unknown &&
            OperationalRecipeKind != ActorOperationalRecipeKind.Unknown &&
            HasOccurrences;

        public bool Equals(CanonicalActorResolution other)
        {
            return ActorSetRef.Equals(other.ActorSetRef) &&
                   ActorSetMemberId.Equals(other.ActorSetMemberId) &&
                   string.Equals(ActorSpecId, other.ActorSpecId, StringComparison.Ordinal) &&
                   string.Equals(SpawnArchetypeId, other.SpawnArchetypeId, StringComparison.Ordinal) &&
                   Role == other.Role &&
                   OperationalRecipeKind == other.OperationalRecipeKind &&
                   RealizationMode == other.RealizationMode &&
                   ContinuityResetPolicy == other.ContinuityResetPolicy &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   OccurrenceCount == other.OccurrenceCount;
        }

        public override bool Equals(object obj)
        {
            return obj is CanonicalActorResolution other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ActorSetRef.GetHashCode();
                hashCode = (hashCode * 397) ^ ActorSetMemberId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSpecId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SpawnArchetypeId ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)Role;
                hashCode = (hashCode * 397) ^ (int)OperationalRecipeKind;
                hashCode = (hashCode * 397) ^ (int)RealizationMode;
                hashCode = (hashCode * 397) ^ (int)ContinuityResetPolicy;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ OccurrenceCount;
                return hashCode;
            }
        }
    }

    public readonly struct CanonicalActorResolutionSet : IEquatable<CanonicalActorResolutionSet>
    {
        public CanonicalActorResolutionSet(string signature, CanonicalActorResolution[] entries)
        {
            Signature = CanonicalResolutionText.Normalize(signature);
            Entries = entries == null ? Array.Empty<CanonicalActorResolution>() : (CanonicalActorResolution[])entries.Clone();
        }

        public string Signature { get; }
        public CanonicalActorResolution[] Entries { get; }
        public int Count => Entries?.Length ?? 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(Signature);

        public bool Equals(CanonicalActorResolutionSet other)
        {
            return string.Equals(Signature, other.Signature, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is CanonicalActorResolutionSet other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(Signature ?? string.Empty) * 397) ^ Count;
            }
        }
    }
    
    internal static class CanonicalResolutionText
    {
        internal static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
