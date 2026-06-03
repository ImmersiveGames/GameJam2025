namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityObjectSnapshotCoordinateSpace
    {
        Unknown = 0,
        WorldTransform = 1,
        LocalTransform = 2,
    }

    public readonly struct ActivityObjectSnapshot
    {
        public ActivityObjectSnapshot(
            SessionActivityIdentity identity,
            string contentProfileId,
            string targetId,
            ActivityObjectSnapshotCoordinateSpace coordinateSpace,
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
            CoordinateSpace = coordinateSpace;
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
        public ActivityObjectSnapshotCoordinateSpace CoordinateSpace { get; }
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
            CoordinateSpace != ActivityObjectSnapshotCoordinateSpace.Unknown &&
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

    public interface IActivityObjectSnapshotProviderContractView
    {
        bool TryDescribeContract(
            string targetId,
            out string providerPath,
            out string targetTransformPath,
            out string failureReason);
    }

    public readonly struct ActivityObjectSnapshotRestoreCommand
    {
        public ActivityObjectSnapshotRestoreCommand(
            SessionActivityIdentity identity,
            string targetId,
            ActivityObjectSnapshotCoordinateSpace coordinateSpace,
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
            TargetId = Normalize(targetId);
            CoordinateSpace = coordinateSpace;
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
        public string TargetId { get; }
        public ActivityObjectSnapshotCoordinateSpace CoordinateSpace { get; }
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
            CoordinateSpace != ActivityObjectSnapshotCoordinateSpace.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum ActivityObjectSnapshotRestoreResultKind
    {
        Unknown = 0,
        Restored = 1,
        SkippedOptional = 2,
        Failed = 3,
    }

    public readonly struct ActivityObjectSnapshotRestoreResult
    {
        public ActivityObjectSnapshotRestoreResult(
            ActivityObjectSnapshotRestoreResultKind kind,
            ActivityObjectSnapshotRestoreCommand command,
            bool restoreVerified,
            float beforePositionX,
            float beforePositionY,
            float beforePositionZ,
            float afterPositionX,
            float afterPositionY,
            float afterPositionZ,
            string source,
            string reason,
            string detail)
        {
            Kind = kind;
            Command = command;
            RestoreVerified = restoreVerified;
            BeforePositionX = beforePositionX;
            BeforePositionY = beforePositionY;
            BeforePositionZ = beforePositionZ;
            AfterPositionX = afterPositionX;
            AfterPositionY = afterPositionY;
            AfterPositionZ = afterPositionZ;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public ActivityObjectSnapshotRestoreResultKind Kind { get; }
        public ActivityObjectSnapshotRestoreCommand Command { get; }
        public bool RestoreVerified { get; }
        public float BeforePositionX { get; }
        public float BeforePositionY { get; }
        public float BeforePositionZ { get; }
        public float AfterPositionX { get; }
        public float AfterPositionY { get; }
        public float AfterPositionZ { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsRestored => Kind == ActivityObjectSnapshotRestoreResultKind.Restored;
        public bool IsSkippedOptional => Kind == ActivityObjectSnapshotRestoreResultKind.SkippedOptional;
        public bool IsFailed => Kind == ActivityObjectSnapshotRestoreResultKind.Failed;
        public bool IsValid =>
            Kind != ActivityObjectSnapshotRestoreResultKind.Unknown &&
            Command.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IActivityObjectSnapshotRestoreEndpoint
    {
        bool Supports(string targetId);
        ActivityObjectSnapshotRestoreResult ApplyRestore(ActivityObjectSnapshotRestoreCommand command);
    }

    public interface IActivityObjectSnapshotRestoreEndpointContractView
    {
        bool TryDescribeContract(
            string targetId,
            out string endpointPath,
            out string targetTransformPath,
            out string failureReason);
    }
}
