using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
using static _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages.ActivityEntryObjectSetupStageUtility;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryObjectSnapshotRestoreStage
    {
        private const string RouteActivitySnapshotSchemaId = "progression.route_activity.object_snapshot.v1";
        private const string TransformSnapshotSchemaId = "activity_object.transform_snapshot.v1";
        private const string WorldTransformCoordinateSpace = "world_transform";

        public static void Execute(
            ActivityEntryObjectSetupCommand command,
            ActivityObjectContributorDiscoveryResult discoveryResult,
            ActivityCapabilityInventory inventory,
            IActivityEntryRuntimeBridge endpoint,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            List<SessionActivityFact> facts)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity restoreIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupStarted);
            endpoint.SetCurrentIdentity(restoreIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotRestoreStarted,
                restoreIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity object snapshot restore started.");

            if (!loadedSnapshotPayloadContext.HasPayload)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoPayload,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore skipped reason='no_loaded_payload'.");
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore completed payloadAvailable='false' payloadKind='<none>' canonicalPayload='<none>' recordCount='0' matchedRecordCount='0' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            LoadedRouteActivitySnapshotPayload loadedPayload = loadedSnapshotPayloadContext.Payload;

            if (!IsLoadedSnapshotPayloadForCurrentActivity(loadedPayload, command.Identity.SessionId, command.ActivityId))
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore failed reason='payload_foreign_or_stale' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' payloadSessionStateId='{loadedPayload.SessionStateId}' payloadActivityId='{loadedPayload.ActivityId}' payloadSourceEntrySequence='{loadedPayload.SourceEntrySequence}' recordCount='{loadedPayload.RecordCount}'.");
                throw new InvalidOperationException(
                    $"payload_foreign_or_stale: activityId='{command.ActivityId}' entrySequence='{entrySequence}' payloadSessionStateId='{loadedPayload.SessionStateId}' payloadActivityId='{loadedPayload.ActivityId}' payloadSourceEntrySequence='{loadedPayload.SourceEntrySequence}'.");
            }

            if (!TryBuildActivityObjectTransformPayloadByTargetId(
                    loadedPayload.CapabilitySnapshotEnvelope,
                    out Dictionary<string, ActivityObjectTransformSnapshotPayload> payloadByTargetId,
                    out int matchedRecordCount,
                    out string failureReason,
                    out string failureDetail))
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore failed reason='{failureReason}' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' detail='{failureDetail}'.");
                throw new InvalidOperationException(
                    $"{failureReason}: activityId='{command.ActivityId}' entrySequence='{entrySequence}' detail='{failureDetail}'.");
            }

            if (matchedRecordCount == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_activity_object_transform_records' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='0'.");
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='0' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            if (!IsDiscoveryResultForCurrentEntryForIdentity(discoveryResult, command.Identity, entrySequence, restoreIdentity) ||
                discoveryResult.Reports.Count == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_matching_target_for_entry' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='{matchedRecordCount}'.");
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='{matchedRecordCount}' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            int matchedTargetCount = 0;
            int restoredCount = 0;
            bool restoreFailed = false;
            HashSet<string> matchedTargetIds = new(StringComparer.Ordinal);
            bool hasValidRestoreInventory =
                inventory.IsValid &&
                string.Equals(inventory.Id.PipelineId, restoreIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.SessionStateId, restoreIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.ActivityId, restoreIdentity.ActivityId, StringComparison.Ordinal) &&
                inventory.Id.EntrySequence == restoreIdentity.EntrySequence;

            if (!hasValidRestoreInventory)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore failed reason='restore_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{inventory.IsValid.ToString().ToLowerInvariant()}'.");
                throw new InvalidOperationException(
                    $"restore_inventory_missing_or_invalid: activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!IsReportForCurrentEntryForIdentity(report, restoreIdentity, entrySequence, restoreIdentity))
                {
                    continue;
                }

                if (!payloadByTargetId.TryGetValue(report.TargetId, out ActivityObjectTransformSnapshotPayload payloadObject) || !payloadObject.IsValid)
                {
                    continue;
                }

                matchedTargetCount += 1;
                matchedTargetIds.Add(report.TargetId);
                IActivityObjectSnapshotRestoreEndpoint[] endpoints = ResolveObjectSnapshotRestoreEndpointsFromInventory(inventory, report);
                ActivityObjectSnapshotRestoreCommand restoreCommand = new(
                    restoreIdentity,
                    report.TargetId,
                    ActivityObjectSnapshotCoordinateSpace.WorldTransform,
                    payloadObject.PositionX,
                    payloadObject.PositionY,
                    payloadObject.PositionZ,
                    payloadObject.RotationX,
                    payloadObject.RotationY,
                    payloadObject.RotationZ,
                    payloadObject.RotationW,
                    payloadObject.ScaleX,
                    payloadObject.ScaleY,
                    payloadObject.ScaleZ,
                    command.Source,
                    command.Reason);

                ActivityObjectSnapshotRestoreResult result = ExecuteObjectSnapshotRestoreCommand(restoreCommand, endpoints, report);
                if (!IsObjectSnapshotRestoreResultForCurrentEntry(result, restoreIdentity, entrySequence, restoreIdentity))
                {
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' activity object snapshot restore failed reason='restore_result_invalid_or_failed_required' targetId='{report.TargetId}' detail='{result.Detail}'.");
                    throw new InvalidOperationException(
                        $"restore_result_invalid_or_failed_required: activityId='{command.ActivityId}' targetId='{report.TargetId}' detail='{result.Detail}'.");
                }

                if (result.IsRestored)
                {
                    restoredCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreApplied,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' activity object snapshot restore applied targetId='{report.TargetId}' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' payloadSchemaId='{TransformSnapshotSchemaId}' coordinateSpace='{ToCoordinateSpaceToken(restoreCommand.CoordinateSpace)}' payloadPosition='({payloadObject.PositionX:0.###},{payloadObject.PositionY:0.###},{payloadObject.PositionZ:0.###})' beforePosition='({result.BeforePositionX:0.###},{result.BeforePositionY:0.###},{result.BeforePositionZ:0.###})' afterPosition='({result.AfterPositionX:0.###},{result.AfterPositionY:0.###},{result.AfterPositionZ:0.###})' restoreVerified='{result.RestoreVerified.ToString().ToLowerInvariant()}' hasTransformPayload='true' detail='{result.Detail}'.");
                    continue;
                }

                if (result.IsSkippedOptional)
                {
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoEndpointOptional,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' activity object snapshot restore skipped optional targetId='{report.TargetId}' reason='{result.Detail}'.");
                    continue;
                }

                restoreFailed = true;
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore failed reason='restore_endpoint_missing_required' targetId='{report.TargetId}' detail='{result.Detail}'.");
                throw new InvalidOperationException(
                    $"restore_endpoint_missing_required: activityId='{command.ActivityId}' targetId='{report.TargetId}' detail='{result.Detail}'.");
            }

            if (matchedTargetCount == 0)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_matching_target_for_entry' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='{matchedRecordCount}'.");
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                restoreIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' matchedRecordCount='{matchedRecordCount}' matchedTargetCount='{matchedTargetCount}' restoredCount='{restoredCount}' targetIds='{JoinValues(matchedTargetIds)}' appliedTargetIds='{JoinValues(matchedTargetIds)}' failedTargetIds='<none>' coordinateSpace='world_transform' restoreVerified='{(!restoreFailed && restoredCount == matchedTargetCount).ToString().ToLowerInvariant()}' restoreFailed='{restoreFailed.ToString().ToLowerInvariant()}'.");
        }

        private static bool IsLoadedSnapshotPayloadForCurrentActivity(
            LoadedRouteActivitySnapshotPayload loadedPayload,
            string sessionId,
            string activityId)
        {
            return loadedPayload.IsValid &&
                   string.Equals(loadedPayload.SchemaId, RouteActivitySnapshotSchemaId, StringComparison.Ordinal) &&
                   string.Equals(loadedPayload.SessionStateId, sessionId, StringComparison.Ordinal) &&
                   string.Equals(loadedPayload.ActivityId, activityId, StringComparison.Ordinal) &&
                   loadedPayload.SourceEntrySequence > 0;
        }

        private static bool TryBuildActivityObjectTransformPayloadByTargetId(
            ActivityCapabilitySnapshotEnvelope envelope,
            out Dictionary<string, ActivityObjectTransformSnapshotPayload> payloadByTargetId,
            out int matchedRecordCount,
            out string failureReason,
            out string failureDetail)
        {
            payloadByTargetId = new Dictionary<string, ActivityObjectTransformSnapshotPayload>(StringComparer.Ordinal);
            matchedRecordCount = 0;
            failureReason = string.Empty;
            failureDetail = string.Empty;

            if (!envelope.IsValid || envelope.Records == null)
            {
                failureReason = "capability_snapshot_envelope_invalid";
                failureDetail = "Envelope missing or invalid.";
                return false;
            }

            for (int index = 0; index < envelope.Records.Count; index++)
            {
                ActivityCapabilitySnapshotRecord record = envelope.Records[index];
                if (!IsActivityObjectTransformRecord(record))
                {
                    continue;
                }

                matchedRecordCount += 1;
                string targetId = record.OwnerId.TrimToEmpty();
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    failureReason = "activity_object_snapshot_record_owner_missing";
                    failureDetail = $"recordIndex='{index}'";
                    return false;
                }

                ActivityObjectTransformSnapshotPayloadDto dto;
                try
                {
                    dto = JsonUtility.FromJson<ActivityObjectTransformSnapshotPayloadDto>(record.Payload);
                }
                catch (Exception ex)
                {
                    failureReason = "activity_object_snapshot_record_payload_invalid_json";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}' exception='{ex.GetType().Name}:{ex.Message.TrimToEmpty()}'";
                    return false;
                }

                if (dto == null)
                {
                    failureReason = "activity_object_snapshot_record_payload_invalid_json";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}'";
                    return false;
                }

                string payloadTargetId = dto.targetId.TrimToEmpty();
                if (!string.IsNullOrWhiteSpace(payloadTargetId) && !string.Equals(payloadTargetId, targetId, StringComparison.Ordinal))
                {
                    failureReason = "activity_object_snapshot_record_target_mismatch";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}' payloadTargetId='{payloadTargetId}'";
                    return false;
                }

                string coordinateSpace = dto.coordinateSpace.TrimToEmpty();
                if (!string.Equals(coordinateSpace, WorldTransformCoordinateSpace, StringComparison.Ordinal))
                {
                    failureReason = "activity_object_snapshot_record_coordinate_space_unsupported";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}' coordinateSpace='{coordinateSpace}'";
                    return false;
                }

                if (payloadByTargetId.ContainsKey(targetId))
                {
                    failureReason = "activity_object_snapshot_record_duplicate_target";
                    failureDetail = $"recordIndex='{index}' ownerId='{targetId}'";
                    return false;
                }

                payloadByTargetId.Add(
                    targetId,
                    new ActivityObjectTransformSnapshotPayload(
                        targetId,
                        dto.position.x,
                        dto.position.y,
                        dto.position.z,
                        dto.rotation.x,
                        dto.rotation.y,
                        dto.rotation.z,
                        dto.rotation.w,
                        dto.scale.x,
                        dto.scale.y,
                        dto.scale.z));
            }

            return true;
        }

        private static bool IsActivityObjectTransformRecord(ActivityCapabilitySnapshotRecord record)
        {
            return record.OwnerKind == ActivityCapabilitySnapshotOwnerKind.ActivityObject &&
                string.Equals(record.PayloadSchemaId, TransformSnapshotSchemaId, StringComparison.Ordinal) &&
                record.PayloadSchemaVersion > 0 &&
                record.PayloadFormat == ActivityCapabilitySnapshotPayloadFormat.Json &&
                !string.IsNullOrWhiteSpace(record.Payload);
        }
