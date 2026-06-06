using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

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
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
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
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            ActivityObjectExitRuntimeState runtimeState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectContributorUnregisterStageCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            SessionActivityIdentity identity = command.Identity;
            int entrySequence = command.EntrySequence;
            SessionActivityIdentity unregisterStartedIdentity = endpoint.BuildIdentity(
                definition,
                SessionActivityStage.ActivityObjectContributorUnregisterStarted,
                entrySequence);
            endpoint.SetCurrentIdentity(unregisterStartedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterStarted);

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectContributorUnregisterStarted,
                unregisterStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor unregister started.");
            DebugUtility.Log(
                typeof(ActivityObjectContributorUnregisterStage),
                $"[OBS][ActivityObjectContributorUnregisterStage] event='ActivityObjectContributorUnregisterStarted' owner='ActivityObjectContributorUnregisterStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            endpoint.EmitSnapshot(
                snapshots,
                "activity_object_contributor_unregister_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor unregister started.");

            ActivityObjectContributorDiscoveryResult discoveryResult = runtimeState.CurrentContributorDiscoveryResult;
            if (!discoveryResult.IsValid || discoveryResult.Reports == null || discoveryResult.Reports.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(
                    definition,
                    SessionActivityStage.ActivityObjectContributorUnregisterSkippedNoContributors,
                    entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterSkippedNoContributors);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterSkippedNoContributors,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister skipped reason='no_discovery_result'.");
                SessionActivityIdentity completedIdentity = endpoint.BuildIdentity(
                    definition,
                    SessionActivityStage.ActivityObjectContributorUnregisterCompleted,
                    entrySequence);
                endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterCompleted);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterCompleted,
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister completed unregisteredCount='0'.");
                DebugUtility.Log(
                    typeof(ActivityObjectContributorUnregisterStage),
                    $"[OBS][ActivityObjectContributorUnregisterStage] event='ActivityObjectContributorUnregisterCompleted' owner='ActivityObjectContributorUnregisterStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' unregisteredCount='0' skippedNoContributors='True' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_unregister_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister completed unregisteredCount='0'.");
                return new ActivityObjectContributorUnregisterStageResult(
                    completed: true,
                    identity: completedIdentity,
                    unregisteredCount: 0,
                    skippedNoContributors: true,
                    reason: "no_discovery_result");
            }

            if (!IsDiscoveryResultForCurrentEntry(discoveryResult, unregisterStartedIdentity, entrySequence))
            {
                SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(
                    definition,
                    SessionActivityStage.ActivityObjectContributorUnregisterFailed,
                    entrySequence);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister failed reason='stale_or_foreign_discovery_result' discoveryIdentity='{discoveryResult.Identity}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_unregister_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister failed reason='stale_or_foreign_discovery_result'.");
                throw new InvalidOperationException(
                    $"stale_or_foreign_contributor_discovery_result: activityId='{definition.ActivityId}' entrySequence='{entrySequence}' discoveryIdentity='{discoveryResult.Identity}'.");
            }

            int unregisteredCount = 0;
            bool hasCurrentEntryContributors = false;
            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !IsReportForCurrentEntry(report, unregisterStartedIdentity, entrySequence))
                {
                    continue;
                }

                hasCurrentEntryContributors = true;
                unregisteredCount += 1;
                SessionActivityIdentity unregisteredIdentity = endpoint.BuildIdentity(
                    definition,
                    SessionActivityStage.ActivityObjectContributorUnregistered,
                    entrySequence);
                endpoint.SetCurrentIdentity(unregisteredIdentity, SessionActivityStage.ActivityObjectContributorUnregistered);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregistered,
                    unregisteredIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregistered contentProfileId='{report.ContentProfileId}' targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}'.");
                DebugUtility.Log(
                    typeof(ActivityObjectContributorUnregisterStage),
                    $"[OBS][ActivityObjectContributorUnregisterStage] event='ActivityObjectContributorUnregistered' owner='ActivityObjectContributorUnregisterStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
            }

            bool skipped = !hasCurrentEntryContributors;
            if (skipped)
            {
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(
                    definition,
                    SessionActivityStage.ActivityObjectContributorUnregisterSkippedNoContributors,
                    entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityObjectContributorUnregisterSkippedNoContributors);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterSkippedNoContributors,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister skipped reason='no_contributors_for_entry'.");
            }
            SessionActivityIdentity completedIdentityFinal = endpoint.BuildIdentity(
                definition,
                SessionActivityStage.ActivityObjectContributorUnregisterCompleted,
                entrySequence);
            endpoint.SetCurrentIdentity(completedIdentityFinal, SessionActivityStage.ActivityObjectContributorUnregisterCompleted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectContributorUnregisterCompleted,
                completedIdentityFinal,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor unregister completed unregisteredCount='{unregisteredCount}'.");
            DebugUtility.Log(
                typeof(ActivityObjectContributorUnregisterStage),
                $"[OBS][ActivityObjectContributorUnregisterStage] event='ActivityObjectContributorUnregisterCompleted' owner='ActivityObjectContributorUnregisterStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' unregisteredCount='{unregisteredCount}' skippedNoContributors='{skipped}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
            endpoint.EmitSnapshot(
                snapshots,
                "activity_object_contributor_unregister_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor unregister completed unregisteredCount='{unregisteredCount}'.");

            return new ActivityObjectContributorUnregisterStageResult(
                completed: true,
                identity: completedIdentityFinal,
                unregisteredCount: unregisteredCount,
                skippedNoContributors: skipped,
                reason: skipped ? "no_contributors_for_entry" : "completed");
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
