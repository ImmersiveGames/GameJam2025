using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Policies;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityResetStage
    {
        public static ActivityResetResult Execute(
            ActivityResetCommand command,
            ActivityResetScopePlan resetScopePlan,
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

            if (!resetScopePlan.IsValid)
            {
                throw new InvalidOperationException("ActivityResetScopePlan is invalid for QA object reset execution.");
            }

            if (!resetScopePlan.Identity.CycleKey.Equals(command.Identity.CycleKey))
            {
                throw new InvalidOperationException(
                    $"ActivityResetStage requires reset scope plan from the current activity cycle. activityId='{command.ActivityId}'.");
            }

            SessionActivityIdentity resetIdentity = command.Identity;
            string activityId = command.ActivityId;
            int activityOrdinal = command.ActivityOrdinal;
            int entrySequence = resetIdentity.EntrySequence;
            ActivityObjectContributorDiscoveryResult discoveryResult = context.DiscoveryResult;

            emitFact(
                SessionActivityFactKind.ObjectResetStarted,
                $"'{activityId}' object reset started resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' boundaryEligibilityRequired='{ActivityResetBoundaryPolicy.ResolveEligibility(resetScopePlan.BoundaryKind)}' behaviorMode='ResetIntentStateProfilePolicy'.");
            emitSnapshot(
                "object_reset_started",
                $"'{activityId}' object reset started resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' boundaryEligibilityRequired='{ActivityResetBoundaryPolicy.ResolveEligibility(resetScopePlan.BoundaryKind)}' behaviorMode='ResetIntentStateProfilePolicy'.");

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
            int noEndpointCount = 0;
            int filteredByEligibilityCount = 0;
            int reportEvaluatedCount = 0;

            ActivityCapabilityInventory resetInventory = context.ResetInventory;
            ActivityCapabilityInventoryValidationResult resetInventoryValidation = context.ResetInventoryValidation;
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
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !isReportForCurrentEntry(report, activityId, activityOrdinal, entrySequence))
                {
                    continue;
                }

                reportEvaluatedCount += 1;

                if (!ActivityResetBoundaryPolicy.AllowsReset(resetScopePlan, report.ResetBoundaryEligibility))
                {
                    skippedCount += 1;
                    filteredByEligibilityCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetSkippedOptional,
                        $"'{activityId}' object reset skipped targetId='{report.TargetId}' reason='reset_endpoint_filtered_by_boundary_policy' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' resetBoundaryEligibility='{ActivityResetBoundaryPolicy.FormatResetBoundaryEligibility(report.ResetBoundaryEligibility)}'.");
                    continue;
                }

                IActivityObjectResetEndpoint[] endpoints = resolveEndpointsFromInventory(resetInventory, report);
                string resetDescriptorMetadata = ResolveResetDescriptorMetadata(report);
                if (endpoints == null || endpoints.Length == 0)
                {
                    noEndpointCount += 1;
                    if (report.Requiredness == ActivitySetupRequirementRequiredness.Required)
                    {
                        failedCount += 1;
                        emitFact(
                            SessionActivityFactKind.ObjectResetFailed,
                            $"'{activityId}' object reset failed targetId='{report.TargetId}' reason='required_reset_endpoint_missing' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
                        throw new InvalidOperationException(
                            $"required_reset_endpoint_missing: activityId='{activityId}' targetId='{report.TargetId}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}'.");
                    }

                    skippedCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetSkippedOptional,
                        $"'{activityId}' object reset skipped targetId='{report.TargetId}' reason='reset_endpoint_missing' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}'.");
                    continue;
                }

                ActivityObjectResetCommand resetCommand = new(
                    resetIdentity,
                    report.TargetId,
                    report.RoleId,
                    report.ContributorKind,
                    report.Requiredness,
                    resetDescriptorMetadata,
                    resetScopePlan,
                    command.Source,
                    command.Reason);
                if (!resetCommand.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Activity '{activityId}' produced invalid object reset command targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
                }

                commandCount += 1;
                emitFact(
                    SessionActivityFactKind.ObjectResetCommandIssued,
                    $"'{activityId}' object reset command issued targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetDescriptor='{resetDescriptorMetadata}' resetDescriptors='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");

                ActivityObjectResetResult result = executeObjectResetCommand(resetCommand, endpoints);
                if (!result.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Activity '{activityId}' object reset returned invalid result targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report'.");
                }

                if (!isObjectResetResultForCurrentEntry(result, activityId, activityOrdinal, entrySequence))
                {
                    failedCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetFailed,
                        $"'{activityId}' object reset failed targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' reason='stale_or_foreign_reset_result'.");
                    throw new InvalidOperationException(
                        $"stale_or_foreign_reset_result: activityId='{activityId}' targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
                }

                if (result.IsApplied)
                {
                    appliedCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetApplied,
                        $"'{activityId}' object reset applied targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetDescriptor='{resetDescriptorMetadata}' resetDescriptors='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
                    continue;
                }

                if (result.IsSkippedOptional)
                {
                    skippedCount += 1;
                    emitFact(
                        SessionActivityFactKind.ObjectResetSkippedOptional,
                        $"'{activityId}' object reset skipped optional targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' reason='{result.Message}'.");
                    continue;
                }

                failedCount += 1;
                emitFact(
                    SessionActivityFactKind.ObjectResetFailed,
                    $"'{activityId}' object reset failed targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' reason='{result.Message}'.");
                throw new InvalidOperationException(
                    $"object_reset_failed: activityId='{activityId}' targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' reason='{result.Message}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
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
            else if (commandCount <= 0 && filteredByEligibilityCount > 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.NoApplicableGroups;
                finalCompletionReason = "reset_endpoints_filtered_by_boundary_policy";
            }
            else if (commandCount <= 0 && noEndpointCount > 0)
            {
                finalCompletionKind = ActivityResetCompletionKind.NoApplicableGroups;
                finalCompletionReason = "reset_endpoint_missing";
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
                $"'{activityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
            emitSnapshot(
                "object_reset_completed",
                $"'{activityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");

            return new ActivityResetResult(finalCompletionKind, finalCompletionReason, commandCount, appliedCount, skippedCount, failedCount);
        }

        private static string ResolveResetDescriptorMetadata(ActivityObjectContributionReport report)
        {
            return report.IsValid ? "endpoint_inventory" : "<none>";
        }
    }
}
