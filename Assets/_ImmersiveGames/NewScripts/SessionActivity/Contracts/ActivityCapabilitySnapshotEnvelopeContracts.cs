using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityCapabilitySnapshotOwnerKind
    {
        Unknown = 0,
        ActivityObject = 1,
        Actor = 2
    }

    public enum ActivityCapabilitySnapshotPayloadFormat
    {
        Unknown = 0,
        Json = 1,
        Text = 2
    }

    public readonly struct ActivityCapabilitySnapshotRecord
    {
        public ActivityCapabilitySnapshotRecord(
            SessionActivityIdentity identity,
            ActivityCapabilitySnapshotOwnerKind ownerKind,
            string ownerId,
            string contentProfileId,
            string capabilityId,
            string capabilityKind,
            string payloadSchemaId,
            int payloadSchemaVersion,
            ActivityCapabilitySnapshotPayloadFormat payloadFormat,
            string payload,
            string source,
            string reason)
        {
            Identity = identity;
            OwnerKind = ownerKind;
            OwnerId = ownerId.TrimToEmpty();
            ContentProfileId = contentProfileId.TrimToEmpty();
            CapabilityId = capabilityId.TrimToEmpty();
            CapabilityKind = capabilityKind.TrimToEmpty();
            PayloadSchemaId = payloadSchemaId.TrimToEmpty();
            PayloadSchemaVersion = payloadSchemaVersion < 0 ? 0 : payloadSchemaVersion;
            PayloadFormat = payloadFormat;
            Payload = payload.TrimToEmpty();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public ActivityCapabilitySnapshotOwnerKind OwnerKind { get; }
        public string OwnerId { get; }
        public string ContentProfileId { get; }
        public string CapabilityId { get; }
        public string CapabilityKind { get; }
        public string PayloadSchemaId { get; }
        public int PayloadSchemaVersion { get; }
        public ActivityCapabilitySnapshotPayloadFormat PayloadFormat { get; }
        public string Payload { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            OwnerKind != ActivityCapabilitySnapshotOwnerKind.Unknown &&
            !string.IsNullOrWhiteSpace(OwnerId) &&
            !string.IsNullOrWhiteSpace(CapabilityId) &&
            !string.IsNullOrWhiteSpace(CapabilityKind) &&
            !string.IsNullOrWhiteSpace(PayloadSchemaId) &&
            PayloadSchemaVersion > 0 &&
            PayloadFormat != ActivityCapabilitySnapshotPayloadFormat.Unknown &&
            !string.IsNullOrWhiteSpace(Payload) &&
            !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct ActivityCapabilitySnapshotEnvelope
    {
        public ActivityCapabilitySnapshotEnvelope(
            string schemaId,
            string pipelineId,
            string sessionStateId,
            string activityId,
            int activityOrdinal,
            int entrySequence,
            IReadOnlyList<ActivityCapabilitySnapshotRecord> records,
            string source,
            string reason)
        {
            SchemaId = schemaId.TrimToEmpty();
            PipelineId = pipelineId.TrimToEmpty();
            SessionStateId = sessionStateId.TrimToEmpty();
            ActivityId = activityId.TrimToEmpty();
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            Records = records ?? Array.Empty<ActivityCapabilitySnapshotRecord>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string SchemaId { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public IReadOnlyList<ActivityCapabilitySnapshotRecord> Records { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SchemaId) &&
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            Records is { Count: > 0 } &&
            !string.IsNullOrWhiteSpace(Source);
    }
}
