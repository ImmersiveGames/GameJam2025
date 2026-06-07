using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryActorCommandBindingStage
    {
        public static ActorCommandBindingResult Execute(
            ActorCommandBindingCommand command,
            IActivityEntryRuntimeBridge endpoint,
            IActorCommandBindingAdapter adapter,
            ActivityPlayerActorRegistry registry,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActorCommandBindingCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            registry = registry ?? throw new ArgumentNullException(nameof(registry));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            int entrySequence = command.PipelineIdentity.EntrySequence;
            SessionActivityIdentity startedIdentity = BuildIdentity(command, SessionActivityStage.ActorCommandBindingStarted);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorCommandBindingStarted);

            IReadOnlyList<ActorCommandBindingReference> requirements = command.Bindings ?? Array.Empty<ActorCommandBindingReference>();
            if (requirements.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorCommandBindingSkippedNoRequiredCapability);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorCommandBindingSkippedNoRequiredCapability);
                return new ActorCommandBindingResult(
                    skippedIdentity,
                    totalRequirements: 0,
                    requiredRequirements: 0,
                    requiredBoundCount: 0,
                    totalBoundCount: 0,
                    skippedCount: 0,
                    skipped: true,
                    reason: "no_actor_command_requirements");
            }

            IReadOnlyList<ActorCommandBindingRecord> records = adapter.Execute(command, command.PipelineIdentity, registry);
            if (records == null || records.Count != requirements.Count)
            {
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][ActorCommandBinding] Adapter result mismatch activityId='{command.PipelineIdentity.ActivityId}' entrySequence='{entrySequence}'.");
            }

            int requiredCount = 0;
            int requiredBoundCount = 0;
            int totalBoundCount = 0;
            int skippedCount = 0;
            for (int index = 0; index < requirements.Count; index++)
            {
                if (requirements[index].Required)
                {
                    requiredCount += 1;
                }
            }

            for (int index = 0; index < records.Count; index++)
            {
                ActorCommandBindingRecord record = records[index];
                if (!record.IsValid)
                {
                    throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][ActorCommandBinding] Invalid binding record activityId='{command.PipelineIdentity.ActivityId}' entrySequence='{entrySequence}' index='{index}'.");
                }

                if (record.Bound)
                {
                    totalBoundCount += 1;
                }

                if (record.Skipped)
                {
                    skippedCount += 1;
                }

                if (record.Requirement.Required && record.Bound)
                {
                    requiredBoundCount += 1;
                }
            }

            if (requiredBoundCount < requiredCount)
            {
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][ActorCommandBinding] Required binding incomplete activityId='{command.PipelineIdentity.ActivityId}' entrySequence='{entrySequence}' requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(command, SessionActivityStage.ActorCommandBindingCompleted);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorCommandBindingCompleted);
            return new ActorCommandBindingResult(
                completedIdentity,
                requirements.Count,
                requiredCount,
                requiredBoundCount,
                totalBoundCount,
                skippedCount: skippedCount,
                skipped: skippedCount > 0,
                reason: "resolved");
        }

        private static SessionActivityIdentity BuildIdentity(
            ActorCommandBindingCommand command,
            SessionActivityStage stage)
        {
            return new SessionActivityIdentity(
                command.PipelineIdentity.PipelineId,
                command.PipelineIdentity.SessionId,
                command.PipelineIdentity.ActivityId,
                command.PipelineIdentity.ActivityOrdinal,
                command.PipelineIdentity.EntrySequence,
                stage,
                command.Source);
        }
    }
}
