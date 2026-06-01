using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityObjectReleaseStageCommand
    {
        public ActivityObjectReleaseStageCommand(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            int entrySequence)
        {
            Definition = definition;
            Command = command;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
        }

        public SessionActivityDefinition Definition { get; }
        public SessionActivityCommand Command { get; }
        public int EntrySequence { get; }
        public string Source => Command.Source;
        public string Reason => Command.Reason;

        public bool IsValid =>
            Definition.IsValid &&
            Command.Identity.IsValid &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(Source);
    }

    internal readonly struct ActivityObjectReleaseStageResult
    {
        public ActivityObjectReleaseStageResult(
            bool completed,
            SessionActivityIdentity identity,
            int commandCount,
            int appliedCount,
            int skippedCount,
            int failedCount,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            CommandCount = commandCount < 0 ? 0 : commandCount;
            AppliedCount = appliedCount < 0 ? 0 : appliedCount;
            SkippedCount = skippedCount < 0 ? 0 : skippedCount;
            FailedCount = failedCount < 0 ? 0 : failedCount;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int CommandCount { get; }
        public int AppliedCount { get; }
        public int SkippedCount { get; }
        public int FailedCount { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal static class ActivityObjectReleaseStage
    {
        public static ActivityObjectReleaseStageResult Execute(
            ActivityObjectReleaseStageCommand command,
            IActivityEntryRuntimeEndpoint endpoint,
            ActivityObjectExitRuntimeState runtimeState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectReleaseStageCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.EntrySequence;
            ActivityObjectContributorDiscoveryResult discoveryResult = runtimeState.CurrentContributorDiscoveryResult;
            SessionActivityIdentity releaseIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivityContentReleaseStarted, entrySequence);
            endpoint.SetCurrentIdentity(releaseIdentity, SessionActivityStage.ActivityContentReleaseStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ObjectReleaseStarted,
                releaseIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object release started.");
            DebugUtility.Log(
                typeof(ActivityObjectReleaseStage),
                $"[OBS][ActivityObjectReleaseStage] event='ActivityObjectReleaseStarted' owner='ActivityObjectReleaseStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            endpoint.EmitSnapshot(
                snapshots,
                "object_release_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object release started.");

            if (!discoveryResult.IsValid ||
                !IsDiscoveryResultForCurrentEntry(discoveryResult, releaseIdentity, definition, entrySequence) ||
                discoveryResult.Reports.Count == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectReleaseCompleted,
                    releaseIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object release completed with no contributors for current entry.");
                DebugUtility.Log(
                    typeof(ActivityObjectReleaseStage),
                    $"[OBS][ActivityObjectReleaseStage] event='ActivityObjectReleaseCompleted' owner='ActivityObjectReleaseStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' commandCount='0' appliedCount='0' skippedCount='0' failedCount='0' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                endpoint.EmitSnapshot(
                    snapshots,
                    "object_release_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object release completed with no contributors for current entry.");
                return new ActivityObjectReleaseStageResult(
                    completed: true,
                    identity: releaseIdentity,
                    commandCount: 0,
                    appliedCount: 0,
                    skippedCount: 0,
                    failedCount: 0,
                    reason: "no_contributors");
            }

            int commandCount = 0;
            int appliedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            ActivityCapabilityInventory releaseInventory = runtimeState.CurrentInventoryPreview;
            ActivityCapabilityInventoryValidationResult releaseInventoryValidation = runtimeState.CurrentInventoryPreviewValidation;
            bool hasValidReleaseInventory =
                releaseInventory.IsValid &&
                releaseInventoryValidation.IsValid &&
                string.Equals(releaseInventory.Id.PipelineId, releaseIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(releaseInventory.Id.SessionStateId, releaseIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(releaseInventory.Id.ActivityId, releaseIdentity.ActivityId, StringComparison.Ordinal) &&
                releaseInventory.Id.EntrySequence == releaseIdentity.EntrySequence;

            if (!hasValidReleaseInventory)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectReleaseFailed,
                    releaseIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object release failed reason='release_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{releaseInventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{releaseInventoryValidation.IsValid.ToString().ToLowerInvariant()}'.");
                throw new InvalidOperationException(
                    $"release_inventory_missing_or_invalid: activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !IsReportForCurrentEntry(report, releaseIdentity, definition, entrySequence))
                {
                    continue;
                }

                if (report.SupportedReleaseKinds == null || report.SupportedReleaseKinds.Count == 0)
                {
                    skippedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectReleaseSkippedOptional,
                        releaseIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' object release skipped targetId='{report.TargetId}' reason='no_supported_release_kinds'.");
                    continue;
                }

                IActivityObjectReleaseEndpoint[] endpoints = ResolveObjectReleaseEndpointsFromInventory(releaseInventory, report);

                for (int kindIndex = 0; kindIndex < report.SupportedReleaseKinds.Count; kindIndex++)
                {
                    ActivityReleaseRequirementKind releaseKind = report.SupportedReleaseKinds[kindIndex];
                    if (releaseKind == ActivityReleaseRequirementKind.Unknown)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' release kind cannot be Unknown targetId='{report.TargetId}'.");
                    }

                    ActivityObjectReleaseCommand releaseCommand = new(
                        releaseIdentity,
                        report.TargetId,
                        report.RoleId,
                        report.ContributorKind,
                        report.Requiredness,
                        releaseKind,
                        command.Source,
                        command.Reason);
                    if (!releaseCommand.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' produced invalid object release command targetId='{report.TargetId}' releaseKind='{releaseKind}'.");
                    }

                    commandCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectReleaseCommandIssued,
                        releaseIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' object release command issued targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' releaseKind='{releaseKind}'.");

                    ActivityObjectReleaseResult result = ExecuteObjectReleaseCommand(releaseCommand, endpoints);
                    if (!result.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' object release returned invalid result targetId='{report.TargetId}' releaseKind='{releaseKind}'.");
                    }

                    if (!IsObjectReleaseResultAcceptedForIssuedCommand(result, releaseCommand, definition, entrySequence, releaseIdentity))
                    {
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectReleaseRejectedForeignOrStale,
                            releaseIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' object release rejected foreign/stale targetId='{report.TargetId}' releaseKind='{releaseKind}' reason='stale_or_foreign_release_result'.");
                        continue;
                    }

                    if (result.IsApplied)
                    {
                        appliedCount += 1;
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectReleaseApplied,
                            releaseIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' object release applied targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' releaseKind='{releaseKind}'.");
                        continue;
                    }

                    if (result.IsSkippedOptional)
                    {
                        skippedCount += 1;
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectReleaseSkippedOptional,
                            releaseIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' object release skipped optional targetId='{report.TargetId}' releaseKind='{releaseKind}' reason='{result.Message}'.");
                        continue;
                    }

                    failedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectReleaseFailed,
                        releaseIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' object release failed targetId='{report.TargetId}' releaseKind='{releaseKind}' reason='{result.Message}'.");
                    throw new InvalidOperationException(
                        $"object_release_failed: activityId='{definition.ActivityId}' targetId='{report.TargetId}' releaseKind='{releaseKind}' reason='{result.Message}'.");
                }
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ObjectReleaseCompleted,
                releaseIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object release completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}'.");
            DebugUtility.Log(
                typeof(ActivityObjectReleaseStage),
                $"[OBS][ActivityObjectReleaseStage] event='ActivityObjectReleaseCompleted' owner='ActivityObjectReleaseStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' source='{command.Source}' reason='{command.Reason}'.",
                failedCount == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
            endpoint.EmitSnapshot(
                snapshots,
                "object_release_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object release completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}'.");
            return new ActivityObjectReleaseStageResult(
                completed: true,
                identity: releaseIdentity,
                commandCount: commandCount,
                appliedCount: appliedCount,
                skippedCount: skippedCount,
                failedCount: failedCount,
                reason: failedCount == 0 ? "completed" : "completed_with_failures");
        }

        private static IActivityObjectReleaseEndpoint[] ResolveObjectReleaseEndpointsFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectReleaseEndpoint>();
            }

            List<IActivityObjectReleaseEndpoint> endpoints = new();
            HashSet<IActivityObjectReleaseEndpoint> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.ReleaseEndpoint)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference<ActivityObjectReleaseEndpointReference>(capability.CapabilityId, out ActivityObjectReleaseEndpointReference runtimeReference) ||
                    runtimeReference.Endpoint == null)
                {
                    continue;
                }

                if (unique.Add(runtimeReference.Endpoint))
                {
                    endpoints.Add(runtimeReference.Endpoint);
                }
            }

            return endpoints.ToArray();
        }

        private static ActivityObjectReleaseResult ExecuteObjectReleaseCommand(
            ActivityObjectReleaseCommand command,
            IActivityObjectReleaseEndpoint[] endpoints)
        {
            if (endpoints == null || endpoints.Length == 0)
            {
                if (command.IsRequired)
                {
                    return new ActivityObjectReleaseResult(
                        ActivityObjectReleaseResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "required_release_endpoint_missing");
                }

                return new ActivityObjectReleaseResult(
                    ActivityObjectReleaseResultKind.SkippedOptional,
                    command,
                    command.Source,
                    command.Reason,
                    "optional_release_endpoint_missing");
            }

            bool hasSupportingEndpoint = false;
            for (int index = 0; index < endpoints.Length; index++)
            {
                IActivityObjectReleaseEndpoint endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(command.ReleaseKind))
                {
                    continue;
                }

                hasSupportingEndpoint = true;
                ActivityObjectReleaseResult result = endpoint.ApplyRelease(command);
                if (!result.IsValid)
                {
                    return new ActivityObjectReleaseResult(
                        ActivityObjectReleaseResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "invalid_release_result");
                }

                return result;
            }

            if (command.IsRequired)
            {
                return new ActivityObjectReleaseResult(
                    ActivityObjectReleaseResultKind.Failed,
                    command,
                    command.Source,
                    command.Reason,
                    hasSupportingEndpoint ? "required_release_not_applied" : "required_release_kind_not_supported");
            }

            return new ActivityObjectReleaseResult(
                ActivityObjectReleaseResultKind.SkippedOptional,
                command,
                command.Source,
                command.Reason,
                hasSupportingEndpoint ? "optional_release_not_applied" : "optional_release_kind_not_supported");
        }

        private static bool IsObjectReleaseResultAcceptedForIssuedCommand(
            ActivityObjectReleaseResult result,
            ActivityObjectReleaseCommand issuedCommand,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityIdentity releaseIdentity)
        {
            return IsObjectReleaseResultForCurrentEntry(result, definition, entrySequence, releaseIdentity) &&
                   issuedCommand.IsValid &&
                   string.Equals(result.Command.TargetId, issuedCommand.TargetId, StringComparison.Ordinal) &&
                   result.Command.ReleaseKind == issuedCommand.ReleaseKind;
        }

        private static bool IsObjectReleaseResultForCurrentEntry(
            ActivityObjectReleaseResult result,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityIdentity releaseIdentity)
        {
            SessionActivityIdentity identity = result.Command.Identity;
            return result.IsValid &&
                   identity.IsValid &&
                   releaseIdentity.IsValid &&
                   string.Equals(identity.PipelineId, releaseIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(identity.SessionId, releaseIdentity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   identity.EntrySequence == entrySequence &&
                   string.Equals(result.Command.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   result.Command.ActivityOrdinal == definition.ActivityOrdinal &&
                   result.Command.EntrySequence == entrySequence &&
                   string.Equals(result.Command.PipelineId, releaseIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(result.Command.SessionStateId, releaseIdentity.SessionId, StringComparison.Ordinal) &&
                   !string.IsNullOrWhiteSpace(result.Command.TargetId) &&
                   result.Command.ReleaseKind != ActivityReleaseRequirementKind.Unknown;
        }

        private static bool IsDiscoveryResultForCurrentEntry(
            ActivityObjectContributorDiscoveryResult result,
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            return result.IsValid &&
                   result.Identity.IsValid &&
                   identity.IsValid &&
                   string.Equals(result.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   result.Identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   result.Identity.EntrySequence == entrySequence;
        }

        private static bool IsReportForCurrentEntry(
            ActivityObjectContributionReport report,
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            return report.IsValid &&
                   report.Identity.IsValid &&
                   identity.IsValid &&
                   string.Equals(report.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(report.SessionStateId, identity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(report.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   report.ActivityOrdinal == definition.ActivityOrdinal &&
                   report.EntrySequence == entrySequence;
        }

        private static bool TryGetPolicyValue(IReadOnlyList<ActivityCapabilityPolicyEntry> metadata, string key, out string value)
        {
            value = string.Empty;
            if (metadata == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            for (int index = 0; index < metadata.Count; index++)
            {
                ActivityCapabilityPolicyEntry entry = metadata[index];
                if (string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    value = entry.Value;
                    return !string.IsNullOrWhiteSpace(value);
                }
            }

            return false;
        }
    }
}
