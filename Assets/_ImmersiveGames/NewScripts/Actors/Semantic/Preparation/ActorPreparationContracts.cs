using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Preparation
{
    public enum ActorPreparationOutcome
    {
        Unknown = 0,
        ObservedNoOp = 1,
        PlannedOnly = 2,
    }

    public enum ActorParticipationKind
    {
        Unknown = 0,
        NoActors = 1,
        ActivityEntryWithoutActorSet = 2,
        ActorSetExpected = 3,
        PlayerExpected = 4,
    }

    public readonly struct ActorSetEntry
    {
        public ActorSetEntry(string actorId, bool required, bool hasPrefabReference, ActorPlacementMode placementMode, bool hasPlacementPlan)
        {
            ActorId = Normalize(actorId);
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
        }

        public string ActorId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(ActorId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum ActorPreparationEntryStatus
    {
        Unknown = 0,
        PlannedOnly = 1,
    }

    public readonly struct ActorPlannedEntry
    {
        public ActorPlannedEntry(string actorId, bool required, bool hasPrefabReference, ActorPlacementMode placementMode, bool hasPlacementPlan, ActorPreparationEntryStatus status)
        {
            ActorId = Normalize(actorId);
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
            Status = status;
        }

        public string ActorId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public ActorPreparationEntryStatus Status { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(ActorId) && Status != ActorPreparationEntryStatus.Unknown;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum ActorMaterializationStatus
    {
        Unknown = 0,
        NotMaterialized = 1,
    }

    public readonly struct ActorMaterializationEntry
    {
        public ActorMaterializationEntry(
            string actorId,
            bool required,
            bool hasPrefabReference,
            ActorPlacementMode placementMode,
            bool hasPlacementPlan,
            ActorPreparationEntryStatus preparationStatus,
            ActorMaterializationStatus materializationStatus)
        {
            ActorId = Normalize(actorId);
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
            PreparationStatus = preparationStatus;
            MaterializationStatus = materializationStatus;
        }

        public string ActorId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public ActorPreparationEntryStatus PreparationStatus { get; }
        public ActorMaterializationStatus MaterializationStatus { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ActorId) &&
            PreparationStatus == ActorPreparationEntryStatus.PlannedOnly &&
            MaterializationStatus == ActorMaterializationStatus.NotMaterialized;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum ActorReadinessStatus
    {
        Unknown = 0,
        PendingMaterialization = 1,
        OptionalPending = 2,
    }

    public readonly struct ActorReadinessEntry
    {
        public ActorReadinessEntry(
            string actorId,
            bool required,
            bool hasPrefabReference,
            ActorPlacementMode placementMode,
            bool hasPlacementPlan,
            ActorPreparationEntryStatus preparationStatus,
            ActorMaterializationStatus materializationStatus,
            ActorReadinessStatus readinessStatus)
        {
            ActorId = Normalize(actorId);
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
            PreparationStatus = preparationStatus;
            MaterializationStatus = materializationStatus;
            ReadinessStatus = readinessStatus;
        }

        public string ActorId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public ActorPreparationEntryStatus PreparationStatus { get; }
        public ActorMaterializationStatus MaterializationStatus { get; }
        public ActorReadinessStatus ReadinessStatus { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ActorId) &&
            PreparationStatus == ActorPreparationEntryStatus.PlannedOnly &&
            MaterializationStatus == ActorMaterializationStatus.NotMaterialized &&
            ((Required && ReadinessStatus == ActorReadinessStatus.PendingMaterialization) ||
             (!Required && ReadinessStatus == ActorReadinessStatus.OptionalPending));

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActorSet
    {
        public ActorSet(IReadOnlyList<ActorSetEntry> entries)
        {
            Entries = entries ?? Array.Empty<ActorSetEntry>();
        }

        public IReadOnlyList<ActorSetEntry> Entries { get; }
        public int PlannedActorsCount => Entries?.Count ?? 0;
        public int RequiredActorsCount => CountRequired(Entries);
        public int OptionalActorsCount => PlannedActorsCount - RequiredActorsCount;
        public bool IsEmpty => PlannedActorsCount == 0;
        public bool IsValid => Entries != null && AreEntriesValid(Entries);
        public static ActorSet Empty => new(Array.Empty<ActorSetEntry>());

        private static bool AreEntriesValid(IReadOnlyList<ActorSetEntry> entries)
        {
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountRequired(IReadOnlyList<ActorSetEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Required)
                {
                    count += 1;
                }
            }

            return count;
        }
    }

    public readonly struct ActorPreparationIdentity
    {
        public ActorPreparationIdentity(
            string pipelineId,
            string sessionId,
            string routeIdentity,
            int routeSequence,
            string transitionId)
        {
            PipelineId = Normalize(pipelineId);
            SessionId = Normalize(sessionId);
            RouteIdentity = Normalize(routeIdentity);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            TransitionId = Normalize(transitionId);
        }

        public string PipelineId { get; }
        public string SessionId { get; }
        public string RouteIdentity { get; }
        public int RouteSequence { get; }
        public string TransitionId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionId) &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(TransitionId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActorPreparationPlan
    {
        public ActorPreparationPlan(
            ActorPreparationIdentity identity,
            bool expectsSessionActivityEntry,
            ActorSet actorSet,
            string source,
            string reason)
        {
            Identity = identity;
            ExpectsSessionActivityEntry = expectsSessionActivityEntry;
            ActorSet = actorSet.IsValid ? actorSet : ActorSet.Empty;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorPreparationIdentity Identity { get; }
        public bool ExpectsSessionActivityEntry { get; }
        public ActorSet ActorSet { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ActorSet.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActorPreparationSnapshot
    {
        public ActorPreparationSnapshot(
            ActorPreparationIdentity identity,
            ActorPreparationOutcome outcome,
            ActorParticipationKind participationKind,
            ActorSet actorSet,
            IReadOnlyList<ActorPlannedEntry> plannedEntries,
            IReadOnlyList<ActorMaterializationEntry> materializationEntries,
            IReadOnlyList<ActorReadinessEntry> readinessEntries,
            string message)
        {
            Identity = identity;
            Outcome = outcome;
            ParticipationKind = participationKind;
            ActorSet = actorSet.IsValid ? actorSet : ActorSet.Empty;
            PlannedEntries = plannedEntries ?? Array.Empty<ActorPlannedEntry>();
            MaterializationEntries = materializationEntries ?? Array.Empty<ActorMaterializationEntry>();
            ReadinessEntries = readinessEntries ?? Array.Empty<ActorReadinessEntry>();
            Message = Normalize(message);
        }

        public ActorPreparationIdentity Identity { get; }
        public ActorPreparationOutcome Outcome { get; }
        public ActorParticipationKind ParticipationKind { get; }
        public ActorSet ActorSet { get; }
        public IReadOnlyList<ActorPlannedEntry> PlannedEntries { get; }
        public IReadOnlyList<ActorMaterializationEntry> MaterializationEntries { get; }
        public IReadOnlyList<ActorReadinessEntry> ReadinessEntries { get; }
        public int PlannedActorsCount => ActorSet.PlannedActorsCount;
        public int RequiredActorsCount => ActorSet.RequiredActorsCount;
        public int OptionalActorsCount => ActorSet.OptionalActorsCount;
        public int NotMaterializedActorsCount => CountNotMaterialized(MaterializationEntries);
        public int PendingRequiredActorsCount => CountByReadiness(ReadinessEntries, ActorReadinessStatus.PendingMaterialization);
        public int PendingOptionalActorsCount => CountByReadiness(ReadinessEntries, ActorReadinessStatus.OptionalPending);
        public int ActorsWithPrefabCount => CountByPrefab(PlannedEntries, true);
        public int ActorsWithoutPrefabCount => CountByPrefab(PlannedEntries, false);
        public int ActorsWithPlacementCount => CountByPlacement(PlannedEntries, true);
        public int ActorsWithoutPlacementCount => CountByPlacement(PlannedEntries, false);
        public string Message { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Outcome != ActorPreparationOutcome.Unknown &&
            ParticipationKind != ActorParticipationKind.Unknown &&
            ActorSet.IsValid &&
            ArePlannedEntriesValid(PlannedEntries) &&
            AreMaterializationEntriesValid(MaterializationEntries) &&
            AreOutcomeCountsConsistent(
                Outcome,
                PlannedEntries,
                MaterializationEntries,
                ReadinessEntries,
                PlannedActorsCount,
                RequiredActorsCount,
                OptionalActorsCount,
                NotMaterializedActorsCount,
                PendingRequiredActorsCount,
                PendingOptionalActorsCount) &&
            AreReadinessEntriesValid(ReadinessEntries) &&
            !string.IsNullOrWhiteSpace(Message);

        private static bool ArePlannedEntriesValid(IReadOnlyList<ActorPlannedEntry> plannedEntries)
        {
            if (plannedEntries == null)
            {
                return false;
            }

            for (int i = 0; i < plannedEntries.Count; i++)
            {
                if (!plannedEntries[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountByPrefab(IReadOnlyList<ActorPlannedEntry> entries, bool hasPrefab)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].HasPrefabReference == hasPrefab)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static int CountByPlacement(IReadOnlyList<ActorPlannedEntry> entries, bool hasPlacement)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].HasPlacementPlan == hasPlacement)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool AreMaterializationEntriesValid(IReadOnlyList<ActorMaterializationEntry> entries)
        {
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountNotMaterialized(IReadOnlyList<ActorMaterializationEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].MaterializationStatus == ActorMaterializationStatus.NotMaterialized)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static bool AreReadinessEntriesValid(IReadOnlyList<ActorReadinessEntry> entries)
        {
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (!entries[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountByReadiness(IReadOnlyList<ActorReadinessEntry> entries, ActorReadinessStatus target)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].ReadinessStatus == target)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static bool AreOutcomeCountsConsistent(
            ActorPreparationOutcome outcome,
            IReadOnlyList<ActorPlannedEntry> plannedEntries,
            IReadOnlyList<ActorMaterializationEntry> materializationEntries,
            IReadOnlyList<ActorReadinessEntry> readinessEntries,
            int plannedActorsCount,
            int requiredActorsCount,
            int optionalActorsCount,
            int notMaterializedActorsCount,
            int pendingRequiredActorsCount,
            int pendingOptionalActorsCount)
        {
            int plannedCount = plannedEntries?.Count ?? 0;
            int materializationCount = materializationEntries?.Count ?? 0;
            int readinessCount = readinessEntries?.Count ?? 0;

            if (outcome == ActorPreparationOutcome.ObservedNoOp)
            {
                return plannedActorsCount == 0 &&
                       plannedCount == 0 &&
                       materializationCount == 0 &&
                       readinessCount == 0 &&
                       requiredActorsCount == 0 &&
                       optionalActorsCount == 0 &&
                       notMaterializedActorsCount == 0 &&
                       pendingRequiredActorsCount == 0 &&
                       pendingOptionalActorsCount == 0;
            }

            if (outcome == ActorPreparationOutcome.PlannedOnly)
            {
                return plannedActorsCount > 0 &&
                       plannedCount == plannedActorsCount &&
                       materializationCount == plannedActorsCount &&
                       readinessCount == plannedActorsCount &&
                       requiredActorsCount + optionalActorsCount == plannedActorsCount &&
                       notMaterializedActorsCount == plannedActorsCount &&
                       pendingRequiredActorsCount == requiredActorsCount &&
                       pendingOptionalActorsCount == optionalActorsCount;
            }

            return false;
        }
    }

    public readonly struct ActorPreparationResult
    {
        public ActorPreparationResult(ActorPreparationPlan plan, ActorPreparationSnapshot snapshot)
        {
            Plan = plan;
            Snapshot = snapshot;
        }

        public ActorPreparationPlan Plan { get; }
        public ActorPreparationSnapshot Snapshot { get; }
        public IReadOnlyList<ActorPlannedEntry> PlannedEntries => Snapshot.PlannedEntries;
        public IReadOnlyList<ActorMaterializationEntry> MaterializationEntries => Snapshot.MaterializationEntries;
        public IReadOnlyList<ActorReadinessEntry> ReadinessEntries => Snapshot.ReadinessEntries;
        public bool IsObservedNoOp =>
            Snapshot.Outcome == ActorPreparationOutcome.ObservedNoOp ||
            Snapshot.Outcome == ActorPreparationOutcome.PlannedOnly;
        public bool IsPlannedOnly => Snapshot.Outcome == ActorPreparationOutcome.PlannedOnly;
        public bool IsPlannedOrNoOp => IsObservedNoOp || IsPlannedOnly;

        public bool IsValid =>
            Plan.IsValid &&
            Snapshot.IsValid;
    }
}
