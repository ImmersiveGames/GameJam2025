using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public enum ActorMaterializationSpecKind
    {
        AxisActor = 0,
        RuntimeOrphan = 1
    }

    public enum ActorMaterializationIntent
    {
        Unknown = 0,
        Keep = 1,
        Materialize = 2,
        Rematerialize = 3,
        ObserveWithoutAction = 4,
        Inconsistent = 5,
        ExcessOrphan = 6
    }

    public enum ActorMaterializationClassification
    {
        Unknown = 0,
        StableNoAction = 1,
        ObserveNoAction = 2,
        RequiresExecutionMaterialize = 3,
        RequiresExecutionRematerialize = 4,
        InconsistentNoAutoRemediation = 5,
        RuntimeOrphanTolerated = 6,
        RuntimeOrphanProblematic = 7
    }

    public readonly struct ActorsMaterializationSpecEntry : IEquatable<ActorsMaterializationSpecEntry>
    {
        public ActorsMaterializationSpecEntry(
            ActorMaterializationSpecKind kind,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            ActorRole role,
            ActorOperationalRecipeKind operationalRecipeKind,
            ActorRelevance relevance,
            string semanticParticipantId,
            string actorSpecId,
            string spawnArchetypeId,
            string actorSetMemberId,
            int occurrenceIndex,
            ActorSpecRealizationMode realizationMode,
            ActorSpecContinuityResetPolicy continuityResetPolicy,
            string actorSetRef,
            ActorPresenceStatus presenceStatus,
            bool isExpected,
            bool isMaterialized,
            bool isInconsistent,
            bool hasOperationalBinding,
            ActorsOperationalBindingState operationalBindingState,
            ActorMaterializationIntent intent,
            ActorMaterializationClassification classification,
            string reason)
        {
            Kind = kind;
            AxisActorId = axisActorId;
            RuntimeActorId = runtimeActorId;
            Role = role;
            OperationalRecipeKind = operationalRecipeKind;
            Relevance = relevance;
            SemanticParticipantId = Normalize(semanticParticipantId);
            ActorSpecId = Normalize(actorSpecId);
            SpawnArchetypeId = Normalize(spawnArchetypeId);
            ActorSetMemberId = Normalize(actorSetMemberId);
            OccurrenceIndex = occurrenceIndex < 0 ? 0 : occurrenceIndex;
            RealizationMode = realizationMode;
            ContinuityResetPolicy = continuityResetPolicy;
            ActorSetRef = Normalize(actorSetRef);
            PresenceStatus = presenceStatus;
            IsExpected = isExpected;
            IsMaterialized = isMaterialized;
            IsInconsistent = isInconsistent;
            HasOperationalBinding = hasOperationalBinding;
            OperationalBindingState = operationalBindingState;
            Intent = intent;
            Classification = classification;
            Reason = Normalize(reason);
        }

        public ActorMaterializationSpecKind Kind { get; }
        public AxisActorId AxisActorId { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public ActorRole Role { get; }
        public ActorOperationalRecipeKind OperationalRecipeKind { get; }
        public ActorRelevance Relevance { get; }
        public string SemanticParticipantId { get; }
        public string ActorSpecId { get; }
        public string SpawnArchetypeId { get; }
        public string ActorSetMemberId { get; }
        public int OccurrenceIndex { get; }
        public ActorSpecRealizationMode RealizationMode { get; }
        public ActorSpecContinuityResetPolicy ContinuityResetPolicy { get; }
        public string ActorSetRef { get; }
        public ActorPresenceStatus PresenceStatus { get; }
        public bool IsExpected { get; }
        public bool IsMaterialized { get; }
        public bool IsInconsistent { get; }
        public bool HasOperationalBinding { get; }
        public ActorsOperationalBindingState OperationalBindingState { get; }
        public ActorMaterializationIntent Intent { get; }
        public ActorMaterializationClassification Classification { get; }
        public string Reason { get; }

        public bool IsValid =>
            Intent != ActorMaterializationIntent.Unknown &&
            Classification != ActorMaterializationClassification.Unknown &&
            (Kind == ActorMaterializationSpecKind.AxisActor
                ? AxisActorId.IsValid &&
                  !string.IsNullOrWhiteSpace(ActorSpecId) &&
                  !string.IsNullOrWhiteSpace(SpawnArchetypeId) &&
                  !string.IsNullOrWhiteSpace(ActorSetMemberId) &&
                  OccurrenceIndex >= 0
                : RuntimeActorId.IsValid);

        public bool Equals(ActorsMaterializationSpecEntry other)
        {
            return Kind == other.Kind &&
                   AxisActorId.Equals(other.AxisActorId) &&
                   RuntimeActorId.Equals(other.RuntimeActorId) &&
                   Role == other.Role &&
                   OperationalRecipeKind == other.OperationalRecipeKind &&
                   Relevance == other.Relevance &&
                   string.Equals(SemanticParticipantId, other.SemanticParticipantId, StringComparison.Ordinal) &&
                   string.Equals(ActorSpecId, other.ActorSpecId, StringComparison.Ordinal) &&
                   string.Equals(SpawnArchetypeId, other.SpawnArchetypeId, StringComparison.Ordinal) &&
                   string.Equals(ActorSetMemberId, other.ActorSetMemberId, StringComparison.Ordinal) &&
                   OccurrenceIndex == other.OccurrenceIndex &&
                   RealizationMode == other.RealizationMode &&
                   ContinuityResetPolicy == other.ContinuityResetPolicy &&
                   string.Equals(ActorSetRef, other.ActorSetRef, StringComparison.Ordinal) &&
                   PresenceStatus == other.PresenceStatus &&
                   IsExpected == other.IsExpected &&
                   IsMaterialized == other.IsMaterialized &&
                   IsInconsistent == other.IsInconsistent &&
                   HasOperationalBinding == other.HasOperationalBinding &&
                   OperationalBindingState == other.OperationalBindingState &&
                   Intent == other.Intent &&
                   Classification == other.Classification &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsMaterializationSpecEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Role;
                hashCode = (hashCode * 397) ^ (int)OperationalRecipeKind;
                hashCode = (hashCode * 397) ^ (int)Relevance;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSpecId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SpawnArchetypeId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSetMemberId ?? string.Empty);
                hashCode = (hashCode * 397) ^ OccurrenceIndex;
                hashCode = (hashCode * 397) ^ (int)RealizationMode;
                hashCode = (hashCode * 397) ^ (int)ContinuityResetPolicy;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSetRef ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)PresenceStatus;
                hashCode = (hashCode * 397) ^ IsExpected.GetHashCode();
                hashCode = (hashCode * 397) ^ IsMaterialized.GetHashCode();
                hashCode = (hashCode * 397) ^ IsInconsistent.GetHashCode();
                hashCode = (hashCode * 397) ^ HasOperationalBinding.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)OperationalBindingState;
                hashCode = (hashCode * 397) ^ (int)Intent;
                hashCode = (hashCode * 397) ^ (int)Classification;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"kind='{Kind}', axisActorId='{AxisActorId}', runtimeActorId='{RuntimeActorId}', role='{Role}', operationalRecipeKind='{OperationalRecipeKind}', relevance='{Relevance}', semanticParticipantId='{AsText(SemanticParticipantId)}', actorSpecId='{AsText(ActorSpecId)}', spawnArchetypeId='{AsText(SpawnArchetypeId)}', actorSetMemberId='{AsText(ActorSetMemberId)}', occurrenceIndex='{OccurrenceIndex}', realizationMode='{RealizationMode}', continuityResetPolicy='{ContinuityResetPolicy}', actorSetRef='{AsText(ActorSetRef)}', presenceStatus='{PresenceStatus}', expected='{IsExpected}', materialized='{IsMaterialized}', inconsistent='{IsInconsistent}', hasBinding='{HasOperationalBinding}', bindingState='{OperationalBindingState}', intent='{Intent}', classification='{Classification}', reason='{AsText(Reason)}'";
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

    public readonly struct ActorsMaterializationPlanSnapshot : IEquatable<ActorsMaterializationPlanSnapshot>
    {
        public ActorsMaterializationPlanSnapshot(
            string ensembleSignature,
            string presenceSignature,
            string runtimeObservationSignature,
            string operationalBindingSignature,
            string planSignature,
            ActorsMaterializationSpecEntry[] entries,
            int keepCount,
            int materializeCount,
            int rematerializeCount,
            int observeWithoutActionCount,
            int inconsistentCount,
            int excessOrphanCount,
            string reason)
        {
            EnsembleSignature = Normalize(ensembleSignature);
            PresenceSignature = Normalize(presenceSignature);
            RuntimeObservationSignature = Normalize(runtimeObservationSignature);
            OperationalBindingSignature = Normalize(operationalBindingSignature);
            PlanSignature = Normalize(planSignature);
            Entries = entries == null ? Array.Empty<ActorsMaterializationSpecEntry>() : (ActorsMaterializationSpecEntry[])entries.Clone();
            KeepCount = Clamp(keepCount);
            MaterializeCount = Clamp(materializeCount);
            RematerializeCount = Clamp(rematerializeCount);
            ObserveWithoutActionCount = Clamp(observeWithoutActionCount);
            InconsistentCount = Clamp(inconsistentCount);
            ExcessOrphanCount = Clamp(excessOrphanCount);
            Reason = Normalize(reason);
        }

        public string EnsembleSignature { get; }
        public string PresenceSignature { get; }
        public string RuntimeObservationSignature { get; }
        public string OperationalBindingSignature { get; }
        public string PlanSignature { get; }
        public ActorsMaterializationSpecEntry[] Entries { get; }
        public int KeepCount { get; }
        public int MaterializeCount { get; }
        public int RematerializeCount { get; }
        public int ObserveWithoutActionCount { get; }
        public int InconsistentCount { get; }
        public int ExcessOrphanCount { get; }
        public string Reason { get; }

        public int Count => Entries?.Length ?? 0;
        public bool HasEntries => Count > 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(PlanSignature);

        public static ActorsMaterializationPlanSnapshot Empty => new(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<ActorsMaterializationSpecEntry>(),
            0,
            0,
            0,
            0,
            0,
            0,
            string.Empty);

        public bool Equals(ActorsMaterializationPlanSnapshot other)
        {
            return string.Equals(EnsembleSignature, other.EnsembleSignature, StringComparison.Ordinal) &&
                   string.Equals(PresenceSignature, other.PresenceSignature, StringComparison.Ordinal) &&
                   string.Equals(RuntimeObservationSignature, other.RuntimeObservationSignature, StringComparison.Ordinal) &&
                   string.Equals(OperationalBindingSignature, other.OperationalBindingSignature, StringComparison.Ordinal) &&
                   string.Equals(PlanSignature, other.PlanSignature, StringComparison.Ordinal) &&
                   KeepCount == other.KeepCount &&
                   MaterializeCount == other.MaterializeCount &&
                   RematerializeCount == other.RematerializeCount &&
                   ObserveWithoutActionCount == other.ObserveWithoutActionCount &&
                   InconsistentCount == other.InconsistentCount &&
                   ExcessOrphanCount == other.ExcessOrphanCount &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsMaterializationPlanSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(EnsembleSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PresenceSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RuntimeObservationSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(OperationalBindingSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PlanSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ KeepCount;
                hashCode = (hashCode * 397) ^ MaterializeCount;
                hashCode = (hashCode * 397) ^ RematerializeCount;
                hashCode = (hashCode * 397) ^ ObserveWithoutActionCount;
                hashCode = (hashCode * 397) ^ InconsistentCount;
                hashCode = (hashCode * 397) ^ ExcessOrphanCount;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ Count;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"planSignature='{AsText(PlanSignature)}', count='{Count}', keep='{KeepCount}', materialize='{MaterializeCount}', rematerialize='{RematerializeCount}', observeWithoutAction='{ObserveWithoutActionCount}', inconsistent='{InconsistentCount}', excessOrphan='{ExcessOrphanCount}', reason='{AsText(Reason)}'";
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value;
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
