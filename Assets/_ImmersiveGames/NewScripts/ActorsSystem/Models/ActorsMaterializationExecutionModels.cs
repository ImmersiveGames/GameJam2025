using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public enum ActorMaterializationExecutionDirective
    {
        Unknown = 0,
        NoActionStable = 1,
        NoActionObserve = 2,
        RequestMaterialize = 3,
        RequestRematerialize = 4,
        PreserveExisting = 5,
        FlagInconsistentNoAutoRemediation = 6,
        FlagRuntimeOrphanTolerated = 7,
        FlagRuntimeOrphanProblematic = 8
    }

    public readonly struct ActorsMaterializationExecutionEntry : IEquatable<ActorsMaterializationExecutionEntry>
    {
        public ActorsMaterializationExecutionEntry(
            ActorMaterializationSpecKind specKind,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            ActorRole role,
            ActorOperationalRecipeKind operationalRecipeKind,
            ActorMaterializationIntent intent,
            ActorMaterializationClassification classification,
            ActorMaterializationExecutionDirective directive,
            string semanticParticipantId,
            string actorSpecId,
            string spawnArchetypeId,
            string actorSetMemberId,
            int occurrenceIndex,
            ActorSpecRealizationMode realizationMode,
            ActorSpecContinuityResetPolicy continuityResetPolicy,
            string actorSetRef,
            string reason)
        {
            SpecKind = specKind;
            AxisActorId = axisActorId;
            RuntimeActorId = runtimeActorId;
            Role = role;
            OperationalRecipeKind = operationalRecipeKind;
            Intent = intent;
            Classification = classification;
            Directive = directive;
            SemanticParticipantId = string.IsNullOrWhiteSpace(semanticParticipantId) ? string.Empty : semanticParticipantId.Trim();
            ActorSpecId = string.IsNullOrWhiteSpace(actorSpecId) ? string.Empty : actorSpecId.Trim();
            SpawnArchetypeId = string.IsNullOrWhiteSpace(spawnArchetypeId) ? string.Empty : spawnArchetypeId.Trim();
            ActorSetMemberId = string.IsNullOrWhiteSpace(actorSetMemberId) ? string.Empty : actorSetMemberId.Trim();
            OccurrenceIndex = occurrenceIndex < 0 ? 0 : occurrenceIndex;
            RealizationMode = realizationMode;
            ContinuityResetPolicy = continuityResetPolicy;
            ActorSetRef = string.IsNullOrWhiteSpace(actorSetRef) ? string.Empty : actorSetRef.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public ActorMaterializationSpecKind SpecKind { get; }
        public AxisActorId AxisActorId { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public ActorRole Role { get; }
        public ActorOperationalRecipeKind OperationalRecipeKind { get; }
        public ActorMaterializationIntent Intent { get; }
        public ActorMaterializationClassification Classification { get; }
        public ActorMaterializationExecutionDirective Directive { get; }
        public string SemanticParticipantId { get; }
        public string ActorSpecId { get; }
        public string SpawnArchetypeId { get; }
        public string ActorSetMemberId { get; }
        public int OccurrenceIndex { get; }
        public ActorSpecRealizationMode RealizationMode { get; }
        public ActorSpecContinuityResetPolicy ContinuityResetPolicy { get; }
        public string ActorSetRef { get; }
        public string Reason { get; }

        public bool IsValid =>
            Directive != ActorMaterializationExecutionDirective.Unknown &&
            (SpecKind == ActorMaterializationSpecKind.AxisActor
                ? AxisActorId.IsValid &&
                  !string.IsNullOrWhiteSpace(ActorSpecId) &&
                  !string.IsNullOrWhiteSpace(SpawnArchetypeId) &&
                  !string.IsNullOrWhiteSpace(ActorSetMemberId) &&
                  OccurrenceIndex >= 0
                : RuntimeActorId.IsValid);

        public bool Equals(ActorsMaterializationExecutionEntry other)
        {
            return SpecKind == other.SpecKind &&
                   AxisActorId.Equals(other.AxisActorId) &&
                   RuntimeActorId.Equals(other.RuntimeActorId) &&
                   Role == other.Role &&
                   OperationalRecipeKind == other.OperationalRecipeKind &&
                   Intent == other.Intent &&
                   Classification == other.Classification &&
                   Directive == other.Directive &&
                   string.Equals(SemanticParticipantId, other.SemanticParticipantId, StringComparison.Ordinal) &&
                   string.Equals(ActorSpecId, other.ActorSpecId, StringComparison.Ordinal) &&
                   string.Equals(SpawnArchetypeId, other.SpawnArchetypeId, StringComparison.Ordinal) &&
                   string.Equals(ActorSetMemberId, other.ActorSetMemberId, StringComparison.Ordinal) &&
                   OccurrenceIndex == other.OccurrenceIndex &&
                   RealizationMode == other.RealizationMode &&
                   ContinuityResetPolicy == other.ContinuityResetPolicy &&
                   string.Equals(ActorSetRef, other.ActorSetRef, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsMaterializationExecutionEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)SpecKind;
                hashCode = (hashCode * 397) ^ AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Role;
                hashCode = (hashCode * 397) ^ (int)OperationalRecipeKind;
                hashCode = (hashCode * 397) ^ (int)Intent;
                hashCode = (hashCode * 397) ^ (int)Classification;
                hashCode = (hashCode * 397) ^ (int)Directive;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSpecId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SpawnArchetypeId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSetMemberId ?? string.Empty);
                hashCode = (hashCode * 397) ^ OccurrenceIndex;
                hashCode = (hashCode * 397) ^ (int)RealizationMode;
                hashCode = (hashCode * 397) ^ (int)ContinuityResetPolicy;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSetRef ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"specKind='{SpecKind}', axisActorId='{AxisActorId}', runtimeActorId='{RuntimeActorId}', role='{Role}', operationalRecipeKind='{OperationalRecipeKind}', intent='{Intent}', classification='{Classification}', directive='{Directive}', semanticParticipantId='{(string.IsNullOrWhiteSpace(SemanticParticipantId) ? "<none>" : SemanticParticipantId)}', actorSpecId='{(string.IsNullOrWhiteSpace(ActorSpecId) ? "<none>" : ActorSpecId)}', spawnArchetypeId='{(string.IsNullOrWhiteSpace(SpawnArchetypeId) ? "<none>" : SpawnArchetypeId)}', actorSetMemberId='{(string.IsNullOrWhiteSpace(ActorSetMemberId) ? "<none>" : ActorSetMemberId)}', occurrenceIndex='{OccurrenceIndex}', realizationMode='{RealizationMode}', continuityResetPolicy='{ContinuityResetPolicy}', actorSetRef='{(string.IsNullOrWhiteSpace(ActorSetRef) ? "<none>" : ActorSetRef)}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}'";
        }
    }

    public readonly struct ActorsMaterializationExecutionSnapshot : IEquatable<ActorsMaterializationExecutionSnapshot>
    {
        public ActorsMaterializationExecutionSnapshot(
            string planSignature,
            string executionSignature,
            ActorsMaterializationExecutionEntry[] entries,
            int noActionStableCount,
            int noActionObserveCount,
            int requestMaterializeCount,
            int requestRematerializeCount,
            int flagInconsistentCount,
            int flagRuntimeOrphanToleratedCount,
            int flagRuntimeOrphanProblematicCount,
            string reason)
        {
            PlanSignature = string.IsNullOrWhiteSpace(planSignature) ? string.Empty : planSignature.Trim();
            ExecutionSignature = string.IsNullOrWhiteSpace(executionSignature) ? string.Empty : executionSignature.Trim();
            Entries = entries == null ? Array.Empty<ActorsMaterializationExecutionEntry>() : (ActorsMaterializationExecutionEntry[])entries.Clone();
            NoActionStableCount = Clamp(noActionStableCount);
            NoActionObserveCount = Clamp(noActionObserveCount);
            RequestMaterializeCount = Clamp(requestMaterializeCount);
            RequestRematerializeCount = Clamp(requestRematerializeCount);
            FlagInconsistentCount = Clamp(flagInconsistentCount);
            FlagRuntimeOrphanToleratedCount = Clamp(flagRuntimeOrphanToleratedCount);
            FlagRuntimeOrphanProblematicCount = Clamp(flagRuntimeOrphanProblematicCount);
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public string PlanSignature { get; }
        public string ExecutionSignature { get; }
        public ActorsMaterializationExecutionEntry[] Entries { get; }
        public int NoActionStableCount { get; }
        public int NoActionObserveCount { get; }
        public int RequestMaterializeCount { get; }
        public int RequestRematerializeCount { get; }
        public int FlagInconsistentCount { get; }
        public int FlagRuntimeOrphanToleratedCount { get; }
        public int FlagRuntimeOrphanProblematicCount { get; }
        public string Reason { get; }

        public int Count => Entries?.Length ?? 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(ExecutionSignature);

        public static ActorsMaterializationExecutionSnapshot Empty => new(
            string.Empty,
            string.Empty,
            Array.Empty<ActorsMaterializationExecutionEntry>(),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            string.Empty);

        public bool Equals(ActorsMaterializationExecutionSnapshot other)
        {
            return string.Equals(PlanSignature, other.PlanSignature, StringComparison.Ordinal) &&
                   string.Equals(ExecutionSignature, other.ExecutionSignature, StringComparison.Ordinal) &&
                   NoActionStableCount == other.NoActionStableCount &&
                   NoActionObserveCount == other.NoActionObserveCount &&
                   RequestMaterializeCount == other.RequestMaterializeCount &&
                   RequestRematerializeCount == other.RequestRematerializeCount &&
                   FlagInconsistentCount == other.FlagInconsistentCount &&
                   FlagRuntimeOrphanToleratedCount == other.FlagRuntimeOrphanToleratedCount &&
                   FlagRuntimeOrphanProblematicCount == other.FlagRuntimeOrphanProblematicCount &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsMaterializationExecutionSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(PlanSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ExecutionSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ NoActionStableCount;
                hashCode = (hashCode * 397) ^ NoActionObserveCount;
                hashCode = (hashCode * 397) ^ RequestMaterializeCount;
                hashCode = (hashCode * 397) ^ RequestRematerializeCount;
                hashCode = (hashCode * 397) ^ FlagInconsistentCount;
                hashCode = (hashCode * 397) ^ FlagRuntimeOrphanToleratedCount;
                hashCode = (hashCode * 397) ^ FlagRuntimeOrphanProblematicCount;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ Count;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"executionSignature='{(string.IsNullOrWhiteSpace(ExecutionSignature) ? "<none>" : ExecutionSignature)}', count='{Count}', noActionStable='{NoActionStableCount}', noActionObserve='{NoActionObserveCount}', requestMaterialize='{RequestMaterializeCount}', requestRematerialize='{RequestRematerializeCount}', flagInconsistent='{FlagInconsistentCount}', orphanTolerated='{FlagRuntimeOrphanToleratedCount}', orphanProblematic='{FlagRuntimeOrphanProblematicCount}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}'";
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
