using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct SessionActivitySnapshotPayloadObject
    {
        public SessionActivitySnapshotPayloadObject(
            string targetId,
            string contentProfileId,
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
            TargetId = Normalize(targetId);
            ContentProfileId = Normalize(contentProfileId);
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
        public string ContentProfileId { get; }
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivitySnapshotPayload
    {
        public SessionActivitySnapshotPayload(
            string schemaId,
            string pipelineId,
            string sessionStateId,
            string activityId,
            int activityOrdinal,
            int entrySequence,
            IReadOnlyList<SessionActivitySnapshotPayloadObject> objects)
        {
            SchemaId = Normalize(schemaId);
            PipelineId = Normalize(pipelineId);
            SessionStateId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            Objects = objects ?? Array.Empty<SessionActivitySnapshotPayloadObject>();
        }

        public string SchemaId { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public IReadOnlyList<SessionActivitySnapshotPayloadObject> Objects { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SchemaId) &&
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            Objects is { Count: > 0 };

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionActivitySnapshotPayloadProvider
    {
        bool TryGetSnapshotPayloadForSaveOnExit(
            string sessionStateId,
            out SessionActivitySnapshotPayload payload,
            out string failureReason);
    }
}
