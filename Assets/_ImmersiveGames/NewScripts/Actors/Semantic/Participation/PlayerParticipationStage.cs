using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Participation
{
    public static class PlayerParticipationStage
    {
        public static PlayerParticipationResult Execute(PlayerParticipationPlan plan, IReadOnlyList<PlayerMaterializationRecord> materializationRecords = null)
        {
            if (!plan.IsValid)
            {
                throw new System.InvalidOperationException("PlayerParticipationPlan is invalid.");
            }

            PlayerParticipationKind participationKind = ResolveParticipationKind(plan);
            IReadOnlyList<PlayerParticipationSeedEntry> seedEntries = BuildSeedEntries(plan.PlayerSet);
            IReadOnlyList<PlayerMaterializationEntry> materializationEntries = BuildMaterializationEntries(seedEntries, materializationRecords);
            IReadOnlyList<PlayerReadinessEntry> readinessEntries = BuildReadinessEntries(materializationEntries);
            PlayerParticipationOutcome outcome = ResolveOutcome(plan.PlayerSet, materializationEntries);
            string message = BuildOutcomeMessage(outcome);
            PlayerParticipationSeedSnapshot snapshot = new(
                plan.Identity,
                outcome,
                participationKind,
                plan.PlayerSet,
                seedEntries,
                materializationEntries,
                readinessEntries,
                message);

            PlayerParticipationResult result = new(plan, snapshot);
            if (!result.IsValid)
            {
                throw new System.InvalidOperationException("PlayerParticipationResult is invalid.");
            }

            DebugUtility.Log(typeof(PlayerParticipationStage),
                $"[OBS][PlayerParticipationStage] pipelineId='{plan.Identity.PipelineId}' sessionId='{plan.Identity.SessionId}' routeIdentity='{plan.Identity.RouteIdentity}' routeOperationId='{plan.Identity.RouteOperationId}' routeSequence='{plan.Identity.RouteSequence}' transitionId='{plan.Identity.TransitionId}' stage='PlayerParticipationStage' outcome='{FormatOutcome(snapshot.Outcome)}' participationKind='{snapshot.ParticipationKind}' source='{plan.Source}' reason='{plan.Reason}' seedEntries='{snapshot.SeedEntriesCount}' requiredSeedEntries='{snapshot.RequiredPlayersCount}' optionalSeedEntries='{snapshot.OptionalPlayersCount}' unmaterializedSeedEntries='{snapshot.NotMaterializedPlayersCount}' requiredMaterializationPending='{snapshot.PendingRequiredPlayersCount}' optionalMaterializationPending='{snapshot.PendingOptionalPlayersCount}' entriesWithPrefab='{snapshot.PlayersWithPrefabCount}' entriesWithoutPrefab='{snapshot.PlayersWithoutPrefabCount}' entriesWithPlacement='{snapshot.PlayersWithPlacementCount}' entriesWithoutPlacement='{snapshot.PlayersWithoutPlacementCount}' seedSlotIds='{FormatSeedSlotIds(snapshot.SeedEntries)}' seedActorDefinitionIds='{FormatSeedActorDefinitionIds(snapshot.SeedEntries)}' seedActorIds='{FormatSeedActorIds(snapshot.SeedEntries)}' seedActorIdSource='PlayerSetDefinitionEntry' derivedActorScopes='{FormatSeedActorScopes(snapshot.SeedEntries)}' message='{snapshot.Message}'.",
                DebugUtility.Colors.Info);

            return result;
        }

        private static PlayerParticipationKind ResolveParticipationKind(PlayerParticipationPlan plan)
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

        private static PlayerParticipationOutcome ResolveOutcome(PlayerSet playerSet, IReadOnlyList<PlayerMaterializationEntry> materializationEntries)
        {
            if (playerSet.IsEmpty)
            {
                return PlayerParticipationOutcome.ObservedNoOp;
            }

            if (materializationEntries != null)
            {
                for (int i = 0; i < materializationEntries.Count; i++)
                {
                    if (materializationEntries[i].MaterializationStatus == PlayerMaterializationStatus.Materialized)
                    {
                        return PlayerParticipationOutcome.Materialized;
                    }
                }
            }

            return PlayerParticipationOutcome.SeedResolved;
        }

        private static IReadOnlyList<PlayerParticipationSeedEntry> BuildSeedEntries(PlayerSet playerSet)
        {
            if (playerSet.Entries == null || playerSet.Entries.Count == 0)
            {
                return System.Array.Empty<PlayerParticipationSeedEntry>();
            }

            List<PlayerParticipationSeedEntry> seedEntries = new(playerSet.Entries.Count);
            for (int i = 0; i < playerSet.Entries.Count; i++)
            {
                PlayerSetEntry sourceEntry = playerSet.Entries[i];
                seedEntries.Add(new PlayerParticipationSeedEntry(
                    sourceEntry.PlayerSlotId,
                    sourceEntry.PlayerSelectionId,
                    sourceEntry.ActorDefinitionId,
                    sourceEntry.ActorId,
                    ActorScope.SessionScoped,
                    sourceEntry.Required,
                    sourceEntry.HasPrefabReference,
                    sourceEntry.PlacementMode,
                    sourceEntry.HasPlacementPlan,
                    PlayerParticipationSeedEntryStatus.SeedResolved));
            }

            return seedEntries;
        }

        private static IReadOnlyList<PlayerMaterializationEntry> BuildMaterializationEntries(IReadOnlyList<PlayerParticipationSeedEntry> seedEntries, IReadOnlyList<PlayerMaterializationRecord> materializationRecords)
        {
            if (seedEntries == null || seedEntries.Count == 0)
            {
                return System.Array.Empty<PlayerMaterializationEntry>();
            }

            List<PlayerMaterializationEntry> materializationEntries = new(seedEntries.Count);
            for (int i = 0; i < seedEntries.Count; i++)
            {
                PlayerParticipationSeedEntry seedEntry = seedEntries[i];
                PlayerMaterializationRecord record = ResolveRecord(seedEntry, materializationRecords);
                bool isMaterialized = record.MaterializationStatus == PlayerMaterializationStatus.Materialized;
                bool isSkipped = record.MaterializationStatus == PlayerMaterializationStatus.Skipped;
                materializationEntries.Add(new PlayerMaterializationEntry(
                    seedEntry.PlayerSlotId,
                    seedEntry.PlayerSelectionId,
                    seedEntry.ActorDefinitionId,
                    seedEntry.ActorId,
                    seedEntry.Required,
                    seedEntry.HasPrefabReference,
                    seedEntry.PlacementMode,
                    seedEntry.HasPlacementPlan,
                    isMaterialized ? PlayerParticipationSeedEntryStatus.Materialized : (isSkipped ? PlayerParticipationSeedEntryStatus.Skipped : seedEntry.Status),
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
                    materializationEntry.PlayerSlotId,
                    materializationEntry.PlayerSelectionId,
                    materializationEntry.ActorDefinitionId,
                    materializationEntry.ActorId,
                    materializationEntry.Required,
                    materializationEntry.HasPrefabReference,
                    materializationEntry.PlacementMode,
                    materializationEntry.HasPlacementPlan,
                    materializationEntry.ParticipationStatus,
                    materializationEntry.MaterializationStatus,
                    readinessStatus));
            }

            return readinessEntries;
        }


        private static string FormatSeedActorScopes(IReadOnlyList<PlayerParticipationSeedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            List<string> values = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].ActorScope != ActorScope.Unknown)
                {
                    values.Add(entries[i].ActorScope.ToString());
                }
            }

            return values.Count == 0 ? "<none>" : string.Join(", ", values);
        }

        private static string FormatSeedSlotIds(IReadOnlyList<PlayerParticipationSeedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            int max = entries.Count > 6 ? 6 : entries.Count;
            List<string> ids = new(max + 1);
            for (int i = 0; i < max; i++)
            {
                ids.Add(entries[i].PlayerSlotId.ToString());
            }

            if (entries.Count > max)
            {
                ids.Add($"+{entries.Count - max} more");
            }

            return string.Join(", ", ids);
        }

        private static string FormatSeedActorDefinitionIds(IReadOnlyList<PlayerParticipationSeedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            int max = entries.Count > 6 ? 6 : entries.Count;
            List<string> ids = new(max + 1);
            for (int i = 0; i < max; i++)
            {
                ids.Add(entries[i].ActorDefinitionId.ToString());
            }

            if (entries.Count > max)
            {
                ids.Add($"+{entries.Count - max} more");
            }

            return string.Join(", ", ids);
        }

        private static string FormatSeedActorIds(IReadOnlyList<PlayerParticipationSeedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            int max = entries.Count > 6 ? 6 : entries.Count;
            List<string> ids = new(max + 1);
            for (int i = 0; i < max; i++)
            {
                ids.Add(entries[i].ActorId.ToString());
            }

            if (entries.Count > max)
            {
                ids.Add($"+{entries.Count - max} more");
            }

            return string.Join(", ", ids);
        }

        private static string FormatOutcome(PlayerParticipationOutcome outcome)
        {
            return outcome switch
            {
                PlayerParticipationOutcome.ObservedNoOp => "observed_noop",
                PlayerParticipationOutcome.SeedResolved => "seed_resolved",
                PlayerParticipationOutcome.Materialized => "materialized",
                _ => "unknown"
            };
        }

        private static string BuildOutcomeMessage(PlayerParticipationOutcome outcome)
        {
            return outcome == PlayerParticipationOutcome.SeedResolved
                ? "Player participation seed resolved. Actor materialization remains owned by ActivityEntryPipeline."
                : (outcome == PlayerParticipationOutcome.Materialized
                    ? "Player participation materialization observed."
                    : "No player participation seed entries resolved. Stage observed as canonical no-op.");
        }

        private static PlayerMaterializationRecord ResolveRecord(PlayerParticipationSeedEntry seedEntry, IReadOnlyList<PlayerMaterializationRecord> materializationRecords)
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

                if (candidate.ActorId == seedEntry.ActorId)
                {
                    return candidate;
                }
            }

            return default;
        }
    }
}
