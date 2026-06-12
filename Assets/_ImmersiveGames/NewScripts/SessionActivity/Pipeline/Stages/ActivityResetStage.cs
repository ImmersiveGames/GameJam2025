using System;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityResetStage
    {
        public static ActivityResetResult Execute(
            ActivityResetCommand command,
            ActivityResetContext context,
            Func<ActivityObjectContributorDiscoveryResult, string, int, int, bool> isDiscoveryResultForCurrentEntry,
            Func<ActivityObjectContributionReport, string, int, int, bool> isReportForCurrentEntry,
            Func<ActivityObjectContributorDiscoveryResult, string, int, int, bool> hasRequiredResetContributor,
            Func<ActivityCapabilityInventory, ActivityObjectContributionReport, IActivityObjectResetEndpoint[]> resolveEndpointsFromInventory,
            Func<ActivityObjectResetCommand, IActivityObjectResetEndpoint[], ActivityObjectResetResult> executeObjectResetCommand,
            Func<ActivityObjectResetResult, string, int, int, bool> isObjectResetResultForCurrentEntry,
            Action<SessionActivityFactKind, string> emitFact,
            Action<string, string> emitSnapshot)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityResetCommand is invalid.");
            }

            var resetIdentity = command.Identity;
            string activityId = command.ActivityId;
            int activityOrdinal = command.ActivityOrdinal;
            int entrySequence = resetIdentity.EntrySequence;
            var discoveryResult = context.DiscoveryResult;

            emitFact(
                SessionActivityFactKind.ObjectResetStarted,
                $"'{activityId}' object reset started.");
            emitSnapshot(
                "object_reset_started",
                $"'{activityId}' object reset started.");

            if (!discoveryResult.IsValid)
            {
                const ActivityResetCompletionKind completionKind = ActivityResetCompletionKind.NoCommands;
                const string completionReason = "no_discovery_for_current_entry";
                emitFact(
                    SessionActivityFactKind.ObjectResetCompleted,
                    $"'{activityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                emitSnapshot(
                    "object_reset_completed",
                    $"'{activityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                return new ActivityResetResult(completionKind, completionReason, 0, 0, 0, 0);
            }

            if (!isDiscoveryResultForCurrentEntry(discoveryResult, activityId, activityOrdinal, entrySequence))
            {
                const ActivityResetCompletionKind completionKind = ActivityResetCompletionKind.NoCommands;
                const string completionReason = "no_contributors_current_entry";
                emitFact(
                    SessionActivityFactKind.ObjectResetCompleted,
                    $"'{activityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                emitSnapshot(
                    "object_reset_completed",
                    $"'{activityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                return new ActivityResetResult(completionKind, completionReason, 0, 0, 0, 0);
            }

            if (discoveryResult.Reports.Count == 0)
            {
                const ActivityResetCompletionKind completionKind = ActivityResetCompletionKind.NoCommands;
                const string completionReason = "no_reports_for_current_entry";
                emitFact(
                    SessionActivityFactKind.ObjectResetCompleted,
                    $"'{activityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                emitSnapshot(
                    "object_reset_completed",
                    $"'{activityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                return new ActivityResetResult(completionKind, completionReason, 0, 0, 0, 0);
            }

            int commandCount = 0;
            int appliedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            int noSupportedGroupsCount = 0;
            int reportEvaluatedCount = 0;

            var resetInventory = context.ResetInventory;
            var resetInventoryValidation = context.ResetInventoryValidation;
            bool hasRequiredContributor = hasRequiredResetContributor(discoveryResult, activityId, activityOrdinal, entrySequence);
            bool hasValidResetInventory =
                resetInventory.IsValid &&
                resetInventoryValidation.IsValid &&
                string.Equals(resetInventory.Id.PipelineId, resetIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(resetInventory.Id.SessionStateId, resetIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(resetInventory.Id.ActivityId, resetIdentity.ActivityId, StringComparison.Ordinal) &&
                resetInventory.Id.EntrySequence == resetIdentity.EntrySequence;

            if (!hasValidResetInventory)
            {
                if (hasRequiredContributor)
                {
                    emitFact(
                        SessionActivityFactKind.ObjectResetFailed,
                        $"'{activityId}' object reset failed reason='required_reset_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{resetInventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{resetInventoryValidation.IsValid.ToString().ToLowerInvariant()}'.");
                    throw new InvalidOperationException(
                        $"required_reset_inventory_missing_or_invalid: activityId='{activityId}' entrySequence='{entrySequence}'.");
                }

                emitFact(
                    SessionActivityFactKind.ObjectResetCompleted,
                    $"'{activityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                emitSnapshot(
                    "object_reset_completed",
                    $"'{activityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                return new ActivityResetResult(ActivityResetCompletionKind.InventoryInvalidOrStale, "inventory_stale_or_invalid", 0, 0, 0, 0);
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                var report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !isReportForCurrentEntry(report, activityId, activityOrdinal, entrySequence))
                {
                    continue;
                }

                reportEvaluatedCount += 1;
                if (report.SupportedResetGroups == null || report.SupportedResetGroups.Count == 0)
                {
                    skippedCount += 1;
                    noSupportedGroupsCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetSkippedOptional,
                        $"'{activityId}' object reset skipped targetId='{report.TargetId}' reason='no_supported_reset_groups'.");
                    continue;
                }

                IActivityObjectResetEndpoint[] endpoints = resolveEndpointsFromInventory(resetInventory, report);
                for (int groupIndex = 0; groupIndex < report.SupportedResetGroups.Count; groupIndex++)
                {
                    var resetGroup = report.SupportedResetGroups[groupIndex];
                    if (resetGroup == ActivityStateResetGroup.Unknown)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{activityId}' reset group cannot be Unknown targetId='{report.TargetId}'.");
                    }

                    ActivityObjectResetCommand resetCommand = new(
                        resetIdentity,
                        report.TargetId,
                        report.RoleId,
                        report.ContributorKind,
                        report.Requiredness,
                        resetGroup,
                        command.Source,
                        command.Reason);
                    if (!resetCommand.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{activityId}' produced invalid object reset command targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
                    }

                    commandCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetCommandIssued,
                        $"'{activityId}' object reset command issued targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroup='{resetGroup}'.");

                    var result = executeObjectResetCommand(resetCommand, endpoints);
                    if (!result.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{activityId}' object reset returned invalid result targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
                    }

                    if (!isObjectResetResultForCurrentEntry(result, activityId, activityOrdinal, entrySequence))
                    {
                        failedCount += 1;
                        emitFact(
                            SessionActivityFactKind.ObjectResetFailed,
                            $"'{activityId}' object reset failed targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='stale_or_foreign_reset_result'.");
                        throw new InvalidOperationException(
                            $"stale_or_foreign_reset_result: activityId='{activityId}' targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
                    }

                    if (result.IsApplied)
                    {
                        appliedCount += 1;
                        emitFact(
                            SessionActivityFactKind.ObjectResetApplied,
                            $"'{activityId}' object reset applied targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroup='{resetGroup}'.");
                        continue;
                    }

                    if (result.IsSkippedOptional)
                    {
                        skippedCount += 1;
                        emitFact(
                            SessionActivityFactKind.ObjectResetSkippedOptional,
                            $"'{activityId}' object reset skipped optional targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                        continue;
                    }

                    failedCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetFailed,
                        $"'{activityId}' object reset failed targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                    throw new InvalidOperationException(
                        $"object_reset_failed: activityId='{activityId}' targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                }
            }

            ActivityResetCompletionKind finalCompletionKind;
            string finalCompletionReason;
            if (appliedCount > 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.Applied;
                finalCompletionReason = "applied";
            }
            else if (commandCount <= 0 && reportEvaluatedCount <= 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.NoCommands;
                finalCompletionReason = "no_reports_for_current_entry";
            }
            else if (commandCount <= 0 && noSupportedGroupsCount > 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.NoApplicableGroups;
                finalCompletionReason = "no_supported_groups";
            }
            else if (commandCount <= 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.NoCommands;
                finalCompletionReason = "no_applicable_reset_groups";
            }
            else if (skippedCount > 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.SkippedOptional;
                finalCompletionReason = "skipped_optional";
            }
            else
            {
                finalCompletionKind = ActivityResetCompletionKind.NoCommands;
                finalCompletionReason = "no_commands";
            }

            emitFact(
                SessionActivityFactKind.ObjectResetCompleted,
                $"'{activityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}'.");
            emitSnapshot(
                "object_reset_completed",
                $"'{activityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}'.");

            return new ActivityResetResult(finalCompletionKind, finalCompletionReason, commandCount, appliedCount, skippedCount, failedCount);
        }
    }
}
