using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Preparation
{
    public static class ActorPreparationStage
    {
        public static ActorPreparationResult Execute(ActorPreparationPlan plan)
        {
            if (!plan.IsValid)
            {
                throw new System.InvalidOperationException("ActorPreparationPlan is invalid.");
            }

            ActorParticipationKind participationKind = ResolveParticipationKind(plan);
            IReadOnlyList<ActorPlannedEntry> plannedEntries = BuildPlannedEntries(plan.ActorSet);
            IReadOnlyList<ActorMaterializationEntry> materializationEntries = BuildMaterializationEntries(plannedEntries);
            IReadOnlyList<ActorReadinessEntry> readinessEntries = BuildReadinessEntries(materializationEntries);
            ActorPreparationOutcome outcome = ResolveOutcome(plan.ActorSet);
            string message = BuildOutcomeMessage(outcome);
            ActorPreparationSnapshot snapshot = new(
                plan.Identity,
                outcome,
                participationKind,
                plan.ActorSet,
                plannedEntries,
                materializationEntries,
                readinessEntries,
                message);

            ActorPreparationResult result = new(plan, snapshot);
            if (!result.IsValid)
            {
                throw new System.InvalidOperationException("ActorPreparationResult is invalid.");
            }

            DebugUtility.Log(typeof(ActorPreparationStage),
                $"[OBS][ActorPreparationStage] pipelineId='{plan.Identity.PipelineId}' sessionId='{plan.Identity.SessionId}' routeIdentity='{plan.Identity.RouteIdentity}' routeSequence='{plan.Identity.RouteSequence}' transitionId='{plan.Identity.TransitionId}' stage='ActorPreparationStage' outcome='{FormatOutcome(snapshot.Outcome)}' participationKind='{snapshot.ParticipationKind}' source='{plan.Source}' reason='{plan.Reason}' plannedActors='{snapshot.PlannedActorsCount}' requiredActors='{snapshot.RequiredActorsCount}' optionalActors='{snapshot.OptionalActorsCount}' notMaterializedActors='{snapshot.NotMaterializedActorsCount}' pendingRequiredActors='{snapshot.PendingRequiredActorsCount}' pendingOptionalActors='{snapshot.PendingOptionalActorsCount}' actorsWithPrefab='{snapshot.ActorsWithPrefabCount}' actorsWithoutPrefab='{snapshot.ActorsWithoutPrefabCount}' actorsWithPlacement='{snapshot.ActorsWithPlacementCount}' actorsWithoutPlacement='{snapshot.ActorsWithoutPlacementCount}' actorIds='{FormatActorIds(snapshot.PlannedEntries)}' message='{snapshot.Message}'.",
                DebugUtility.Colors.Info);

            return result;
        }

        private static ActorParticipationKind ResolveParticipationKind(ActorPreparationPlan plan)
        {
            if (!plan.ActorSet.IsEmpty)
            {
                return ActorParticipationKind.ActorSetExpected;
            }

            if (plan.ExpectsSessionActivityEntry)
            {
                return ActorParticipationKind.ActivityEntryWithoutActorSet;
            }

            return ActorParticipationKind.NoActors;
        }

        private static ActorPreparationOutcome ResolveOutcome(ActorSet actorSet)
        {
            return actorSet.IsEmpty
                ? ActorPreparationOutcome.ObservedNoOp
                : ActorPreparationOutcome.PlannedOnly;
        }

        private static IReadOnlyList<ActorPlannedEntry> BuildPlannedEntries(ActorSet actorSet)
        {
            if (actorSet.Entries == null || actorSet.Entries.Count == 0)
            {
                return System.Array.Empty<ActorPlannedEntry>();
            }

            List<ActorPlannedEntry> plannedEntries = new(actorSet.Entries.Count);
            for (int i = 0; i < actorSet.Entries.Count; i++)
            {
                ActorSetEntry sourceEntry = actorSet.Entries[i];
                plannedEntries.Add(new ActorPlannedEntry(
                    sourceEntry.ActorId,
                    sourceEntry.Required,
                    sourceEntry.HasPrefabReference,
                    sourceEntry.PlacementMode,
                    sourceEntry.HasPlacementPlan,
                    ActorPreparationEntryStatus.PlannedOnly));
            }

            return plannedEntries;
        }

        private static IReadOnlyList<ActorMaterializationEntry> BuildMaterializationEntries(IReadOnlyList<ActorPlannedEntry> plannedEntries)
        {
            if (plannedEntries == null || plannedEntries.Count == 0)
            {
                return System.Array.Empty<ActorMaterializationEntry>();
            }

            List<ActorMaterializationEntry> materializationEntries = new(plannedEntries.Count);
            for (int i = 0; i < plannedEntries.Count; i++)
            {
                ActorPlannedEntry plannedEntry = plannedEntries[i];
                materializationEntries.Add(new ActorMaterializationEntry(
                    plannedEntry.ActorId,
                    plannedEntry.Required,
                    plannedEntry.HasPrefabReference,
                    plannedEntry.PlacementMode,
                    plannedEntry.HasPlacementPlan,
                    plannedEntry.Status,
                    ActorMaterializationStatus.NotMaterialized));
            }

            return materializationEntries;
        }

        private static IReadOnlyList<ActorReadinessEntry> BuildReadinessEntries(IReadOnlyList<ActorMaterializationEntry> materializationEntries)
        {
            if (materializationEntries == null || materializationEntries.Count == 0)
            {
                return System.Array.Empty<ActorReadinessEntry>();
            }

            List<ActorReadinessEntry> readinessEntries = new(materializationEntries.Count);
            for (int i = 0; i < materializationEntries.Count; i++)
            {
                ActorMaterializationEntry materializationEntry = materializationEntries[i];
                ActorReadinessStatus readinessStatus = materializationEntry.Required
                    ? ActorReadinessStatus.PendingMaterialization
                    : ActorReadinessStatus.OptionalPending;

                readinessEntries.Add(new ActorReadinessEntry(
                    materializationEntry.ActorId,
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

        private static string FormatActorIds(IReadOnlyList<ActorPlannedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            int max = entries.Count > 6 ? 6 : entries.Count;
            List<string> ids = new(max + 1);
            for (int i = 0; i < max; i++)
            {
                ids.Add(entries[i].ActorId);
            }

            if (entries.Count > max)
            {
                ids.Add($"+{entries.Count - max} more");
            }

            return string.Join(", ", ids);
        }

        private static string FormatOutcome(ActorPreparationOutcome outcome)
        {
            return outcome switch
            {
                ActorPreparationOutcome.ObservedNoOp => "observed_noop",
                ActorPreparationOutcome.PlannedOnly => "planned_only",
                _ => "unknown"
            };
        }

        private static string BuildOutcomeMessage(ActorPreparationOutcome outcome)
        {
            return outcome == ActorPreparationOutcome.PlannedOnly
                ? "Actor preparation plan created. No actor materialization executed."
                : "No actors planned. Stage observed as canonical no-op.";
        }
    }
}
