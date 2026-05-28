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
            Func<ActivityObjectContributorDiscoveryResult, SessionActivityDefinition, int, bool> isDiscoveryResultForCurrentEntry,
            Func<ActivityObjectContributionReport, SessionActivityDefinition, int, bool> isReportForCurrentEntry,
            Func<ActivityObjectContributorDiscoveryResult, SessionActivityDefinition, int, bool> hasRequiredResetContributor,
            Func<ActivityCapabilityInventory, ActivityObjectContributionReport, IActivityObjectResetEndpoint[]> resolveEndpointsFromInventory,
            Func<ActivityObjectResetCommand, IActivityObjectResetEndpoint[], ActivityObjectResetResult> executeObjectResetCommand,
            Func<ActivityObjectResetResult, SessionActivityDefinition, int, bool> isObjectResetResultForCurrentEntry,
            Action<SessionActivityFactKind, string> emitFact,
            Action<string, string> emitSnapshot)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityResetCommand is invalid.");
            }

            SessionActivityDefinition definition = command.Definition;
            SessionActivityIdentity resetIdentity = command.Identity;
            int entrySequence = resetIdentity.EntrySequence;
            ActivityObjectContributorDiscoveryResult discoveryResult = context.DiscoveryResult;

            emitFact(
                SessionActivityFactKind.ObjectResetStarted,
                $"'{definition.ActivityId}' object reset started.");
            emitSnapshot(
                "object_reset_started",
                $"'{definition.ActivityId}' object reset started.");

            if (!discoveryResult.IsValid)
            {
                const ActivityResetCompletionKind completionKind = ActivityResetCompletionKind.NoCommands;
                const string completionReason = "no_discovery_for_current_entry";
                emitFact(
                    SessionActivityFactKind.ObjectResetCompleted,
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                emitSnapshot(
                    "object_reset_completed",
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                return new ActivityResetResult(completionKind, completionReason, 0, 0, 0, 0);
            }

            if (!isDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence))
            {
                const ActivityResetCompletionKind completionKind = ActivityResetCompletionKind.NoCommands;
                const string completionReason = "no_contributors_current_entry";
                emitFact(
                    SessionActivityFactKind.ObjectResetCompleted,
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                emitSnapshot(
                    "object_reset_completed",
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                return new ActivityResetResult(completionKind, completionReason, 0, 0, 0, 0);
            }

            if (discoveryResult.Reports.Count == 0)
            {
                const ActivityResetCompletionKind completionKind = ActivityResetCompletionKind.NoCommands;
                const string completionReason = "no_reports_for_current_entry";
                emitFact(
                    SessionActivityFactKind.ObjectResetCompleted,
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                emitSnapshot(
                    "object_reset_completed",
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{completionKind}' completionReason='{completionReason}'.");
                return new ActivityResetResult(completionKind, completionReason, 0, 0, 0, 0);
            }

            int commandCount = 0;
            int appliedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            int noSupportedGroupsCount = 0;
            int reportEvaluatedCount = 0;

            ActivityCapabilityInventory resetInventory = context.ResetInventory;
            ActivityCapabilityInventoryValidationResult resetInventoryValidation = context.ResetInventoryValidation;
            bool hasRequiredContributor = hasRequiredResetContributor(discoveryResult, definition, entrySequence);
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
                        $"'{definition.ActivityId}' object reset failed reason='required_reset_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{resetInventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{resetInventoryValidation.IsValid.ToString().ToLowerInvariant()}'.");
                    throw new InvalidOperationException(
                        $"required_reset_inventory_missing_or_invalid: activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                emitFact(
                    SessionActivityFactKind.ObjectResetCompleted,
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                emitSnapshot(
                    "object_reset_completed",
                    $"'{definition.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                return new ActivityResetResult(ActivityResetCompletionKind.InventoryInvalidOrStale, "inventory_stale_or_invalid", 0, 0, 0, 0);
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !isReportForCurrentEntry(report, definition, entrySequence))
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
                        $"'{definition.ActivityId}' object reset skipped targetId='{report.TargetId}' reason='no_supported_reset_groups'.");
                    continue;
                }

                IActivityObjectResetEndpoint[] endpoints = resolveEndpointsFromInventory(resetInventory, report);
                for (int groupIndex = 0; groupIndex < report.SupportedResetGroups.Count; groupIndex++)
                {
                    ActivityStateResetGroup resetGroup = report.SupportedResetGroups[groupIndex];
                    if (resetGroup == ActivityStateResetGroup.Unknown)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' reset group cannot be Unknown targetId='{report.TargetId}'.");
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
                            $"Activity '{definition.ActivityId}' produced invalid object reset command targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
                    }

                    commandCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetCommandIssued,
                        $"'{definition.ActivityId}' object reset command issued targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroup='{resetGroup}'.");

                    ActivityObjectResetResult result = executeObjectResetCommand(resetCommand, endpoints);
                    if (!result.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' object reset returned invalid result targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
                    }

                    if (!isObjectResetResultForCurrentEntry(result, definition, entrySequence))
                    {
                        failedCount += 1;
                        emitFact(
                            SessionActivityFactKind.ObjectResetFailed,
                            $"'{definition.ActivityId}' object reset failed targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='stale_or_foreign_reset_result'.");
                        throw new InvalidOperationException(
                            $"stale_or_foreign_reset_result: activityId='{definition.ActivityId}' targetId='{report.TargetId}' resetGroup='{resetGroup}'.");
                    }

                    if (result.IsApplied)
                    {
                        appliedCount += 1;
                        emitFact(
                            SessionActivityFactKind.ObjectResetApplied,
                            $"'{definition.ActivityId}' object reset applied targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroup='{resetGroup}'.");
                        continue;
                    }

                    if (result.IsSkippedOptional)
                    {
                        skippedCount += 1;
                        emitFact(
                            SessionActivityFactKind.ObjectResetSkippedOptional,
                            $"'{definition.ActivityId}' object reset skipped optional targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                        continue;
                    }

                    failedCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetFailed,
                        $"'{definition.ActivityId}' object reset failed targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
                    throw new InvalidOperationException(
                        $"object_reset_failed: activityId='{definition.ActivityId}' targetId='{report.TargetId}' resetGroup='{resetGroup}' reason='{result.Message}'.");
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
                $"'{definition.ActivityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}'.");
            emitSnapshot(
                "object_reset_completed",
                $"'{definition.ActivityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}'.");

            return new ActivityResetResult(finalCompletionKind, finalCompletionReason, commandCount, appliedCount, skippedCount, failedCount);
        }
    }
}
