using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Participation
{
    public enum PlayerParticipationOutcome
    {
        Unknown = 0,
        ObservedNoOp = 1,
        SeedResolved = 2,
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
        public PlayerSetEntry(
            PlayerSlotId playerSlotId,
            PlayerSelectionId playerSelectionId,
            ActorDefinitionId actorDefinitionId,
            ActorId actorId,
            bool required,
            bool hasPrefabReference,
            ActorPlacementMode placementMode,
            bool hasPlacementPlan)
        {
            PlayerSlotId = playerSlotId;
            PlayerSelectionId = playerSelectionId;
            ActorDefinitionId = actorDefinitionId;
            ActorId = actorId;
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
        }

        public PlayerSlotId PlayerSlotId { get; }
        public PlayerSelectionId PlayerSelectionId { get; }
        public ActorDefinitionId ActorDefinitionId { get; }
        public ActorId ActorId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public bool IsValid =>
            PlayerSlotId.IsValid &&
            PlayerSelectionId.IsValid &&
            ActorDefinitionId.IsValid &&
            ActorId.IsValid;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public enum PlayerParticipationSeedEntryStatus
    {
        Unknown = 0,
        SeedResolved = 1,
        Materialized = 2,
        Skipped = 3,
    }

    public readonly struct PlayerParticipationSeedEntry
    {
        public PlayerParticipationSeedEntry(
            PlayerSlotId playerSlotId,
            PlayerSelectionId playerSelectionId,
            ActorDefinitionId actorDefinitionId,
            ActorId actorId,
            ActorScope actorScope,
            bool required,
            bool hasPrefabReference,
            ActorPlacementMode placementMode,
            bool hasPlacementPlan,
            PlayerParticipationSeedEntryStatus status)
        {
            PlayerSlotId = playerSlotId;
            PlayerSelectionId = playerSelectionId;
            ActorDefinitionId = actorDefinitionId;
            ActorId = actorId;
            ActorScope = actorScope;
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
            Status = status;
        }

        public PlayerSlotId PlayerSlotId { get; }
        public PlayerSelectionId PlayerSelectionId { get; }
        public ActorDefinitionId ActorDefinitionId { get; }
        public ActorId ActorId { get; }
        public ActorScope ActorScope { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public PlayerParticipationSeedEntryStatus Status { get; }
        public bool IsValid =>
            PlayerSlotId.IsValid &&
            PlayerSelectionId.IsValid &&
            ActorDefinitionId.IsValid &&
            ActorId.IsValid &&
            ActorScope != ActorScope.Unknown &&
            Status != PlayerParticipationSeedEntryStatus.Unknown;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
            PlayerSlotId playerSlotId,
            PlayerSelectionId playerSelectionId,
            ActorDefinitionId actorDefinitionId,
            ActorId actorId,
            bool required,
            bool hasPrefabReference,
            ActorPlacementMode placementMode,
            bool hasPlacementPlan,
            PlayerParticipationSeedEntryStatus participationStatus,
            PlayerMaterializationStatus materializationStatus,
            string runtimeName,
            string runtimeSceneName)
        {
            PlayerSlotId = playerSlotId;
            PlayerSelectionId = playerSelectionId;
            ActorDefinitionId = actorDefinitionId;
            ActorId = actorId;
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
            ParticipationStatus = participationStatus;
            MaterializationStatus = materializationStatus;
            RuntimeName = Normalize(runtimeName);
            RuntimeSceneName = Normalize(runtimeSceneName);
        }

        public PlayerSlotId PlayerSlotId { get; }
        public PlayerSelectionId PlayerSelectionId { get; }
        public ActorDefinitionId ActorDefinitionId { get; }
        public ActorId ActorId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public PlayerParticipationSeedEntryStatus ParticipationStatus { get; }
        public PlayerMaterializationStatus MaterializationStatus { get; }
        public string RuntimeName { get; }
        public string RuntimeSceneName { get; }

        public bool IsValid =>
            PlayerSlotId.IsValid &&
            PlayerSelectionId.IsValid &&
            ActorDefinitionId.IsValid &&
            ActorId.IsValid &&
            ((ParticipationStatus == PlayerParticipationSeedEntryStatus.SeedResolved && MaterializationStatus == PlayerMaterializationStatus.NotMaterialized) ||
             (ParticipationStatus == PlayerParticipationSeedEntryStatus.Materialized && MaterializationStatus == PlayerMaterializationStatus.Materialized) ||
             (ParticipationStatus == PlayerParticipationSeedEntryStatus.Skipped && MaterializationStatus == PlayerMaterializationStatus.Skipped));

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
            PlayerSlotId playerSlotId,
            PlayerSelectionId playerSelectionId,
            ActorDefinitionId actorDefinitionId,
            ActorId actorId,
            bool required,
            bool hasPrefabReference,
            ActorPlacementMode placementMode,
            bool hasPlacementPlan,
            PlayerParticipationSeedEntryStatus participationStatus,
            PlayerMaterializationStatus materializationStatus,
            PlayerReadinessStatus readinessStatus)
        {
            PlayerSlotId = playerSlotId;
            PlayerSelectionId = playerSelectionId;
            ActorDefinitionId = actorDefinitionId;
            ActorId = actorId;
            Required = required;
            HasPrefabReference = hasPrefabReference;
            PlacementMode = placementMode;
            HasPlacementPlan = hasPlacementPlan;
            ParticipationStatus = participationStatus;
            MaterializationStatus = materializationStatus;
            ReadinessStatus = readinessStatus;
        }

        public PlayerSlotId PlayerSlotId { get; }
        public PlayerSelectionId PlayerSelectionId { get; }
        public ActorDefinitionId ActorDefinitionId { get; }
        public ActorId ActorId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public ActorPlacementMode PlacementMode { get; }
        public bool HasPlacementPlan { get; }
        public PlayerParticipationSeedEntryStatus ParticipationStatus { get; }
        public PlayerMaterializationStatus MaterializationStatus { get; }
        public PlayerReadinessStatus ReadinessStatus { get; }

        public bool IsValid =>
            PlayerSlotId.IsValid &&
            PlayerSelectionId.IsValid &&
            ActorDefinitionId.IsValid &&
            ActorId.IsValid &&
            ((ParticipationStatus == PlayerParticipationSeedEntryStatus.SeedResolved &&
              MaterializationStatus == PlayerMaterializationStatus.NotMaterialized &&
              ((Required && ReadinessStatus == PlayerReadinessStatus.PendingMaterialization) ||
               (!Required && ReadinessStatus == PlayerReadinessStatus.OptionalPending))) ||
             (ParticipationStatus == PlayerParticipationSeedEntryStatus.Materialized &&
              MaterializationStatus == PlayerMaterializationStatus.Materialized &&
              ReadinessStatus == PlayerReadinessStatus.Ready) ||
             (!Required &&
              ParticipationStatus == PlayerParticipationSeedEntryStatus.Skipped &&
              MaterializationStatus == PlayerMaterializationStatus.Skipped &&
              ReadinessStatus == PlayerReadinessStatus.OptionalSkipped));

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerMaterializationRecord
    {
        public PlayerMaterializationRecord(
            ActorId actorId,
            bool required,
            bool hasPrefabReference,
            PlayerMaterializationStatus materializationStatus,
            string runtimeName,
            string runtimeSceneName)
        {
            ActorId = actorId;
            Required = required;
            HasPrefabReference = hasPrefabReference;
            MaterializationStatus = materializationStatus;
            RuntimeName = Normalize(runtimeName);
            RuntimeSceneName = Normalize(runtimeSceneName);
        }

        public ActorId ActorId { get; }
        public bool Required { get; }
        public bool HasPrefabReference { get; }
        public PlayerMaterializationStatus MaterializationStatus { get; }
        public string RuntimeName { get; }
        public string RuntimeSceneName { get; }
        public bool IsValid => ActorId.IsValid && MaterializationStatus != PlayerMaterializationStatus.Unknown;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
    public readonly struct PlayerSet
    {
        public PlayerSet(IReadOnlyList<PlayerSetEntry> entries)
        {
            Entries = entries ?? Array.Empty<PlayerSetEntry>();
        }

        public IReadOnlyList<PlayerSetEntry> Entries { get; }
        public int SeedEntriesCount => Entries?.Count ?? 0;
        public int RequiredPlayersCount => CountRequired(Entries);
        public int OptionalPlayersCount => SeedEntriesCount - RequiredPlayersCount;
        public bool IsEmpty => SeedEntriesCount == 0;
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

    public readonly struct PlayerParticipationSeedIdentity
    {
        public PlayerParticipationSeedIdentity(
            string pipelineId,
            string sessionId,
            string routeIdentity,
            string routeOperationId,
            int routeSequence,
            string transitionId)
        {
            PipelineId = Normalize(pipelineId);
            SessionId = Normalize(sessionId);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            TransitionId = Normalize(transitionId);
        }

        public string PipelineId { get; }
        public string SessionId { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public int RouteSequence { get; }
        public string TransitionId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionId) &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(TransitionId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct PlayerParticipationPlan
    {
        public PlayerParticipationPlan(
            PlayerParticipationSeedIdentity identity,
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

        public PlayerParticipationSeedIdentity Identity { get; }
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

    public readonly struct PlayerParticipationSeedSnapshot
    {
        public PlayerParticipationSeedSnapshot(
            PlayerParticipationSeedIdentity identity,
            PlayerParticipationOutcome outcome,
            PlayerParticipationKind participationKind,
            PlayerSet playerSet,
            IReadOnlyList<PlayerParticipationSeedEntry> seedEntries,
            IReadOnlyList<PlayerMaterializationEntry> materializationEntries,
            IReadOnlyList<PlayerReadinessEntry> readinessEntries,
            string message)
        {
            Identity = identity;
            Outcome = outcome;
            ParticipationKind = participationKind;
            PlayerSet = playerSet.IsValid ? playerSet : PlayerSet.Empty;
            SeedEntries = seedEntries ?? Array.Empty<PlayerParticipationSeedEntry>();
            MaterializationEntries = materializationEntries ?? Array.Empty<PlayerMaterializationEntry>();
            ReadinessEntries = readinessEntries ?? Array.Empty<PlayerReadinessEntry>();
            Message = Normalize(message);
        }

        public PlayerParticipationSeedIdentity Identity { get; }
        public PlayerParticipationOutcome Outcome { get; }
        public PlayerParticipationKind ParticipationKind { get; }
        public PlayerSet PlayerSet { get; }
        public IReadOnlyList<PlayerParticipationSeedEntry> SeedEntries { get; }
        public IReadOnlyList<PlayerMaterializationEntry> MaterializationEntries { get; }
        public IReadOnlyList<PlayerReadinessEntry> ReadinessEntries { get; }
        public int SeedEntriesCount => PlayerSet.SeedEntriesCount;
        public int RequiredPlayersCount => PlayerSet.RequiredPlayersCount;
        public int OptionalPlayersCount => PlayerSet.OptionalPlayersCount;
        public int NotMaterializedPlayersCount => CountNotMaterialized(MaterializationEntries);
        public int MaterializedPlayersCount => CountByMaterialization(MaterializationEntries, PlayerMaterializationStatus.Materialized);
        public int SkippedPlayersCount => CountByMaterialization(MaterializationEntries, PlayerMaterializationStatus.Skipped);
        public int PendingRequiredPlayersCount => CountByReadiness(ReadinessEntries, PlayerReadinessStatus.PendingMaterialization);
        public int PendingOptionalPlayersCount => CountByReadiness(ReadinessEntries, PlayerReadinessStatus.OptionalPending);
        public int PlayersWithPrefabCount => CountByPrefab(SeedEntries, true);
        public int PlayersWithoutPrefabCount => CountByPrefab(SeedEntries, false);
        public int PlayersWithPlacementCount => CountByPlacement(SeedEntries, true);
        public int PlayersWithoutPlacementCount => CountByPlacement(SeedEntries, false);
        public string Message { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Outcome != PlayerParticipationOutcome.Unknown &&
            ParticipationKind != PlayerParticipationKind.Unknown &&
            PlayerSet.IsValid &&
            AreSeedEntriesValid(SeedEntries) &&
            AreMaterializationEntriesValid(MaterializationEntries) &&
            AreOutcomeCountsConsistent(
                Outcome,
                SeedEntries,
                MaterializationEntries,
                ReadinessEntries,
                SeedEntriesCount,
                RequiredPlayersCount,
                OptionalPlayersCount,
                NotMaterializedPlayersCount,
                PendingRequiredPlayersCount,
                PendingOptionalPlayersCount) &&
            AreReadinessEntriesValid(ReadinessEntries) &&
            !string.IsNullOrWhiteSpace(Message);

        private static bool AreSeedEntriesValid(IReadOnlyList<PlayerParticipationSeedEntry> seedEntries)
        {
            if (seedEntries == null)
            {
                return false;
            }

            for (int i = 0; i < seedEntries.Count; i++)
            {
                if (!seedEntries[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountByPrefab(IReadOnlyList<PlayerParticipationSeedEntry> entries, bool hasPrefab)
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

        private static int CountByPlacement(IReadOnlyList<PlayerParticipationSeedEntry> entries, bool hasPlacement)
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
            PlayerParticipationOutcome outcome,
            IReadOnlyList<PlayerParticipationSeedEntry> seedEntries,
            IReadOnlyList<PlayerMaterializationEntry> materializationEntries,
            IReadOnlyList<PlayerReadinessEntry> readinessEntries,
            int seedEntriesCount,
            int requiredPlayersCount,
            int optionalPlayersCount,
            int notMaterializedPlayersCount,
            int pendingRequiredPlayersCount,
            int pendingOptionalPlayersCount)
        {
            int seedCount = seedEntries?.Count ?? 0;
            int materializationCount = materializationEntries?.Count ?? 0;
            int readinessCount = readinessEntries?.Count ?? 0;

            if (outcome == PlayerParticipationOutcome.ObservedNoOp)
            {
                return seedEntriesCount == 0 &&
                       seedCount == 0 &&
                       materializationCount == 0 &&
                       readinessCount == 0 &&
                       requiredPlayersCount == 0 &&
                       optionalPlayersCount == 0 &&
                       notMaterializedPlayersCount == 0 &&
                       pendingRequiredPlayersCount == 0 &&
                       pendingOptionalPlayersCount == 0;
            }

            if (outcome == PlayerParticipationOutcome.SeedResolved)
            {
                return seedEntriesCount > 0 &&
                       seedCount == seedEntriesCount &&
                       materializationCount == seedEntriesCount &&
                       readinessCount == seedEntriesCount &&
                       requiredPlayersCount + optionalPlayersCount == seedEntriesCount &&
                       notMaterializedPlayersCount == seedEntriesCount &&
                       pendingRequiredPlayersCount == requiredPlayersCount &&
                       pendingOptionalPlayersCount == optionalPlayersCount;
            }

            if (outcome == PlayerParticipationOutcome.Materialized)
            {
                return seedEntriesCount > 0 &&
                       seedCount == seedEntriesCount &&
                       materializationCount == seedEntriesCount &&
                       readinessCount == seedEntriesCount &&
                       requiredPlayersCount + optionalPlayersCount == seedEntriesCount &&
                       pendingRequiredPlayersCount == 0;
            }

            return false;
        }
    }

    public readonly struct PlayerParticipationResult
    {
        public PlayerParticipationResult(PlayerParticipationPlan plan, PlayerParticipationSeedSnapshot snapshot)
        {
            Plan = plan;
            Snapshot = snapshot;
        }

        public PlayerParticipationPlan Plan { get; }
        public PlayerParticipationSeedSnapshot Snapshot { get; }
        public IReadOnlyList<PlayerParticipationSeedEntry> SeedEntries => Snapshot.SeedEntries;
        public IReadOnlyList<PlayerMaterializationEntry> MaterializationEntries => Snapshot.MaterializationEntries;
        public IReadOnlyList<PlayerReadinessEntry> ReadinessEntries => Snapshot.ReadinessEntries;
        public bool IsObservedNoOp => Snapshot.Outcome == PlayerParticipationOutcome.ObservedNoOp;
        public bool IsSeedResolved => Snapshot.Outcome == PlayerParticipationOutcome.SeedResolved;
        public bool IsMaterialized => Snapshot.Outcome == PlayerParticipationOutcome.Materialized;
        public bool IsSeedOrNoOp => IsObservedNoOp || IsSeedResolved;

        public bool IsValid =>
            Plan.IsValid &&
            Snapshot.IsValid;
    }
}
