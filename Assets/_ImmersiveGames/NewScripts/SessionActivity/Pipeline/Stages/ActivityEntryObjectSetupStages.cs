using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryObjectSetupStageUtility
    {
        public static SessionActivityIdentity BuildIdentityFromCommandIdentity(
            SessionActivityIdentity identity,
            SessionActivityStage stage)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("SessionActivityIdentity is invalid.");
            }

            return new SessionActivityIdentity(
                identity.PipelineId,
                identity.SessionId,
                identity.ActivityId,
                identity.ActivityOrdinal,
                identity.EntrySequence,
                stage,
                identity.Source);
        }

        public static bool HasLoadedSetForCurrentEntry(
            ActivityContentLoadedSet loadedSet,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            return loadedSet is { IsValid: true, Identity: { Stage: SessionActivityStage.ActivityContentLoadedSetReady } } &&
                string.Equals(loadedSet.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(loadedSet.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(loadedSet.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                loadedSet.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                loadedSet.Identity.EntrySequence == entrySequence;
        }

        public static bool IsDiscoveryResultForCurrentEntryForIdentity(
            ActivityObjectContributorDiscoveryResult result,
            SessionActivityIdentity identity,
            int entrySequence,
            SessionActivityIdentity currentIdentity)
        {
            return result is { IsValid: true, Identity: { IsValid: true } } &&
                string.Equals(result.Identity.PipelineId, currentIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(result.Identity.SessionId, currentIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(result.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                result.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                result.Identity.EntrySequence == entrySequence;
        }

        public static bool IsReportForCurrentEntryForIdentity(
            ActivityObjectContributionReport report,
            SessionActivityIdentity identity,
            int entrySequence,
            SessionActivityIdentity currentIdentity)
        {
            return report is { IsValid: true, Identity: { IsValid: true } } &&
                string.Equals(report.Identity.PipelineId, currentIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(report.Identity.SessionId, currentIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(report.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                report.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                report.Identity.EntrySequence == entrySequence;
        }

        public static bool IsDiscoveryResultForCurrentEntry(
            ActivityObjectContributorDiscoveryResult result,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            return result is { IsValid: true, Identity: { IsValid: true } } &&
                string.Equals(result.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(result.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(result.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                result.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                result.Identity.EntrySequence == entrySequence;
        }

        public static bool IsReportForCurrentEntry(
            ActivityObjectContributionReport report,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            return report is { IsValid: true, Identity: { IsValid: true } } &&
                string.Equals(report.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(report.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(report.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                report.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                report.Identity.EntrySequence == entrySequence;
        }

        public static bool HasRequiredResetContributor(
            ActivityObjectContributorDiscoveryResult discoveryResult,
            SessionActivityIdentity identity)
        {
            if (!discoveryResult.IsValid || discoveryResult.Reports == null)
            {
                return false;
            }

            for (int index = 0; index < discoveryResult.Reports.Count; index++)
            {
                var report = discoveryResult.Reports[index];
                if (!report.IsValid)
                {
                    continue;
                }

                if (!string.Equals(report.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) ||
                    report.Identity.ActivityOrdinal != identity.ActivityOrdinal ||
                    report.Identity.EntrySequence != identity.EntrySequence)
                {
                    continue;
                }

                if (report.Requiredness == ActivitySetupRequirementRequiredness.Required)
                {
                    return true;
                }
            }

            return false;
        }

        public static IActivityObjectResetEndpoint[] ResolveObjectResetEndpointsFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectResetEndpoint>();
            }

            List<IActivityObjectResetEndpoint> endpoints = new();
            HashSet<IActivityObjectResetEndpoint> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                var capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.ResetEndpoint)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference(capability.CapabilityId, out ActivityObjectResetEndpointReference runtimeReference) ||
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

        public static IActivityObjectSnapshotRestoreEndpoint[] ResolveObjectSnapshotRestoreEndpointsFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectSnapshotRestoreEndpoint>();
            }

            List<IActivityObjectSnapshotRestoreEndpoint> endpoints = new();
            HashSet<IActivityObjectSnapshotRestoreEndpoint> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                var capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.SnapshotRestoreEndpoint)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference(capability.CapabilityId, out ActivityObjectSnapshotRestoreEndpointReference runtimeReference) ||
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

        public static ActivityObjectResetResult ExecuteObjectResetCommand(
            ActivityObjectResetCommand command,
            IActivityObjectResetEndpoint[] endpoints)
        {
            if (endpoints == null || endpoints.Length == 0)
            {
                if (command.IsRequired)
                {
                    return new ActivityObjectResetResult(
                        ActivityObjectResetResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "required_reset_endpoint_missing");
                }

                return new ActivityObjectResetResult(
                    ActivityObjectResetResultKind.SkippedOptional,
                    command,
                    command.Source,
                    command.Reason,
                    "optional_reset_endpoint_missing");
            }

            for (int index = 0; index < endpoints.Length; index++)
            {
                var endpoint = endpoints[index];
                if (endpoint == null)
                {
                    continue;
                }

                var result = ApplyObjectResetByIntent(endpoint, command);
                if (!result.IsValid)
                {
                    return new ActivityObjectResetResult(
                        ActivityObjectResetResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "invalid_reset_result");
                }

                return result;
            }

            if (command.IsRequired)
            {
                return new ActivityObjectResetResult(
                    ActivityObjectResetResultKind.Failed,
                    command,
                    command.Source,
                    command.Reason,
                    "required_reset_endpoint_missing");
            }

            return new ActivityObjectResetResult(
                ActivityObjectResetResultKind.SkippedOptional,
                command,
                command.Source,
                command.Reason,
                "optional_reset_endpoint_missing");
        }

        private static ActivityObjectResetResult ApplyObjectResetByIntent(
            IActivityObjectResetEndpoint endpoint,
            ActivityObjectResetCommand command)
        {
            switch (command.ResetIntent)
            {
                case ActivityResetIntent.EntryInitialize when endpoint is IActivityObjectEntryInitializeResetEndpoint entryInitialize:
                    return entryInitialize.ApplyEntryInitializeReset(command);
                case ActivityResetIntent.RuntimeLocalReset when endpoint is IActivityObjectRuntimeLocalResetEndpoint runtimeLocal:
                    return runtimeLocal.ApplyRuntimeLocalReset(command);
                case ActivityResetIntent.RuntimeActivityReset when endpoint is IActivityObjectRuntimeActivityResetEndpoint runtimeActivity:
                    return runtimeActivity.ApplyRuntimeActivityReset(command);
                case ActivityResetIntent.RuntimeActivityTransitionReset when endpoint is IActivityObjectRuntimeActivityTransitionResetEndpoint runtimeActivityTransition:
                    return runtimeActivityTransition.ApplyRuntimeActivityTransitionReset(command);
                case ActivityResetIntent.RuntimeRouteTransitionReset when endpoint is IActivityObjectRuntimeRouteTransitionResetEndpoint runtimeRouteTransition:
                    return runtimeRouteTransition.ApplyRuntimeRouteTransitionReset(command);
                default:
                    return new ActivityObjectResetResult(
                        ActivityObjectResetResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        $"object_reset_intent_handler_missing intent='{command.ResetIntent}' stateProfile='{command.StateProfileKind}' endpoint='{endpoint?.GetType().Name ?? "<null>"}' resetDescriptor='{command.ResetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report'");
            }
        }

        public static bool IsObjectResetResultForCurrentEntry(
            ActivityObjectResetResult result,
            SessionActivityIdentity identity,
            int entrySequence,
            SessionActivityIdentity currentIdentity)
        {
            var resultIdentity = result.Command.Identity;
            return result.IsValid &&
                resultIdentity.IsValid &&
                string.Equals(resultIdentity.PipelineId, currentIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(resultIdentity.SessionId, currentIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(resultIdentity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                resultIdentity.ActivityOrdinal == identity.ActivityOrdinal &&
                resultIdentity.EntrySequence == entrySequence &&
                !string.IsNullOrWhiteSpace(result.Command.TargetId);
        }

        public static ActivityObjectSnapshotRestoreResult ExecuteObjectSnapshotRestoreCommand(
            ActivityObjectSnapshotRestoreCommand command,
            IActivityObjectSnapshotRestoreEndpoint[] endpoints,
            ActivityObjectContributionReport report)
        {
            bool isRequired = report.Requiredness == ActivitySetupRequirementRequiredness.Required;
            if (endpoints == null || endpoints.Length == 0)
            {
                return new ActivityObjectSnapshotRestoreResult(
                    isRequired ? ActivityObjectSnapshotRestoreResultKind.Failed : ActivityObjectSnapshotRestoreResultKind.SkippedOptional,
                    command,
                    false,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    command.Source,
                    command.Reason,
                    isRequired ? "restore_endpoint_missing_required" : "target_has_no_restore_endpoint_optional");
            }

            for (int index = 0; index < endpoints.Length; index++)
            {
                var endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(command.TargetId))
                {
                    continue;
                }

                var result = endpoint.ApplyRestore(command);
                if (!result.IsValid)
                {
                    return new ActivityObjectSnapshotRestoreResult(
                        ActivityObjectSnapshotRestoreResultKind.Failed,
                        command,
                        false,
                        0f,
                        0f,
                        0f,
                        0f,
                        0f,
                        0f,
                        command.Source,
                        command.Reason,
                        "restore_result_invalid_or_failed_required");
                }

                return result;
            }

            return new ActivityObjectSnapshotRestoreResult(
                isRequired ? ActivityObjectSnapshotRestoreResultKind.Failed : ActivityObjectSnapshotRestoreResultKind.SkippedOptional,
                command,
                false,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                command.Source,
                command.Reason,
                isRequired ? "restore_endpoint_missing_required" : "target_has_no_restore_endpoint_optional");
        }

        public static bool IsObjectSnapshotRestoreResultForCurrentEntry(
            ActivityObjectSnapshotRestoreResult result,
            SessionActivityIdentity identity,
            int entrySequence,
            SessionActivityIdentity currentIdentity)
        {
            var command = result.Command;
            return result.IsValid &&
                command.Identity.IsValid &&
                string.Equals(command.Identity.PipelineId, currentIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(command.Identity.SessionId, currentIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(command.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                command.Identity.ActivityOrdinal == identity.ActivityOrdinal &&
                command.Identity.EntrySequence == entrySequence &&
                !string.IsNullOrWhiteSpace(command.TargetId);
        }

        public static string FormatCapabilityKindsSummary(IReadOnlyList<ActivityCapabilityDescriptor> capabilities)
        {
            if (capabilities == null || capabilities.Count == 0)
            {
                return "<none>";
            }

            Dictionary<ActivityCapabilityKind, int> countsByKind = new();
            for (int index = 0; index < capabilities.Count; index++)
            {
                var kind = capabilities[index].CapabilityKind;
                countsByKind.TryGetValue(kind, out int count);
                countsByKind[kind] = count + 1;
            }

            List<ActivityCapabilityKind> kinds = new(countsByKind.Keys);
            kinds.Sort();
            List<string> segments = new(kinds.Count);
            for (int index = 0; index < kinds.Count; index++)
            {
                var kind = kinds[index];
                segments.Add($"{kind}:{countsByKind[kind]}");
            }

            return string.Join(",", segments);
        }

        public static string ToCoordinateSpaceToken(ActivityObjectSnapshotCoordinateSpace coordinateSpace)
        {
            return coordinateSpace == ActivityObjectSnapshotCoordinateSpace.WorldTransform
                ? "world_transform"
                : "unknown";
        }

        public static string JoinValues(HashSet<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "<none>";
            }

            List<string> ordered = new(values);
            ordered.Sort(StringComparer.Ordinal);
            return string.Join(",", ordered);
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
                var entry = metadata[index];
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