private readonly struct ActivityObjectTransformSnapshotPayload
        {
            public ActivityObjectTransformSnapshotPayload(
                string targetId,
                float positionX,
                float positionY,
                float positionZ,
                float rotationX,
                float rotationY,
                float rotationZ,
                float rotationW,
                float scaleX,
                float scaleY,
                float scaleZ)
            {
                TargetId = targetId.TrimToEmpty();
                PositionX = positionX;
                PositionY = positionY;
                PositionZ = positionZ;
                RotationX = rotationX;
                RotationY = rotationY;
                RotationZ = rotationZ;
                RotationW = rotationW;
                ScaleX = scaleX;
                ScaleY = scaleY;
                ScaleZ = scaleZ;
            }

            public string TargetId { get; }
            public float PositionX { get; }
            public float PositionY { get; }
            public float PositionZ { get; }
            public float RotationX { get; }
            public float RotationY { get; }
            public float RotationZ { get; }
            public float RotationW { get; }
            public float ScaleX { get; }
            public float ScaleY { get; }
            public float ScaleZ { get; }
            public bool IsValid => !string.IsNullOrWhiteSpace(TargetId);
        }

        [Serializable]
        private sealed class ActivityObjectTransformSnapshotPayloadDto
        {
            public string targetId;
            public string contentProfileId;
            public string coordinateSpace;
            public Vector3Dto position;
            public QuaternionDto rotation;
            public Vector3Dto scale;
        }

        [Serializable]
        private struct Vector3Dto
        {
            public float x;
            public float y;
            public float z;
        }

        [Serializable]
        private struct QuaternionDto
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }
    }
}
