using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityObjectContributorUnregisterStageCommand
    {
        public ActivityObjectContributorUnregisterStageCommand(
            SessionActivityIdentity identity,
            SessionActivityCommand command,
            int entrySequence)
        {
            Identity = identity;
            Command = command;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityCommand Command { get; }
        public int EntrySequence { get; }
        public string Source => Command.Source;
        public string Reason => Command.Reason;

        public bool IsValid =>
            Identity.IsValid &&
            Command.Identity.IsValid &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(Source);
    }

    internal readonly struct ActivityObjectContributorUnregisterStageResult
    {
        public ActivityObjectContributorUnregisterStageResult(
            bool completed,
            SessionActivityIdentity identity,
            int unregisteredCount,
            bool skippedNoContributors,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            UnregisteredCount = unregisteredCount < 0 ? 0 : unregisteredCount;
            SkippedNoContributors = skippedNoContributors;
            Reason = reason.TrimToEmpty();
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int UnregisteredCount { get; }
        public bool SkippedNoContributors { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);
    }

    internal static class ActivityObjectContributorUnregisterStage
    {
        public static ActivityObjectContributorUnregisterStageResult Execute(
            ActivityObjectContributorUnregisterStageCommand command,
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            ActivityObjectExitRuntimeState runtimeState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectContributorUnregisterStageCommand is invalid.");
            }

            identityBridge = identityBridge ?? throw new ArgumentNullException(nameof(identityBridge));
            factBridge = factBridge ?? throw new ArgumentNullException(nameof(factBridge));
            runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            int entrySequence = command.EntrySequence;
            var unregisterStartedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActivityObjectContributorUnregisterStarted, entrySequence, command.Source);
            identityBridge.SetCurrentIdentity(unregisterStartedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterStarted);

            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectContributorUnregisterStarted,
                unregisterStartedIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity object contributor unregister started.");
            factBridge.EmitSnapshot(
                snapshots,
                "activity_object_contributor_unregister_started",
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity object contributor unregister started.");

            var discoveryResult = runtimeState.CurrentContributorDiscoveryResult;
            if (!discoveryResult.IsValid || discoveryResult.Reports == null || discoveryResult.Reports.Count == 0)
            {
                var skippedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActivityObjectContributorUnregisterSkippedNoContributors, entrySequence, command.Source);
                identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterSkippedNoContributors);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterSkippedNoContributors,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor unregister skipped reason='no_discovery_result'.");
                var completedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActivityObjectContributorUnregisterCompleted, entrySequence, command.Source);
                identityBridge.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterCompleted);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterCompleted,
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor unregister completed unregisteredCount='0'.");
                DebugUtility.Log(
                    typeof(ActivityObjectContributorUnregisterStage),
                    $"event='ActivityObjectContributorUnregisterCompleted' owner='ActivityObjectContributorUnregisterStage' activityId='{command.Identity.ActivityId}' entrySequence='{entrySequence}' unregisteredCount='0' skippedNoContributors='True' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                factBridge.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_unregister_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor unregister completed unregisteredCount='0'.");
                return new ActivityObjectContributorUnregisterStageResult(
                    completed: true,
                    identity: completedIdentity,
                    unregisteredCount: 0,
                    skippedNoContributors: true,
                    reason: "no_discovery_result");
            }

            if (!IsDiscoveryResultForCurrentEntry(discoveryResult, unregisterStartedIdentity, entrySequence))
            {
                var failedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActivityObjectContributorUnregisterFailed, entrySequence, command.Source);
                identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterFailed);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor unregister failed reason='stale_or_foreign_discovery_result' discoveryIdentity='{discoveryResult.Identity}'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_unregister_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor unregister failed reason='stale_or_foreign_discovery_result'.");
                throw new InvalidOperationException(
                    $"stale_or_foreign_contributor_discovery_result: activityId='{command.Identity.ActivityId}' entrySequence='{entrySequence}' discoveryIdentity='{discoveryResult.Identity}'.");
            }

            int unregisteredCount = 0;
            bool hasCurrentEntryContributors = false;
            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                var report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !IsReportForCurrentEntry(report, unregisterStartedIdentity, entrySequence))
                {
                    continue;
                }

                hasCurrentEntryContributors = true;
                unregisteredCount += 1;
                var unregisteredIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActivityObjectContributorUnregistered, entrySequence, command.Source);
                identityBridge.SetCurrentIdentity(unregisteredIdentity, SessionActivityStage.ActivityObjectContributorUnregistered);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregistered,
                    unregisteredIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor unregistered contentProfileId='{report.ContentProfileId}' targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}'.");
                DebugUtility.Log(
                    typeof(ActivityObjectContributorUnregisterStage),
                    $"event='ActivityObjectContributorUnregistered' owner='ActivityObjectContributorUnregisterStage' activityId='{command.Identity.ActivityId}' entrySequence='{entrySequence}' targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
            }

            bool skipped = !hasCurrentEntryContributors;
            if (skipped)
            {
                var skippedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActivityObjectContributorUnregisterSkippedNoContributors, entrySequence, command.Source);
                identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterSkippedNoContributors);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterSkippedNoContributors,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity object contributor unregister skipped reason='no_contributors_for_entry'.");
            }
            var completedIdentityFinal = BuildIdentity(command.Identity, SessionActivityStage.ActivityObjectContributorUnregisterCompleted, entrySequence, command.Source);
            identityBridge.SetCurrentIdentity(completedIdentityFinal, SessionActivityStage.ActivityObjectContributorUnregisterCompleted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectContributorUnregisterCompleted,
                completedIdentityFinal,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity object contributor unregister completed unregisteredCount='{unregisteredCount}'.");
            DebugUtility.Log(
                typeof(ActivityObjectContributorUnregisterStage),
                $"event='ActivityObjectContributorUnregisterCompleted' owner='ActivityObjectContributorUnregisterStage' activityId='{command.Identity.ActivityId}' entrySequence='{entrySequence}' unregisteredCount='{unregisteredCount}' skippedNoContributors='{skipped}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
            factBridge.EmitSnapshot(
                snapshots,
                "activity_object_contributor_unregister_completed",
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity object contributor unregister completed unregisteredCount='{unregisteredCount}'.");

            return new ActivityObjectContributorUnregisterStageResult(
                completed: true,
                identity: completedIdentityFinal,
                unregisteredCount: unregisteredCount,
                skippedNoContributors: skipped,
                reason: skipped ? "no_contributors_for_entry" : "completed");
        }

        private static SessionActivityIdentity BuildIdentity(
            SessionActivityIdentity identity,
            SessionActivityStage stage,
            int entrySequence,
            string source)
        {
            return new SessionActivityIdentity(
                identity.PipelineId,
                identity.SessionId,
                identity.ActivityId,
                identity.ActivityOrdinal,
                entrySequence,
                stage,
                source);
        }

        private static bool IsDiscoveryResultForCurrentEntry(
            ActivityObjectContributorDiscoveryResult result,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            return result.IsValid &&
                   identity.IsValid &&
                   string.Equals(result.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                   result.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                   result.Identity.EntrySequence == entrySequence;
        }

        private static bool IsReportForCurrentEntry(
            ActivityObjectContributionReport report,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            return report.IsValid &&
                   identity.IsValid &&
                   string.Equals(report.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(report.SessionStateId, identity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(report.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                   report.ActivityOrdinal == identity.ActivityOrdinal &&
                   report.EntrySequence == entrySequence;
        }
    }
}
