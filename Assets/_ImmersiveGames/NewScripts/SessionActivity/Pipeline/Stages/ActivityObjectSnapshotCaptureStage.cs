using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityObjectSnapshotCaptureStageCommand
    {
        public ActivityObjectSnapshotCaptureStageCommand(
            SessionActivityIdentity identity,
            SessionActivityCommand command,
            int entrySequence,
            string snapshotSchemaId)
        {
            Identity = identity;
            Command = command;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            SnapshotSchemaId = Normalize(snapshotSchemaId);
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityCommand Command { get; }
        public int EntrySequence { get; }
        public string SnapshotSchemaId { get; }
        public string Source => Command.Source;
        public string Reason => Command.Reason;

        public bool IsValid =>
            Identity.IsValid &&
            Command.Identity.IsValid &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(SnapshotSchemaId) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal readonly struct ActivityObjectSnapshotCaptureStageResult
    {
        public ActivityObjectSnapshotCaptureStageResult(
            bool completed,
            SessionActivityIdentity identity,
            int capturedCount,
            int failedCount,
            bool hasTransformPayload,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            CapturedCount = capturedCount < 0 ? 0 : capturedCount;
            FailedCount = failedCount < 0 ? 0 : failedCount;
            HasTransformPayload = hasTransformPayload;
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public int CapturedCount { get; }
        public int FailedCount { get; }
        public bool HasTransformPayload { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal static class ActivityObjectSnapshotCaptureStage
    {
        public static ActivityObjectSnapshotCaptureStageResult Execute(
            ActivityObjectSnapshotCaptureStageCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            ActivityObjectExitRuntimeState runtimeState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectSnapshotCaptureStageCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            SessionActivityIdentity identity = command.Identity;
            int entrySequence = command.EntrySequence;
            ActivityObjectContributorDiscoveryResult discoveryResult = runtimeState.CurrentContributorDiscoveryResult;
            SessionActivityIdentity captureIdentity = endpoint.BuildIdentity(
                definition,
                SessionActivityStage.ActivityObjectSnapshotCaptureStarted,
                entrySequence);
            endpoint.SetCurrentIdentity(captureIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotCaptureStarted,
                captureIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot capture started.");
            DebugUtility.Log(
                typeof(ActivityObjectSnapshotCaptureStage),
                $"[OBS][ActivityObjectSnapshotCaptureStage] event='ActivityObjectSnapshotCaptureStarted' owner='ActivityObjectSnapshotCaptureStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            endpoint.EmitSnapshot(
                snapshots,
                "activity_object_snapshot_capture_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot capture started.");

            SessionActivityIdentity completedIdentity;
            if (!discoveryResult.IsValid ||
                !IsDiscoveryResultForCurrentEntry(discoveryResult, captureIdentity, entrySequence) ||
                discoveryResult.Reports.Count == 0)
            {
                runtimeState.SetSnapshotPayloadForSaveOnExit(
                    default,
                    captureFailed: false,
                    failureDetail: string.Empty,
                    definition.ActivityId,
                    entrySequence,
                    "ActivityObjectSnapshotCaptureStage",
                    "activity_object_snapshot_capture_skipped_no_discovery");
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(
                    definition,
                    SessionActivityStage.ActivityObjectSnapshotCaptureSkippedNoProviders,
                    entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureSkippedNoProviders);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotCaptureSkippedNoProviders,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot capture skipped reason='no_discovery_result'.");
                completedIdentity = endpoint.BuildIdentity(
                    definition,
                    SessionActivityStage.ActivityObjectSnapshotCaptureCompleted,
                    entrySequence);
                endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureCompleted);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotCaptureCompleted,
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot capture completed capturedCount='0' targetIds='<none>' hasTransformPayload='false'.");
                DebugUtility.Log(
                    typeof(ActivityObjectSnapshotCaptureStage),
                    $"[OBS][ActivityObjectSnapshotCaptureStage] event='ActivityObjectSnapshotCaptureCompleted' owner='ActivityObjectSnapshotCaptureStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' capturedCount='0' failedCount='0' targetIds='<none>' hasTransformPayload='false' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                endpoint.EmitSnapshot(
                    snapshots,
                    "activity_object_snapshot_capture_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot capture completed capturedCount='0'.");
                return new ActivityObjectSnapshotCaptureStageResult(
                    completed: true,
                    identity: completedIdentity,
                    capturedCount: 0,
                    failedCount: 0,
                    hasTransformPayload: false,
                    reason: "no_discovery_result");
            }

            int capturedCount = 0;
            int failedCount = 0;
            bool hasTransformPayload = false;
            string captureFailureDetail = string.Empty;
            HashSet<string> capturedTargetIds = new(StringComparer.Ordinal);
            List<SessionActivitySnapshotPayloadObject> capturedObjects = new();
            ActivityCapabilityInventory snapshotInventory = runtimeState.CurrentInventoryPreview;
            ActivityCapabilityInventoryValidationResult snapshotInventoryValidation = runtimeState.CurrentInventoryPreviewValidation;
            bool hasValidSnapshotInventory =
                snapshotInventory.IsValid &&
                snapshotInventoryValidation.IsValid &&
                string.Equals(snapshotInventory.Id.PipelineId, captureIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(snapshotInventory.Id.SessionStateId, captureIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(snapshotInventory.Id.ActivityId, captureIdentity.ActivityId, StringComparison.Ordinal) &&
                snapshotInventory.Id.EntrySequence == captureIdentity.EntrySequence;

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !IsReportForCurrentEntry(report, captureIdentity, entrySequence))
                {
                    continue;
                }

                SessionActivityIdentity failedIdentity;
                if (!hasValidSnapshotInventory)
                {
                    failedIdentity = endpoint.BuildIdentity(
                        definition,
                        SessionActivityStage.ActivityObjectSnapshotCaptureFailed,
                        entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureFailed);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture failed targetId='{report.TargetId}' contentProfileId='{report.ContentProfileId}' reason='snapshot_inventory_missing_or_invalid'.");
                    failedCount += 1;
                    if (string.IsNullOrWhiteSpace(captureFailureDetail))
                    {
                        captureFailureDetail = "snapshot_inventory_missing_or_invalid";
                    }
                    continue;
                }

                IActivityObjectSnapshotProvider[] providers = ResolveObjectSnapshotProvidersFromInventory(snapshotInventory, report);
                if (providers.Length == 0)
                {
                    SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(
                        definition,
                        SessionActivityStage.ActivityObjectSnapshotCaptureSkippedNoProviders,
                        entrySequence);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureSkippedNoProviders);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureSkippedNoProviders,
                        skippedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture skipped targetId='{report.TargetId}' contentProfileId='{report.ContentProfileId}' reason='no_snapshot_providers'.");
                    continue;
                }

                ActivityObjectSnapshotCaptureCommand captureCommand = new(
                    captureIdentity,
                    report.ContentProfileId,
                    report.TargetId,
                    command.Source,
                    command.Reason);
                if (!captureCommand.IsValid)
                {
                    failedIdentity = endpoint.BuildIdentity(
                        definition,
                        SessionActivityStage.ActivityObjectSnapshotCaptureFailed,
                        entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureFailed);
                    failedCount += 1;
                    if (string.IsNullOrWhiteSpace(captureFailureDetail))
                    {
                        captureFailureDetail = "invalid_capture_command";
                    }
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture failed targetId='{report.TargetId}' contentProfileId='{report.ContentProfileId}' reason='invalid_capture_command'.");
                    continue;
                }

                ActivityObjectSnapshotCaptureResult captureResult = ExecuteObjectSnapshotCaptureCommand(captureCommand, providers);
                if (!captureResult.IsValid || !IsObjectSnapshotCaptureResultForCurrentEntry(captureResult, captureIdentity, entrySequence))
                {
                    failedIdentity = endpoint.BuildIdentity(
                        definition,
                        SessionActivityStage.ActivityObjectSnapshotCaptureFailed,
                        entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureFailed);
                    failedCount += 1;
                    if (string.IsNullOrWhiteSpace(captureFailureDetail))
                    {
                        captureFailureDetail = "invalid_or_foreign_capture_result";
                    }
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture failed targetId='{report.TargetId}' contentProfileId='{report.ContentProfileId}' reason='invalid_or_foreign_capture_result'.");
                    continue;
                }

                if (captureResult.IsCaptured)
                {
                    SessionActivityIdentity capturedIdentity = endpoint.BuildIdentity(
                        definition,
                        SessionActivityStage.ActivityObjectSnapshotCaptured,
                        entrySequence);
                    endpoint.SetCurrentIdentity(capturedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptured);
                    capturedCount += 1;
                    hasTransformPayload |= captureResult.HasTransformPayload;
                    capturedTargetIds.Add(captureResult.Command.TargetId);
                    ActivityObjectSnapshot snapshotData = captureResult.Snapshot;
                    capturedObjects.Add(new SessionActivitySnapshotPayloadObject(
                        captureResult.Command.TargetId,
                        captureResult.Command.ContentProfileId,
                        snapshotData.PositionX,
                        snapshotData.PositionY,
                        snapshotData.PositionZ,
                        snapshotData.RotationX,
                        snapshotData.RotationY,
                        snapshotData.RotationZ,
                        snapshotData.RotationW,
                        snapshotData.ScaleX,
                        snapshotData.ScaleY,
                        snapshotData.ScaleZ));
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptured,
                        capturedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot captured targetId='{captureResult.Command.TargetId}' contentProfileId='{captureResult.Command.ContentProfileId}' coordinateSpace='{ToCoordinateSpaceToken(snapshotData.CoordinateSpace)}' hasTransformPayload='{captureResult.HasTransformPayload.ToString().ToLowerInvariant()}' capturedPosition='({snapshotData.PositionX:0.###},{snapshotData.PositionY:0.###},{snapshotData.PositionZ:0.###})' position='({snapshotData.PositionX:0.###},{snapshotData.PositionY:0.###},{snapshotData.PositionZ:0.###})' rotation='({snapshotData.RotationX:0.###},{snapshotData.RotationY:0.###},{snapshotData.RotationZ:0.###},{snapshotData.RotationW:0.###})' scale='({snapshotData.ScaleX:0.###},{snapshotData.ScaleY:0.###},{snapshotData.ScaleZ:0.###})' detail='{captureResult.Detail}'.");
                    continue;
                }

                if (captureResult.IsSkippedOptional)
                {
                    SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(
                        definition,
                        SessionActivityStage.ActivityObjectSnapshotCaptureSkippedNoProviders,
                        entrySequence);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureSkippedNoProviders);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureSkippedNoProviders,
                        skippedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture skipped targetId='{captureResult.Command.TargetId}' contentProfileId='{captureResult.Command.ContentProfileId}' reason='{captureResult.Detail}'.");
                    continue;
                }

                failedCount += 1;
                if (string.IsNullOrWhiteSpace(captureFailureDetail))
                {
                    captureFailureDetail = string.IsNullOrWhiteSpace(captureResult.Detail)
                        ? "snapshot_capture_failed"
                        : Normalize(captureResult.Detail);
                }
                failedIdentity = endpoint.BuildIdentity(
                    definition,
                    SessionActivityStage.ActivityObjectSnapshotCaptureFailed,
                    entrySequence);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot capture failed targetId='{captureResult.Command.TargetId}' contentProfileId='{captureResult.Command.ContentProfileId}' reason='{captureResult.Detail}'.");
            }

            string capturedTargetIdsText = capturedTargetIds.Count > 0 ? string.Join(",", capturedTargetIds) : "<none>";
            if (capturedObjects.Count > 0)
            {
                SessionActivitySnapshotPayload payload = new(
                    command.SnapshotSchemaId,
                    captureIdentity.PipelineId,
                    captureIdentity.SessionId,
                    definition.ActivityId,
                    definition.ActivityOrdinal,
                    entrySequence,
                    capturedObjects);
                runtimeState.SetSnapshotPayloadForSaveOnExit(
                    payload,
                    captureFailed: false,
                    failureDetail: string.Empty,
                    definition.ActivityId,
                    entrySequence,
                    "ActivityObjectSnapshotCaptureStage",
                    "activity_object_snapshot_capture_completed");
            }
            else
            {
                bool captureFailed = failedCount > 0;
                string failureDetail = failedCount > 0
                    ? (string.IsNullOrWhiteSpace(captureFailureDetail) ? "snapshot_capture_failed" : Normalize(captureFailureDetail))
                    : string.Empty;
                runtimeState.SetSnapshotPayloadForSaveOnExit(
                    default,
                    captureFailed,
                    failureDetail,
                    definition.ActivityId,
                    entrySequence,
                    "ActivityObjectSnapshotCaptureStage",
                    "activity_object_snapshot_capture_completed");
            }

            completedIdentity = endpoint.BuildIdentity(
                definition,
                SessionActivityStage.ActivityObjectSnapshotCaptureCompleted,
                entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityObjectSnapshotCaptureCompleted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotCaptureCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot capture completed capturedCount='{capturedCount}' failedCount='{failedCount}' targetIds='{capturedTargetIdsText}' hasTransformPayload='{hasTransformPayload.ToString().ToLowerInvariant()}'.");
            DebugUtility.Log(
                typeof(ActivityObjectSnapshotCaptureStage),
                $"[OBS][ActivityObjectSnapshotCaptureStage] event='ActivityObjectSnapshotCaptureCompleted' owner='ActivityObjectSnapshotCaptureStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' capturedCount='{capturedCount}' failedCount='{failedCount}' targetIds='{capturedTargetIdsText}' hasTransformPayload='{hasTransformPayload.ToString().ToLowerInvariant()}' source='{command.Source}' reason='{command.Reason}'.",
                failedCount > 0 ? DebugUtility.Colors.Warning : DebugUtility.Colors.Success);
            endpoint.EmitSnapshot(
                snapshots,
                "activity_object_snapshot_capture_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot capture completed capturedCount='{capturedCount}' failedCount='{failedCount}' targetIds='{capturedTargetIdsText}' hasTransformPayload='{hasTransformPayload.ToString().ToLowerInvariant()}'.");

            return new ActivityObjectSnapshotCaptureStageResult(
                completed: true,
                identity: captureIdentity,
                capturedCount: capturedCount,
                failedCount: failedCount,
                hasTransformPayload: hasTransformPayload,
                reason: failedCount > 0 ? "completed_with_failures" : "completed");
        }

        private static IActivityObjectSnapshotProvider[] ResolveObjectSnapshotProvidersFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectSnapshotProvider>();
            }

            List<IActivityObjectSnapshotProvider> providers = new();
            HashSet<IActivityObjectSnapshotProvider> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.SnapshotProvider)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference<ActivityObjectSnapshotProviderReference>(capability.CapabilityId, out ActivityObjectSnapshotProviderReference runtimeReference) ||
                    runtimeReference.Provider == null)
                {
                    continue;
                }

                if (unique.Add(runtimeReference.Provider))
                {
                    providers.Add(runtimeReference.Provider);
                }
            }

            return providers.ToArray();
        }

        private static ActivityObjectSnapshotCaptureResult ExecuteObjectSnapshotCaptureCommand(
            ActivityObjectSnapshotCaptureCommand command,
            IActivityObjectSnapshotProvider[] providers)
        {
            if (providers == null || providers.Length == 0)
            {
                return new ActivityObjectSnapshotCaptureResult(
                    ActivityObjectSnapshotCaptureResultKind.SkippedOptional,
                    command,
                    default,
                    false,
                    command.Source,
                    command.Reason,
                    "snapshot_provider_missing");
            }

            for (int index = 0; index < providers.Length; index++)
            {
                IActivityObjectSnapshotProvider provider = providers[index];
                if (provider == null || !provider.Supports(command.TargetId))
                {
                    continue;
                }

                ActivityObjectSnapshotCaptureResult result = provider.CaptureSnapshot(command);
                if (!result.IsValid)
                {
                    return new ActivityObjectSnapshotCaptureResult(
                        ActivityObjectSnapshotCaptureResultKind.Failed,
                        command,
                        default,
                        false,
                        command.Source,
                        command.Reason,
                        "invalid_snapshot_capture_result");
                }

                return result;
            }

            return new ActivityObjectSnapshotCaptureResult(
                ActivityObjectSnapshotCaptureResultKind.SkippedOptional,
                command,
                default,
                false,
                command.Source,
                command.Reason,
                "no_matching_snapshot_provider");
        }

        private static bool IsDiscoveryResultForCurrentEntry(
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

        private static bool IsReportForCurrentEntry(
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

        private static bool IsObjectSnapshotCaptureResultForCurrentEntry(
            ActivityObjectSnapshotCaptureResult result,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            SessionActivityIdentity resultIdentity = result.Command.Identity;
            if (!result.IsValid ||
                !resultIdentity.IsValid ||
                !string.Equals(resultIdentity.PipelineId, identity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(resultIdentity.SessionId, identity.SessionId, StringComparison.Ordinal) ||
                !string.Equals(resultIdentity.ActivityId, identity.ActivityId, StringComparison.Ordinal) ||
                resultIdentity.ActivityOrdinal != identity.ActivityOrdinal ||
                resultIdentity.EntrySequence != entrySequence)
            {
                return false;
            }

            if (result.IsCaptured)
            {
                return result.Snapshot.IsValid &&
                       string.Equals(result.Command.TargetId, result.Snapshot.TargetId, StringComparison.Ordinal);
            }

            return true;
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
                if (!entry.IsValid || !string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                value = entry.Value;
                return !string.IsNullOrWhiteSpace(value);
            }

            return false;
        }

        private static string ToCoordinateSpaceToken(ActivityObjectSnapshotCoordinateSpace coordinateSpace)
        {
            return coordinateSpace == ActivityObjectSnapshotCoordinateSpace.LocalTransform
                ? "local_transform"
                : "world_transform";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
