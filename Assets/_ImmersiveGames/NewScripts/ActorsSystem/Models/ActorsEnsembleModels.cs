using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public readonly struct ActorsEnsembleInput
    {
        public ActorsEnsembleInput(
            ActorsDefinitionsSnapshot definitions,
            ActorsSemanticParticipationSnapshot semanticParticipation,
            ActorsRuntimeObservationSnapshot runtimeObservation)
        {
            Definitions = definitions;
            SemanticParticipation = semanticParticipation;
            RuntimeObservation = runtimeObservation;
        }

        public ActorsDefinitionsSnapshot Definitions { get; }
        public ActorsSemanticParticipationSnapshot SemanticParticipation { get; }
        public ActorsRuntimeObservationSnapshot RuntimeObservation { get; }

        public bool IsValid => Definitions.IsValid;
    }

    public readonly struct ActorIdentityRecord : IEquatable<ActorIdentityRecord>
    {
        public ActorIdentityRecord(
            AxisActorId axisActorId,
            ActorRole role,
            ActorRelevance relevance,
            string semanticParticipantId,
            ActorOperationalRecipeKind operationalRecipeKind,
            RuntimeActorId runtimeActorId,
            string actorSpecId,
            string spawnArchetypeId,
            string actorSetMemberId,
            int occurrenceIndex,
            ActorSpecRealizationMode realizationMode,
            ActorSpecContinuityResetPolicy continuityResetPolicy,
            string actorSetRef,
            bool hasSemanticLink,
            bool isRuntimeObserved)
        {
            AxisActorId = axisActorId;
            Role = role;
            Relevance = relevance;
            SemanticParticipantId = Normalize(semanticParticipantId);
            OperationalRecipeKind = operationalRecipeKind;
            RuntimeActorId = runtimeActorId;
            ActorSpecId = Normalize(actorSpecId);
            SpawnArchetypeId = Normalize(spawnArchetypeId);
            ActorSetMemberId = Normalize(actorSetMemberId);
            OccurrenceIndex = occurrenceIndex < 0 ? 0 : occurrenceIndex;
            RealizationMode = realizationMode;
            ContinuityResetPolicy = continuityResetPolicy;
            ActorSetRef = Normalize(actorSetRef);
            HasSemanticLink = hasSemanticLink;
            IsRuntimeObserved = isRuntimeObserved;
        }

        public AxisActorId AxisActorId { get; }
        public ActorRole Role { get; }
        public ActorRelevance Relevance { get; }
        public string SemanticParticipantId { get; }
        public ActorOperationalRecipeKind OperationalRecipeKind { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public string ActorSpecId { get; }
        public string SpawnArchetypeId { get; }
        public string ActorSetMemberId { get; }
        public int OccurrenceIndex { get; }
        public ActorSpecRealizationMode RealizationMode { get; }
        public ActorSpecContinuityResetPolicy ContinuityResetPolicy { get; }
        public string ActorSetRef { get; }
        public bool HasSemanticLink { get; }
        public bool IsRuntimeObserved { get; }

        public bool IsValid =>
            AxisActorId.IsValid &&
            Role != ActorRole.Unknown &&
            !string.IsNullOrWhiteSpace(ActorSpecId) &&
            !string.IsNullOrWhiteSpace(SpawnArchetypeId) &&
            !string.IsNullOrWhiteSpace(ActorSetMemberId) &&
            OccurrenceIndex >= 0;

        public bool Equals(ActorIdentityRecord other)
        {
            return AxisActorId.Equals(other.AxisActorId) &&
                   Role == other.Role &&
                   Relevance == other.Relevance &&
                   string.Equals(SemanticParticipantId, other.SemanticParticipantId, StringComparison.Ordinal) &&
                   OperationalRecipeKind == other.OperationalRecipeKind &&
                   RuntimeActorId.Equals(other.RuntimeActorId) &&
                   string.Equals(ActorSpecId, other.ActorSpecId, StringComparison.Ordinal) &&
                   string.Equals(SpawnArchetypeId, other.SpawnArchetypeId, StringComparison.Ordinal) &&
                   string.Equals(ActorSetMemberId, other.ActorSetMemberId, StringComparison.Ordinal) &&
                   OccurrenceIndex == other.OccurrenceIndex &&
                   RealizationMode == other.RealizationMode &&
                   ContinuityResetPolicy == other.ContinuityResetPolicy &&
                   string.Equals(ActorSetRef, other.ActorSetRef, StringComparison.Ordinal) &&
                   HasSemanticLink == other.HasSemanticLink &&
                   IsRuntimeObserved == other.IsRuntimeObserved;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorIdentityRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Role;
                hashCode = (hashCode * 397) ^ (int)Relevance;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)OperationalRecipeKind;
                hashCode = (hashCode * 397) ^ RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSpecId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SpawnArchetypeId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSetMemberId ?? string.Empty);
                hashCode = (hashCode * 397) ^ OccurrenceIndex;
                hashCode = (hashCode * 397) ^ (int)RealizationMode;
                hashCode = (hashCode * 397) ^ (int)ContinuityResetPolicy;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSetRef ?? string.Empty);
                hashCode = (hashCode * 397) ^ HasSemanticLink.GetHashCode();
                hashCode = (hashCode * 397) ^ IsRuntimeObserved.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"axisActorId='{AxisActorId}', role='{Role}', relevance='{Relevance}', semanticParticipantId='{AsText(SemanticParticipantId)}', operationalRecipeKind='{OperationalRecipeKind}', runtimeActorId='{RuntimeActorId}', actorSpecId='{AsText(ActorSpecId)}', spawnArchetypeId='{AsText(SpawnArchetypeId)}', actorSetMemberId='{AsText(ActorSetMemberId)}', occurrenceIndex='{OccurrenceIndex}', realizationMode='{RealizationMode}', continuityResetPolicy='{ContinuityResetPolicy}', actorSetRef='{AsText(ActorSetRef)}', hasSemanticLink='{HasSemanticLink}', isRuntimeObserved='{IsRuntimeObserved}'";
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

    public readonly struct ActorIdentityRoleSet : IEquatable<ActorIdentityRoleSet>
    {
        public ActorIdentityRoleSet(string signature, ActorIdentityRecord[] entries)
        {
            Signature = Normalize(signature);
            Entries = entries == null ? Array.Empty<ActorIdentityRecord>() : (ActorIdentityRecord[])entries.Clone();
        }

        public string Signature { get; }
        public ActorIdentityRecord[] Entries { get; }

        public int Count => Entries?.Length ?? 0;
        public bool HasEntries => Count > 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(Signature);

        public static ActorIdentityRoleSet Empty => new(string.Empty, Array.Empty<ActorIdentityRecord>());

        public bool Equals(ActorIdentityRoleSet other)
        {
            return string.Equals(Signature, other.Signature, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorIdentityRoleSet other && Equals(other);
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

    public readonly struct ActorsEnsembleSnapshot : IEquatable<ActorsEnsembleSnapshot>
    {
        public ActorsEnsembleSnapshot(
            string definitionsSignature,
            string semanticParticipationSignature,
            string runtimeObservationSignature,
            string ensembleSignature,
            ActorIdentityRecord[] members,
            string reason)
        {
            DefinitionsSignature = Normalize(definitionsSignature);
            SemanticParticipationSignature = Normalize(semanticParticipationSignature);
            RuntimeObservationSignature = Normalize(runtimeObservationSignature);
            EnsembleSignature = Normalize(ensembleSignature);
            Members = members == null ? Array.Empty<ActorIdentityRecord>() : (ActorIdentityRecord[])members.Clone();
            Reason = Normalize(reason);
        }

        public string DefinitionsSignature { get; }
        public string SemanticParticipationSignature { get; }
        public string RuntimeObservationSignature { get; }
        public string EnsembleSignature { get; }
        public ActorIdentityRecord[] Members { get; }
        public string Reason { get; }

        public int Count => Members?.Length ?? 0;
        public bool HasMembers => Count > 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(EnsembleSignature);

        public static ActorsEnsembleSnapshot Empty => new(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<ActorIdentityRecord>(),
            string.Empty);

        public bool Equals(ActorsEnsembleSnapshot other)
        {
            return string.Equals(EnsembleSignature, other.EnsembleSignature, StringComparison.Ordinal) &&
                   string.Equals(DefinitionsSignature, other.DefinitionsSignature, StringComparison.Ordinal) &&
                   string.Equals(SemanticParticipationSignature, other.SemanticParticipationSignature, StringComparison.Ordinal) &&
                   string.Equals(RuntimeObservationSignature, other.RuntimeObservationSignature, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsEnsembleSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(EnsembleSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(DefinitionsSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipationSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RuntimeObservationSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ Count;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"ensembleSignature='{AsText(EnsembleSignature)}', count='{Count}', reason='{AsText(Reason)}'";
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
