using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Preparation
{
    public static class PlayerPreparationStage
    {
        public static PlayerPreparationResult Execute(PlayerPreparationPlan plan, IReadOnlyList<PlayerMaterializationRecord> materializationRecords = null)
        {
            if (!plan.IsValid)
            {
                throw new System.InvalidOperationException("PlayerPreparationPlan is invalid.");
            }

            PlayerParticipationKind participationKind = ResolveParticipationKind(plan);
            IReadOnlyList<PlayerPlannedEntry> plannedEntries = BuildPlannedEntries(plan.PlayerSet);
            IReadOnlyList<PlayerMaterializationEntry> materializationEntries = BuildMaterializationEntries(plannedEntries, materializationRecords);
            IReadOnlyList<PlayerReadinessEntry> readinessEntries = BuildReadinessEntries(materializationEntries);
            PlayerPreparationOutcome outcome = ResolveOutcome(plan.PlayerSet, materializationEntries);
            string message = BuildOutcomeMessage(outcome);
            PlayerPreparationSnapshot snapshot = new(
                plan.Identity,
                outcome,
                participationKind,
                plan.PlayerSet,
                plannedEntries,
                materializationEntries,
                readinessEntries,
                message);

            PlayerPreparationResult result = new(plan, snapshot);
            if (!result.IsValid)
            {
                throw new System.InvalidOperationException("PlayerPreparationResult is invalid.");
            }

            DebugUtility.Log(typeof(PlayerPreparationStage),
                $"[OBS][PlayerPreparationStage] pipelineId='{plan.Identity.PipelineId}' sessionId='{plan.Identity.SessionId}' routeIdentity='{plan.Identity.RouteIdentity}' routeOperationId='{plan.Identity.RouteOperationId}' routeSequence='{plan.Identity.RouteSequence}' transitionId='{plan.Identity.TransitionId}' stage='PlayerPreparationStage' outcome='{FormatOutcome(snapshot.Outcome)}' participationKind='{snapshot.ParticipationKind}' source='{plan.Source}' reason='{plan.Reason}' plannedPlayers='{snapshot.PlannedPlayersCount}' requiredPlayers='{snapshot.RequiredPlayersCount}' optionalPlayers='{snapshot.OptionalPlayersCount}' notMaterializedPlayers='{snapshot.NotMaterializedPlayersCount}' pendingRequiredPlayers='{snapshot.PendingRequiredPlayersCount}' pendingOptionalPlayers='{snapshot.PendingOptionalPlayersCount}' playersWithPrefab='{snapshot.PlayersWithPrefabCount}' playersWithoutPrefab='{snapshot.PlayersWithoutPrefabCount}' playersWithPlacement='{snapshot.PlayersWithPlacementCount}' playersWithoutPlacement='{snapshot.PlayersWithoutPlacementCount}' playerIds='{FormatPlayerIds(snapshot.PlannedEntries)}' message='{snapshot.Message}'.",
                DebugUtility.Colors.Info);

            return result;
        }

        private static PlayerParticipationKind ResolveParticipationKind(PlayerPreparationPlan plan)
        {
            if (!plan.PlayerSet.IsEmpty)
            {
                return PlayerParticipationKind.PlayerSetExpected;
            }

            if (plan.ExpectsSessionActivityEntry)
            {
                return PlayerParticipationKind.ActivityEntryWithoutPlayerSet;
            }

            return PlayerParticipationKind.NoPlayers;
        }

        private static PlayerPreparationOutcome ResolveOutcome(PlayerSet playerSet, IReadOnlyList<PlayerMaterializationEntry> materializationEntries)
        {
            if (playerSet.IsEmpty)
            {
                return PlayerPreparationOutcome.ObservedNoOp;
            }

            if (materializationEntries != null)
            {
                for (int i = 0; i < materializationEntries.Count; i++)
                {
                    if (materializationEntries[i].MaterializationStatus == PlayerMaterializationStatus.Materialized)
                    {
                        return PlayerPreparationOutcome.Materialized;
                    }
                }
            }

            return PlayerPreparationOutcome.PlannedOnly;
        }

        private static IReadOnlyList<PlayerPlannedEntry> BuildPlannedEntries(PlayerSet playerSet)
        {
            if (playerSet.Entries == null || playerSet.Entries.Count == 0)
            {
                return System.Array.Empty<PlayerPlannedEntry>();
            }

            List<PlayerPlannedEntry> plannedEntries = new(playerSet.Entries.Count);
            for (int i = 0; i < playerSet.Entries.Count; i++)
            {
                PlayerSetEntry sourceEntry = playerSet.Entries[i];
                plannedEntries.Add(new PlayerPlannedEntry(
                    sourceEntry.PlayerId,
                    sourceEntry.Required,
                    sourceEntry.HasPrefabReference,
                    sourceEntry.PlacementMode,
                    sourceEntry.HasPlacementPlan,
                    PlayerPreparationEntryStatus.PlannedOnly));
            }

            return plannedEntries;
        }

        private static IReadOnlyList<PlayerMaterializationEntry> BuildMaterializationEntries(IReadOnlyList<PlayerPlannedEntry> plannedEntries, IReadOnlyList<PlayerMaterializationRecord> materializationRecords)
        {
            if (plannedEntries == null || plannedEntries.Count == 0)
            {
                return System.Array.Empty<PlayerMaterializationEntry>();
            }

            List<PlayerMaterializationEntry> materializationEntries = new(plannedEntries.Count);
            for (int i = 0; i < plannedEntries.Count; i++)
            {
                PlayerPlannedEntry plannedEntry = plannedEntries[i];
                PlayerMaterializationRecord record = ResolveRecord(plannedEntry, materializationRecords);
                bool isMaterialized = record.MaterializationStatus == PlayerMaterializationStatus.Materialized;
                bool isSkipped = record.MaterializationStatus == PlayerMaterializationStatus.Skipped;
                materializationEntries.Add(new PlayerMaterializationEntry(
                    plannedEntry.PlayerId,
                    plannedEntry.Required,
                    plannedEntry.HasPrefabReference,
                    plannedEntry.PlacementMode,
                    plannedEntry.HasPlacementPlan,
                    isMaterialized ? PlayerPreparationEntryStatus.Materialized : (isSkipped ? PlayerPreparationEntryStatus.Skipped : plannedEntry.Status),
                    record.MaterializationStatus == PlayerMaterializationStatus.Unknown ? PlayerMaterializationStatus.NotMaterialized : record.MaterializationStatus,
                    record.RuntimeName,
                    record.RuntimeSceneName));
            }

            return materializationEntries;
        }

        private static IReadOnlyList<PlayerReadinessEntry> BuildReadinessEntries(IReadOnlyList<PlayerMaterializationEntry> materializationEntries)
        {
            if (materializationEntries == null || materializationEntries.Count == 0)
            {
                return System.Array.Empty<PlayerReadinessEntry>();
            }

            List<PlayerReadinessEntry> readinessEntries = new(materializationEntries.Count);
            for (int i = 0; i < materializationEntries.Count; i++)
            {
                PlayerMaterializationEntry materializationEntry = materializationEntries[i];
                PlayerReadinessStatus readinessStatus = materializationEntry.Required
                    ? PlayerReadinessStatus.PendingMaterialization
                    : PlayerReadinessStatus.OptionalPending;

                if (materializationEntry.MaterializationStatus == PlayerMaterializationStatus.Materialized)
                {
                    readinessStatus = PlayerReadinessStatus.Ready;
                }
                else if (!materializationEntry.Required && materializationEntry.MaterializationStatus == PlayerMaterializationStatus.Skipped)
                {
                    readinessStatus = PlayerReadinessStatus.OptionalSkipped;
                }

                readinessEntries.Add(new PlayerReadinessEntry(
                    materializationEntry.PlayerId,
                    materializationEntry.Required,
                    materializationEntry.HasPrefabReference,
                    materializationEntry.PlacementMode,
                    materializationEntry.HasPlacementPlan,
                    materializationEntry.PreparationStatus,
                    materializationEntry.MaterializationStatus,
                    readinessStatus));
            }

            return readinessEntries;
        }

        private static string FormatPlayerIds(IReadOnlyList<PlayerPlannedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            int max = entries.Count > 6 ? 6 : entries.Count;
            List<string> ids = new(max + 1);
            for (int i = 0; i < max; i++)
            {
                ids.Add(entries[i].PlayerId);
            }

            if (entries.Count > max)
            {
                ids.Add($"+{entries.Count - max} more");
            }

            return string.Join(", ", ids);
        }

        private static string FormatOutcome(PlayerPreparationOutcome outcome)
        {
            return outcome switch
            {
                PlayerPreparationOutcome.ObservedNoOp => "observed_noop",
                PlayerPreparationOutcome.PlannedOnly => "planned_only",
                PlayerPreparationOutcome.Materialized => "materialized",
                _ => "unknown"
            };
        }

        private static string BuildOutcomeMessage(PlayerPreparationOutcome outcome)
        {
            return outcome == PlayerPreparationOutcome.PlannedOnly
                ? "Player preparation plan created. No player materialization executed."
                : (outcome == PlayerPreparationOutcome.Materialized
                    ? "Prototype players materialized."
                    : "No players planned. Stage observed as canonical no-op.");
        }

        private static PlayerMaterializationRecord ResolveRecord(PlayerPlannedEntry plannedEntry, IReadOnlyList<PlayerMaterializationRecord> materializationRecords)
        {
            if (materializationRecords == null || materializationRecords.Count == 0)
            {
                return default;
            }

            for (int i = 0; i < materializationRecords.Count; i++)
            {
                PlayerMaterializationRecord candidate = materializationRecords[i];
                if (!candidate.IsValid)
                {
                    continue;
                }

                if (candidate.PlayerId == plannedEntry.PlayerId)
                {
                    return candidate;
                }
            }

            return default;
        }
    }
}
