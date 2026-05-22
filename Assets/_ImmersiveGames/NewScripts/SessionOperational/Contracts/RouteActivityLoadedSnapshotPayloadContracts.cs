using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public readonly struct LoadedSessionActivitySnapshotPayloadObject
    {
        public LoadedSessionActivitySnapshotPayloadObject(
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
            TargetId = Normalize(targetId);
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct LoadedSessionActivitySnapshotPayload
    {
        public LoadedSessionActivitySnapshotPayload(
            string schemaId,
            string sessionStateId,
            string activityId,
            int sourceEntrySequence,
            IReadOnlyList<LoadedSessionActivitySnapshotPayloadObject> objects)
        {
            SchemaId = Normalize(schemaId);
            SessionStateId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            SourceEntrySequence = sourceEntrySequence < 0 ? 0 : sourceEntrySequence;
            Objects = objects ?? Array.Empty<LoadedSessionActivitySnapshotPayloadObject>();
        }

        public string SchemaId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int SourceEntrySequence { get; }
        public IReadOnlyList<LoadedSessionActivitySnapshotPayloadObject> Objects { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SchemaId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            SourceEntrySequence > 0 &&
            Objects != null &&
            Objects.Count > 0;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IRouteActivityLoadedSnapshotPayloadProvider
    {
        bool TryGetPendingLoadedSnapshotPayload(
            string activityIdentity,
            out LoadedSessionActivitySnapshotPayload payload,
            out string failureReason);
    }
}
