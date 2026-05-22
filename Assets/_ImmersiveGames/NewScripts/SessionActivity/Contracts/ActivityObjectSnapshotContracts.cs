using System;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct ActivityObjectSnapshot
    {
        public ActivityObjectSnapshot(
            SessionActivityIdentity identity,
            string contentProfileId,
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
            float scaleZ,
            string source,
            string reason)
        {
            Identity = identity;
            ContentProfileId = Normalize(contentProfileId);
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
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
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
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(TargetId) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityObjectSnapshotCaptureCommand
    {
        public ActivityObjectSnapshotCaptureCommand(
            SessionActivityIdentity identity,
            string contentProfileId,
            string targetId,
            string source,
            string reason)
        {
            Identity = identity;
            ContentProfileId = Normalize(contentProfileId);
            TargetId = Normalize(targetId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public string ContentProfileId { get; }
        public string TargetId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(TargetId) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum ActivityObjectSnapshotCaptureResultKind
    {
        Unknown = 0,
        Captured = 1,
        SkippedOptional = 2,
        Failed = 3,
    }

    public readonly struct ActivityObjectSnapshotCaptureResult
    {
        public ActivityObjectSnapshotCaptureResult(
            ActivityObjectSnapshotCaptureResultKind kind,
            ActivityObjectSnapshotCaptureCommand command,
            ActivityObjectSnapshot snapshot,
            bool hasTransformPayload,
            string source,
            string reason,
            string detail)
        {
            Kind = kind;
            Command = command;
            Snapshot = snapshot;
            HasTransformPayload = hasTransformPayload;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public ActivityObjectSnapshotCaptureResultKind Kind { get; }
        public ActivityObjectSnapshotCaptureCommand Command { get; }
        public ActivityObjectSnapshot Snapshot { get; }
        public bool HasTransformPayload { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsCaptured => Kind == ActivityObjectSnapshotCaptureResultKind.Captured;
        public bool IsSkippedOptional => Kind == ActivityObjectSnapshotCaptureResultKind.SkippedOptional;
        public bool IsFailed => Kind == ActivityObjectSnapshotCaptureResultKind.Failed;
        public bool IsValid =>
            Kind != ActivityObjectSnapshotCaptureResultKind.Unknown &&
            Command.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActivityObjectSnapshotProvider
    {
        bool Supports(string targetId);
        ActivityObjectSnapshotCaptureResult CaptureSnapshot(ActivityObjectSnapshotCaptureCommand command);
    }
}
