using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum RouteActivitySnapshotPayloadReadFailureKind
    {
        None = 0,
        PayloadMissing = 1,
        PayloadEmpty = 2,
        InvalidJson = 3,
        MissingActivityId = 4,
        MissingEntrySequence = 5,
        MissingCapabilitySnapshotEnvelope = 6,
        InvalidEntrySequence = 7,
        UnsupportedLegacyPayload = 8,
        InvalidEnvelope = 9,
        UnknownFailure = 10,
    }

    public readonly struct RouteActivitySnapshotPayloadReadResult
    {
        public RouteActivitySnapshotPayloadReadResult(
            bool succeeded,
            LoadedRouteActivitySnapshotPayload payload,
            RouteActivitySnapshotPayloadReadFailureKind failureKind,
            string failureReason,
            string detail)
        {
            Succeeded = succeeded;
            Payload = payload;
            FailureKind = failureKind;
            FailureReason = Normalize(failureReason);
            Detail = Normalize(detail);
        }

        public bool Succeeded { get; }
        public LoadedRouteActivitySnapshotPayload Payload { get; }
        public RouteActivitySnapshotPayloadReadFailureKind FailureKind { get; }
        public string FailureReason { get; }
        public string Detail { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class RouteActivitySnapshotPayloadReader
    {
        private const string CanonicalPayloadEnvelope = "CapabilitySnapshotEnvelope";

        public static RouteActivitySnapshotPayloadReadResult Read(string payload, string expectedSchemaId)
        {
            if (payload == null)
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.PayloadMissing, "payload_missing");
            }

            string normalizedPayload = payload.Trim();
            if (string.IsNullOrWhiteSpace(normalizedPayload))
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.PayloadEmpty, "payload_empty");
            }

            SnapshotPayloadDto dto;
            try
            {
                dto = JsonUtility.FromJson<SnapshotPayloadDto>(normalizedPayload);
            }
            catch (Exception ex)
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.InvalidJson, "invalid_json", ex.Message);
            }

            if (dto == null)
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.InvalidJson, "invalid_json");
            }

            string schemaId = Normalize(dto.schemaId);
            if (string.IsNullOrWhiteSpace(schemaId) || !string.Equals(schemaId, Normalize(expectedSchemaId), StringComparison.Ordinal))
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.UnknownFailure, $"schema_id_invalid:{schemaId}");
            }

            string sessionStateId = Normalize(dto.sessionStateId);
            string activityId = Normalize(dto.activityId);
            if (string.IsNullOrWhiteSpace(sessionStateId) || string.IsNullOrWhiteSpace(activityId))
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.MissingActivityId, "missing_activity_id");
            }

            if (dto.entrySequence == 0)
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.MissingEntrySequence, "missing_entry_sequence");
            }

            if (dto.entrySequence < 0)
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.InvalidEntrySequence, "invalid_entry_sequence");
            }

            if (!string.Equals(Normalize(dto.canonicalPayload), CanonicalPayloadEnvelope, StringComparison.Ordinal))
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.UnsupportedLegacyPayload, "legacy_snapshot_payload_not_supported");
            }

            CapabilitySnapshotEnvelopeDto envelopeDto = dto.capabilitySnapshotEnvelope;
            if (envelopeDto == null || envelopeDto.records == null || envelopeDto.records.Length == 0)
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.MissingCapabilitySnapshotEnvelope, "missing_capability_snapshot_envelope_records");
            }

            string envelopeSessionStateId = Normalize(envelopeDto.sessionStateId);
            string envelopeActivityId = Normalize(envelopeDto.activityId);
            int envelopeEntrySequence = envelopeDto.entrySequence;
            if (!string.Equals(envelopeSessionStateId, sessionStateId, StringComparison.Ordinal) ||
                !string.Equals(envelopeActivityId, activityId, StringComparison.Ordinal) ||
                envelopeEntrySequence != dto.entrySequence)
            {
                return Fail(
                    RouteActivitySnapshotPayloadReadFailureKind.InvalidEnvelope,
                    "envelope_identity_mismatch",
                    $"topSession='{sessionStateId}' envelopeSession='{envelopeSessionStateId}' topActivity='{activityId}' envelopeActivity='{envelopeActivityId}' topEntry='{dto.entrySequence}' envelopeEntry='{envelopeEntrySequence}'");
            }

            SessionActivityIdentity envelopeIdentity = new(
                envelopeDto.pipelineId,
                envelopeSessionStateId,
                envelopeActivityId,
                envelopeDto.activityOrdinal,
                envelopeEntrySequence,
                SessionActivityStage.ActivitySetupStarted,
                Normalize(envelopeDto.source));

            if (!envelopeIdentity.IsValid)
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.InvalidEnvelope, "envelope_identity_invalid");
            }

            List<ActivityCapabilitySnapshotRecord> records = new(envelopeDto.records.Length);
            for (int index = 0; index < envelopeDto.records.Length; index++)
            {
                CapabilitySnapshotRecordDto recordDto = envelopeDto.records[index];
                if (recordDto == null)
                {
                    continue;
                }

                records.Add(new ActivityCapabilitySnapshotRecord(
                    envelopeIdentity,
                    ParseOwnerKind(recordDto.ownerKind),
                    recordDto.ownerId,
                    recordDto.contentProfileId,
                    recordDto.capabilityId,
                    recordDto.capabilityKind,
                    recordDto.payloadSchemaId,
                    recordDto.payloadSchemaVersion,
                    ParsePayloadFormat(recordDto.payloadFormat),
                    recordDto.payload,
                    recordDto.source,
                    recordDto.reason));
            }

            ActivityCapabilitySnapshotEnvelope envelope = new(
                envelopeDto.schemaId,
                envelopeDto.pipelineId,
                envelopeSessionStateId,
                envelopeActivityId,
                envelopeDto.activityOrdinal,
                envelopeEntrySequence,
                records,
                envelopeDto.source,
                envelopeDto.reason);

            LoadedRouteActivitySnapshotPayload loadedPayload = new(schemaId, envelope);
            if (!loadedPayload.IsValid)
            {
                return Fail(RouteActivitySnapshotPayloadReadFailureKind.InvalidEnvelope, "loaded_capability_snapshot_envelope_invalid");
            }

            return new RouteActivitySnapshotPayloadReadResult(
                true,
                loadedPayload,
                RouteActivitySnapshotPayloadReadFailureKind.None,
                "read",
                string.Empty);
        }

        private static ActivityCapabilitySnapshotOwnerKind ParseOwnerKind(string value)
        {
            return Enum.TryParse(Normalize(value), false, out ActivityCapabilitySnapshotOwnerKind parsed)
                ? parsed
                : ActivityCapabilitySnapshotOwnerKind.Unknown;
        }

        private static ActivityCapabilitySnapshotPayloadFormat ParsePayloadFormat(string value)
        {
            return Enum.TryParse(Normalize(value), false, out ActivityCapabilitySnapshotPayloadFormat parsed)
                ? parsed
                : ActivityCapabilitySnapshotPayloadFormat.Unknown;
        }

        private static RouteActivitySnapshotPayloadReadResult Fail(
            RouteActivitySnapshotPayloadReadFailureKind failureKind,
            string failureReason,
            string detail = "")
        {
            return new RouteActivitySnapshotPayloadReadResult(
                false,
                default,
                failureKind,
                failureReason,
                detail);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        [Serializable]
        private sealed class SnapshotPayloadDto
        {
            public string schemaId;
            public string sessionStateId;
            public string activityId;
            public int entrySequence;
            public string canonicalPayload;
            public CapabilitySnapshotEnvelopeDto capabilitySnapshotEnvelope;
        }

        [Serializable]
        private sealed class CapabilitySnapshotEnvelopeDto
        {
            public string schemaId;
            public string pipelineId;
            public string sessionStateId;
            public string activityId;
            public int activityOrdinal;
            public int entrySequence;
            public string source;
            public string reason;
            public CapabilitySnapshotRecordDto[] records;
        }

        [Serializable]
        private sealed class CapabilitySnapshotRecordDto
        {
            public string ownerKind;
            public string ownerId;
            public string contentProfileId;
            public string capabilityId;
            public string capabilityKind;
            public string payloadSchemaId;
            public int payloadSchemaVersion;
            public string payloadFormat;
            public string payload;
            public string source;
            public string reason;
        }
    }
}
