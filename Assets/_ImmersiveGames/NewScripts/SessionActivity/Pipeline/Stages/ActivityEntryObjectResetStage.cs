using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Policies;
using static _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages.ActivityEntryObjectSetupStageUtility;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryObjectResetStage
    {
        public static void Execute(
            ActivityEntryObjectSetupCommand command,
            ActivityResetScopePlan resetScopePlan,
            ActivityObjectContributorDiscoveryResult discoveryResult,
            ActivityCapabilityInventory inventory,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            if (!resetScopePlan.IsValid)
            {
                throw new InvalidOperationException("ActivityResetScopePlan is invalid for object reset execution.");
            }

            if (!resetScopePlan.Identity.CycleKey.Equals(command.Identity.CycleKey))
            {
                throw new InvalidOperationException(
                    $"ActivityEntryObjectResetStage requires reset scope plan from the current activity cycle. activityId='{command.ActivityId}'.");
            }

            SessionActivityIdentity resetIdentity = command.Identity;
            int entrySequence = resetIdentity.EntrySequence;
            endpoint.SetCurrentIdentity(resetIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ObjectResetStarted,
                resetIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' object reset started resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' boundaryEligibilityRequired='{ActivityResetBoundaryPolicy.ResolveEligibility(resetScopePlan.BoundaryKind)}' behaviorMode='ResetIntentStateProfilePolicy'.");
            endpoint.EmitSnapshot(
                snapshots,
                "object_reset_started",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' object reset started resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' boundaryEligibilityRequired='{ActivityResetBoundaryPolicy.ResolveEligibility(resetScopePlan.BoundaryKind)}' behaviorMode='ResetIntentStateProfilePolicy'.");

            if (!IsDiscoveryResultForCurrentEntryForIdentity(discoveryResult, resetIdentity, entrySequence, resetIdentity))
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCompleted,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_contributors_current_entry'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_reset_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_contributors_current_entry'.");
                return;
            }

            if (discoveryResult.Reports.Count == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCompleted,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_reports_for_current_entry'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_reset_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.NoCommands}' completionReason='no_reports_for_current_entry'.");
                return;
            }

            int commandCount = 0;
            int appliedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            int noEndpointCount = 0;
            int filteredByEligibilityCount = 0;
            int reportEvaluatedCount = 0;
            bool hasRequiredContributor = HasRequiredResetContributor(discoveryResult, resetIdentity);
            bool hasValidResetInventory =
                inventory.IsValid &&
                string.Equals(inventory.Id.PipelineId, resetIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.SessionStateId, resetIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.ActivityId, resetIdentity.ActivityId, StringComparison.Ordinal) &&
                inventory.Id.EntrySequence == resetIdentity.EntrySequence;

            if (!hasValidResetInventory)
            {
                if (hasRequiredContributor)
                {
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetFailed,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset failed reason='required_reset_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{inventory.IsValid.ToString().ToLowerInvariant()}'.");
                    throw new InvalidOperationException(
                        $"required_reset_inventory_missing_or_invalid: activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
                }

                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCompleted,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_reset_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset completed commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' completionKind='{ActivityResetCompletionKind.InventoryInvalidOrStale}' completionReason='inventory_stale_or_invalid'.");
                return;
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!IsReportForCurrentEntryForIdentity(report, resetIdentity, entrySequence, resetIdentity))
                {
                    continue;
                }

                reportEvaluatedCount += 1;

                if (!ActivityResetBoundaryPolicy.AllowsReset(resetScopePlan, report.ResetBoundaryEligibility))
                {
                    skippedCount += 1;
                    filteredByEligibilityCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetSkippedOptional,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset skipped targetId='{report.TargetId}' reason='reset_endpoint_filtered_by_boundary_policy' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' resetBoundaryEligibility='{ActivityResetBoundaryPolicy.FormatResetBoundaryEligibility(report.ResetBoundaryEligibility)}'.");
                    continue;
                }

                IActivityObjectResetEndpoint[] endpoints = ResolveObjectResetEndpointsFromInventory(inventory, report);
                string resetDescriptorMetadata = ResolveResetDescriptorMetadata(report);
                if (endpoints == null || endpoints.Length == 0)
                {
                    noEndpointCount += 1;
                    if (report.Requiredness == ActivitySetupRequirementRequiredness.Required)
                    {
                        failedCount += 1;
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectResetFailed,
                            resetIdentity,
                            command.Source,
                            command.Reason,
                            $"'{command.ActivityId}' object reset failed targetId='{report.TargetId}' reason='required_reset_endpoint_missing' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
                        throw new InvalidOperationException(
                            $"required_reset_endpoint_missing: activityId='{command.ActivityId}' targetId='{report.TargetId}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}'.");
                    }

                    skippedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetSkippedOptional,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset skipped targetId='{report.TargetId}' reason='reset_endpoint_missing' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}'.");
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
                        $"Activity '{command.ActivityId}' produced invalid object reset command targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
                }

                commandCount += 1;
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetCommandIssued,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset command issued targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetDescriptor='{resetDescriptorMetadata}' resetDescriptors='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");

                ActivityObjectResetResult result = ExecuteObjectResetCommand(resetCommand, endpoints);
                if (!IsObjectResetResultForCurrentEntry(result, resetIdentity, entrySequence, resetIdentity))
                {
                    failedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetFailed,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset failed targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' reason='stale_or_foreign_reset_result'.");
                    throw new InvalidOperationException(
                        $"stale_or_foreign_reset_result: activityId='{command.ActivityId}' targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
                }

                if (result.IsApplied)
                {
                    appliedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetApplied,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset applied targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetDescriptor='{resetDescriptorMetadata}' resetDescriptors='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
                    continue;
                }

                if (result.IsSkippedOptional)
                {
                    skippedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectResetSkippedOptional,
                        resetIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' object reset skipped optional targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' reason='{result.Message}'.");
                    continue;
                }

                failedCount += 1;
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectResetFailed,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' object reset failed targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' reason='{result.Message}'.");
                throw new InvalidOperationException(
                    $"object_reset_failed: activityId='{command.ActivityId}' targetId='{report.TargetId}' resetDescriptor='{resetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' reason='{result.Message}'.");
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

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ObjectResetCompleted,
                resetIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "object_reset_completed",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' object reset completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' completionKind='{finalCompletionKind}' completionReason='{finalCompletionReason}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}'.");
        }

        private static string ResolveResetDescriptorMetadata(ActivityObjectContributionReport report)
        {
            return report.IsValid ? "endpoint_inventory" : "<none>";
        }
    }
}
