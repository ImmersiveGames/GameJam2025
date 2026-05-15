using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Preparation
{
    public enum PlayerPreparationOutcome
    {
        Unknown = 0,
        ObservedNoOp = 1,
        PlannedOnly = 2,
        Materialized = 3,
    }

    public enum PlayerParticipationKind
    {
        Unknown = 0,
        NoPlayers = 1,
        ActivityEntryWithoutPlayerSet = 2,
        PlayerSetExpected = 3,
    }

    public readonly struct PlayerSetEntry
    {
        public PlayerSetEntry(string playerId, bool required, bool hasPrefabReference, ActorPlacementMode placementMode, bool hasPlacementPlan)
        {
            PlayerId = Normalize(playerId);
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
        }

        public string PlayerId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum PlayerPreparationEntryStatus
    {
        Unknown = 0,
        PlannedOnly = 1,
        Materialized = 2,
        Skipped = 3,
    }

    public readonly struct PlayerPlannedEntry
    {
        public PlayerPlannedEntry(string playerId, bool required, bool hasPrefabReference, ActorPlacementMode placementMode, bool hasPlacementPlan, PlayerPreparationEntryStatus status)
        {
            PlayerId = Normalize(playerId);
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
            Status = status;
        }

        public string PlayerId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public PlayerPreparationEntryStatus Status { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId) && Status != PlayerPreparationEntryStatus.Unknown;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum PlayerMaterializationStatus
    {
        Unknown = 0,
        NotMaterialized = 1,
        Materialized = 2,
        Skipped = 3,
    }

    public readonly struct PlayerMaterializationEntry
    {
        public PlayerMaterializationEntry(
            string playerId,
            bool required,
            bool hasPrefabReference,
            ActorPlacementMode placementMode,
            bool hasPlacementPlan,
            PlayerPreparationEntryStatus preparationStatus,
            PlayerMaterializationStatus materializationStatus,
            string runtimeName,
            string runtimeSceneName)
        {
            PlayerId = Normalize(playerId);
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
            PreparationStatus = preparationStatus;
            MaterializationStatus = materializationStatus;
            RuntimeName = Normalize(runtimeName);
            RuntimeSceneName = Normalize(runtimeSceneName);
        }

        public string PlayerId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public PlayerPreparationEntryStatus PreparationStatus { get; }
        public PlayerMaterializationStatus MaterializationStatus { get; }
        public string RuntimeName { get; }
        public string RuntimeSceneName { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PlayerId) &&
            ((PreparationStatus == PlayerPreparationEntryStatus.PlannedOnly && MaterializationStatus == PlayerMaterializationStatus.NotMaterialized) ||
             (PreparationStatus == PlayerPreparationEntryStatus.Materialized && MaterializationStatus == PlayerMaterializationStatus.Materialized) ||
             (PreparationStatus == PlayerPreparationEntryStatus.Skipped && MaterializationStatus == PlayerMaterializationStatus.Skipped));

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum PlayerReadinessStatus
    {
        Unknown = 0,
        PendingMaterialization = 1,
        OptionalPending = 2,
        Ready = 3,
        OptionalSkipped = 4,
    }

    public readonly struct PlayerReadinessEntry
    {
        public PlayerReadinessEntry(
            string playerId,
            bool required,
            bool hasPrefabReference,
            ActorPlacementMode placementMode,
            bool hasPlacementPlan,
            PlayerPreparationEntryStatus preparationStatus,
            PlayerMaterializationStatus materializationStatus,
            PlayerReadinessStatus readinessStatus)
        {
            PlayerId = Normalize(playerId);
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
            PreparationStatus = preparationStatus;
            MaterializationStatus = materializationStatus;
            ReadinessStatus = readinessStatus;
        }

        public string PlayerId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public PlayerPreparationEntryStatus PreparationStatus { get; }
        public PlayerMaterializationStatus MaterializationStatus { get; }
        public PlayerReadinessStatus ReadinessStatus { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PlayerId) &&
            ((PreparationStatus == PlayerPreparationEntryStatus.PlannedOnly &&
              MaterializationStatus == PlayerMaterializationStatus.NotMaterialized &&
              ((Required && ReadinessStatus == PlayerReadinessStatus.PendingMaterialization) ||
               (!Required && ReadinessStatus == PlayerReadinessStatus.OptionalPending))) ||
             (PreparationStatus == PlayerPreparationEntryStatus.Materialized &&
              MaterializationStatus == PlayerMaterializationStatus.Materialized &&
              ReadinessStatus == PlayerReadinessStatus.Ready) ||
             (!Required &&
              PreparationStatus == PlayerPreparationEntryStatus.Skipped &&
              MaterializationStatus == PlayerMaterializationStatus.Skipped &&
              ReadinessStatus == PlayerReadinessStatus.OptionalSkipped));

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct PlayerMaterializationRecord
    {
        public PlayerMaterializationRecord(
            string playerId,
            bool required,
            bool hasPrefabReference,
            PlayerMaterializationStatus materializationStatus,
            string runtimeName,
            string runtimeSceneName)
        {
            PlayerId = Normalize(playerId);
            Required = required;
            HasPrefabReference = hasPrefabReference;
            MaterializationStatus = materializationStatus;
            RuntimeName = Normalize(runtimeName);
            RuntimeSceneName = Normalize(runtimeSceneName);
        }

        public string PlayerId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public PlayerMaterializationStatus MaterializationStatus { get; }
        public string RuntimeName { get; }
        public string RuntimeSceneName { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId) && MaterializationStatus != PlayerMaterializationStatus.Unknown;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct PlayerSet
    {
        public PlayerSet(IReadOnlyList<PlayerSetEntry> entries)
        {
            Entries = entries ?? Array.Empty<PlayerSetEntry>();
        }

        public IReadOnlyList<PlayerSetEntry> Entries { get; }
        public int PlannedPlayersCount => Entries?.Count ?? 0;
        public int RequiredPlayersCount => CountRequired(Entries);
        public int OptionalPlayersCount => PlannedPlayersCount - RequiredPlayersCount;
        public bool IsEmpty => PlannedPlayersCount == 0;
        public bool IsValid => Entries != null && AreEntriesValid(Entries);
        public static PlayerSet Empty => new(Array.Empty<PlayerSetEntry>());

        private static bool AreEntriesValid(IReadOnlyList<PlayerSetEntry> entries)
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

        private static int CountRequired(IReadOnlyList<PlayerSetEntry> entries)
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

    public readonly struct PlayerPreparationIdentity
    {
        public PlayerPreparationIdentity(
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

    public readonly struct PlayerPreparationPlan
    {
        public PlayerPreparationPlan(
            PlayerPreparationIdentity identity,
            bool expectsSessionActivityEntry,
            PlayerSet playerSet,
            string source,
            string reason)
        {
            Identity = identity;
            ExpectsSessionActivityEntry = expectsSessionActivityEntry;
            PlayerSet = playerSet.IsValid ? playerSet : PlayerSet.Empty;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public PlayerPreparationIdentity Identity { get; }
        public bool ExpectsSessionActivityEntry { get; }
        public PlayerSet PlayerSet { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            PlayerSet.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct PlayerPreparationSnapshot
    {
        public PlayerPreparationSnapshot(
            PlayerPreparationIdentity identity,
            PlayerPreparationOutcome outcome,
            PlayerParticipationKind participationKind,
            PlayerSet playerSet,
            IReadOnlyList<PlayerPlannedEntry> plannedEntries,
            IReadOnlyList<PlayerMaterializationEntry> materializationEntries,
            IReadOnlyList<PlayerReadinessEntry> readinessEntries,
            string message)
        {
            Identity = identity;
            Outcome = outcome;
            ParticipationKind = participationKind;
            PlayerSet = playerSet.IsValid ? playerSet : PlayerSet.Empty;
            PlannedEntries = plannedEntries ?? Array.Empty<PlayerPlannedEntry>();
            MaterializationEntries = materializationEntries ?? Array.Empty<PlayerMaterializationEntry>();
            ReadinessEntries = readinessEntries ?? Array.Empty<PlayerReadinessEntry>();
            Message = Normalize(message);
        }

        public PlayerPreparationIdentity Identity { get; }
        public PlayerPreparationOutcome Outcome { get; }
        public PlayerParticipationKind ParticipationKind { get; }
        public PlayerSet PlayerSet { get; }
        public IReadOnlyList<PlayerPlannedEntry> PlannedEntries { get; }
        public IReadOnlyList<PlayerMaterializationEntry> MaterializationEntries { get; }
        public IReadOnlyList<PlayerReadinessEntry> ReadinessEntries { get; }
        public int PlannedPlayersCount => PlayerSet.PlannedPlayersCount;
        public int RequiredPlayersCount => PlayerSet.RequiredPlayersCount;
        public int OptionalPlayersCount => PlayerSet.OptionalPlayersCount;
        public int NotMaterializedPlayersCount => CountNotMaterialized(MaterializationEntries);
        public int MaterializedPlayersCount => CountByMaterialization(MaterializationEntries, PlayerMaterializationStatus.Materialized);
        public int SkippedPlayersCount => CountByMaterialization(MaterializationEntries, PlayerMaterializationStatus.Skipped);
        public int PendingRequiredPlayersCount => CountByReadiness(ReadinessEntries, PlayerReadinessStatus.PendingMaterialization);
        public int PendingOptionalPlayersCount => CountByReadiness(ReadinessEntries, PlayerReadinessStatus.OptionalPending);
        public int PlayersWithPrefabCount => CountByPrefab(PlannedEntries, true);
        public int PlayersWithoutPrefabCount => CountByPrefab(PlannedEntries, false);
        public int PlayersWithPlacementCount => CountByPlacement(PlannedEntries, true);
        public int PlayersWithoutPlacementCount => CountByPlacement(PlannedEntries, false);
        public string Message { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Outcome != PlayerPreparationOutcome.Unknown &&
            ParticipationKind != PlayerParticipationKind.Unknown &&
            PlayerSet.IsValid &&
            ArePlannedEntriesValid(PlannedEntries) &&
            AreMaterializationEntriesValid(MaterializationEntries) &&
            AreOutcomeCountsConsistent(
                Outcome,
                PlannedEntries,
                MaterializationEntries,
                ReadinessEntries,
                PlannedPlayersCount,
                RequiredPlayersCount,
                OptionalPlayersCount,
                NotMaterializedPlayersCount,
                PendingRequiredPlayersCount,
                PendingOptionalPlayersCount) &&
            AreReadinessEntriesValid(ReadinessEntries) &&
            !string.IsNullOrWhiteSpace(Message);

        private static bool ArePlannedEntriesValid(IReadOnlyList<PlayerPlannedEntry> plannedEntries)
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

        private static int CountByPrefab(IReadOnlyList<PlayerPlannedEntry> entries, bool hasPrefab)
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

        private static int CountByPlacement(IReadOnlyList<PlayerPlannedEntry> entries, bool hasPlacement)
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

        private static bool AreMaterializationEntriesValid(IReadOnlyList<PlayerMaterializationEntry> entries)
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

        private static int CountNotMaterialized(IReadOnlyList<PlayerMaterializationEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].MaterializationStatus == PlayerMaterializationStatus.NotMaterialized)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static int CountByMaterialization(IReadOnlyList<PlayerMaterializationEntry> entries, PlayerMaterializationStatus status)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].MaterializationStatus == status)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static bool AreReadinessEntriesValid(IReadOnlyList<PlayerReadinessEntry> entries)
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

        private static int CountByReadiness(IReadOnlyList<PlayerReadinessEntry> entries, PlayerReadinessStatus target)
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
            PlayerPreparationOutcome outcome,
            IReadOnlyList<PlayerPlannedEntry> plannedEntries,
            IReadOnlyList<PlayerMaterializationEntry> materializationEntries,
            IReadOnlyList<PlayerReadinessEntry> readinessEntries,
            int plannedPlayersCount,
            int requiredPlayersCount,
            int optionalPlayersCount,
            int notMaterializedPlayersCount,
            int pendingRequiredPlayersCount,
            int pendingOptionalPlayersCount)
        {
            int plannedCount = plannedEntries?.Count ?? 0;
            int materializationCount = materializationEntries?.Count ?? 0;
            int readinessCount = readinessEntries?.Count ?? 0;

            if (outcome == PlayerPreparationOutcome.ObservedNoOp)
            {
                return plannedPlayersCount == 0 &&
                       plannedCount == 0 &&
                       materializationCount == 0 &&
                       readinessCount == 0 &&
                       requiredPlayersCount == 0 &&
                       optionalPlayersCount == 0 &&
                       notMaterializedPlayersCount == 0 &&
                       pendingRequiredPlayersCount == 0 &&
                       pendingOptionalPlayersCount == 0;
            }

            if (outcome == PlayerPreparationOutcome.PlannedOnly)
            {
                return plannedPlayersCount > 0 &&
                       plannedCount == plannedPlayersCount &&
                       materializationCount == plannedPlayersCount &&
                       readinessCount == plannedPlayersCount &&
                       requiredPlayersCount + optionalPlayersCount == plannedPlayersCount &&
                       notMaterializedPlayersCount == plannedPlayersCount &&
                       pendingRequiredPlayersCount == requiredPlayersCount &&
                       pendingOptionalPlayersCount == optionalPlayersCount;
            }

            if (outcome == PlayerPreparationOutcome.Materialized)
            {
                return plannedPlayersCount > 0 &&
                       plannedCount == plannedPlayersCount &&
                       materializationCount == plannedPlayersCount &&
                       readinessCount == plannedPlayersCount &&
                       requiredPlayersCount + optionalPlayersCount == plannedPlayersCount &&
                       pendingRequiredPlayersCount == 0;
            }

            return false;
        }
    }

    public readonly struct PlayerPreparationResult
    {
        public PlayerPreparationResult(PlayerPreparationPlan plan, PlayerPreparationSnapshot snapshot)
        {
            Plan = plan;
            Snapshot = snapshot;
        }

        public PlayerPreparationPlan Plan { get; }
        public PlayerPreparationSnapshot Snapshot { get; }
        public IReadOnlyList<PlayerPlannedEntry> PlannedEntries => Snapshot.PlannedEntries;
        public IReadOnlyList<PlayerMaterializationEntry> MaterializationEntries => Snapshot.MaterializationEntries;
        public IReadOnlyList<PlayerReadinessEntry> ReadinessEntries => Snapshot.ReadinessEntries;
        public bool IsObservedNoOp => Snapshot.Outcome == PlayerPreparationOutcome.ObservedNoOp;
        public bool IsPlannedOnly => Snapshot.Outcome == PlayerPreparationOutcome.PlannedOnly;
        public bool IsMaterialized => Snapshot.Outcome == PlayerPreparationOutcome.Materialized;
        public bool IsPlannedOrNoOp => IsObservedNoOp || IsPlannedOnly;

        public bool IsValid =>
            Plan.IsValid &&
            Snapshot.IsValid;
    }
}
