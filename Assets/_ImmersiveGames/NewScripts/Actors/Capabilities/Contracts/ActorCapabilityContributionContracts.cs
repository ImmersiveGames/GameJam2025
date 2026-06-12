using System;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts
{
    public readonly struct ActorCapabilityId : IEquatable<ActorCapabilityId>
    {
        public ActorCapabilityId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorCapabilityId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorCapabilityId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActorCapabilityId left, ActorCapabilityId right) => left.Equals(right);
        public static bool operator !=(ActorCapabilityId left, ActorCapabilityId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public enum ActorCapabilityContributionPhase
    {
        Unknown = 0,
        Setup = 1,
        Binding = 2,
        PermissionReceiver = 3,
        Reset = 4,
        Snapshot = 5,
        Restore = 6,
        Release = 7,
    }

    public enum ActorCapabilityContributionRequirement
    {
        Unknown = 0,
        Optional = 1,
        Required = 2,
    }

    public enum ActorCapabilitySnapshotPayloadFormat
    {
        Unknown = 0,
        Json = 1,
        Text = 2,
        BinaryBase64 = 3,
    }

    public enum ActorCapabilityRestoreCompatibility
    {
        Unknown = 0,
        Compatible = 1,
        IncompatibleActor = 2,
        IncompatibleCapability = 3,
        IncompatibleSchema = 4,
        IncompatibleVersion = 5,
        Unsupported = 6,
    }

    public readonly struct ActorCapabilityContributionContext
    {
        public ActorCapabilityContributionContext(
            SessionActivityIdentity identity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            ActorRole actorRole,
            ActorScope actorScope,
            string componentPath,
            string source,
            string reason)
        {
            Identity = identity;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind;
            ActorRole = actorRole;
            ActorScope = actorScope;
            ComponentPath = Normalize(componentPath);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
        public ActorRole ActorRole { get; }
        public ActorScope ActorScope { get; }
        public string ComponentPath { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            ActorKind != ActorKind.Unknown &&
            ActorRole != ActorRole.Unknown &&
            ActorScope != ActorScope.Unknown &&
            !string.IsNullOrWhiteSpace(ComponentPath) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorCapabilityContributionDescriptor
    {
        public ActorCapabilityContributionDescriptor(
            ActorCapabilityId capabilityId,
            ActorCapabilityContributionPhase phase,
            ActorCapabilityContributionRequirement requirement,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            ActorRole actorRole,
            ActorScope actorScope,
            string componentPath,
            string source,
            string reason)
        {
            CapabilityId = capabilityId;
            Phase = phase;
            Requirement = requirement;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind;
            ActorRole = actorRole;
            ActorScope = actorScope;
            ComponentPath = Normalize(componentPath);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorCapabilityId CapabilityId { get; }
        public ActorCapabilityContributionPhase Phase { get; }
        public ActorCapabilityContributionRequirement Requirement { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
        public ActorRole ActorRole { get; }
        public ActorScope ActorScope { get; }
        public string ComponentPath { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            CapabilityId.IsValid &&
            Phase != ActorCapabilityContributionPhase.Unknown &&
            Requirement != ActorCapabilityContributionRequirement.Unknown &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            ActorKind != ActorKind.Unknown &&
            ActorRole != ActorRole.Unknown &&
            ActorScope != ActorScope.Unknown &&
            !string.IsNullOrWhiteSpace(ComponentPath) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorCapabilitySnapshotPayload
    {
        public ActorCapabilitySnapshotPayload(
            SessionActivityIdentity identity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorKind actorKind,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorCapabilityId capabilityId,
            string schemaId,
            int schemaVersion,
            ActorCapabilitySnapshotPayloadFormat payloadFormat,
            string payload,
            string source,
            string reason)
        {
            Identity = identity;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorKind = actorKind;
            ActorRole = actorRole;
            ActorScope = actorScope;
            CapabilityId = capabilityId;
            SchemaId = Normalize(schemaId);
            SchemaVersion = schemaVersion;
            PayloadFormat = payloadFormat;
            Payload = payload ?? string.Empty;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorKind ActorKind { get; }
        public ActorRole ActorRole { get; }
        public ActorScope ActorScope { get; }
        public ActorCapabilityId CapabilityId { get; }
        public string SchemaId { get; }
        public int SchemaVersion { get; }
        public ActorCapabilitySnapshotPayloadFormat PayloadFormat { get; }
        public string Payload { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            ActorKind != ActorKind.Unknown &&
            ActorRole != ActorRole.Unknown &&
            ActorScope != ActorScope.Unknown &&
            CapabilityId.IsValid &&
            !string.IsNullOrWhiteSpace(SchemaId) &&
            SchemaVersion > 0 &&
            PayloadFormat != ActorCapabilitySnapshotPayloadFormat.Unknown &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorCapabilitySnapshotCaptureResult
    {
        public ActorCapabilitySnapshotCaptureResult(
            bool captured,
            ActorCapabilitySnapshotPayload payload,
            string outcomeReason,
            string source,
            string reason)
        {
            Captured = captured;
            Payload = payload;
            OutcomeReason = Normalize(outcomeReason);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public bool Captured { get; }
        public ActorCapabilitySnapshotPayload Payload { get; }
        public string OutcomeReason { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => Captured ? Payload.IsValid : !string.IsNullOrWhiteSpace(OutcomeReason);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorCapabilityRestoreResult
    {
        public ActorCapabilityRestoreResult(
            bool restored,
            ActorCapabilityRestoreCompatibility compatibility,
            string outcomeReason,
            string source,
            string reason)
        {
            Restored = restored;
            Compatibility = compatibility;
            OutcomeReason = Normalize(outcomeReason);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public bool Restored { get; }
        public ActorCapabilityRestoreCompatibility Compatibility { get; }
        public string OutcomeReason { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Compatibility != ActorCapabilityRestoreCompatibility.Unknown &&
            !string.IsNullOrWhiteSpace(OutcomeReason) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorCapabilityReleaseResult
    {
        public ActorCapabilityReleaseResult(
            bool released,
            string outcomeReason,
            string source,
            string reason)
        {
            Released = released;
            OutcomeReason = Normalize(outcomeReason);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public bool Released { get; }
        public string OutcomeReason { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(OutcomeReason) && !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IActorCapabilityContribution
    {
        ActorCapabilityContributionDescriptor Descriptor { get; }
        bool IsValid { get; }
    }

    public interface IActorSetupContribution : IActorCapabilityContribution
    {
    }

    public interface IActorBindingContribution : IActorCapabilityContribution
    {
    }

    public interface IActorPermissionReceiverContribution : IActorCapabilityContribution
    {
    }

    public interface IActorResetContribution : IActorCapabilityContribution
    {
        ActorResetGroup[] SupportedGroups { get; }
        ActivityResetBoundaryEligibility ResetBoundaryEligibility { get; }
    }

    public interface IActorSnapshotContribution : IActorCapabilityContribution
    {
        string SchemaId { get; }
        int SchemaVersion { get; }
        IActorCapabilitySnapshotEndpoint SnapshotEndpoint { get; }
    }

    public interface IActorRestoreContribution : IActorCapabilityContribution
    {
        string SchemaId { get; }
        int SchemaVersion { get; }
        IActorCapabilityRestoreEndpoint RestoreEndpoint { get; }
    }

    public interface IActorReleaseContribution : IActorCapabilityContribution
    {
        IActorCapabilityReleaseEndpoint ReleaseEndpoint { get; }
    }

    public interface IActorCapabilitySnapshotEndpoint
    {
        bool TryCaptureSnapshot(
            ActorCapabilityContributionContext context,
            out ActorCapabilitySnapshotCaptureResult result);
    }

    public interface IActorCapabilityRestoreEndpoint
    {
        bool TryEvaluateRestoreCompatibility(
            ActorCapabilityContributionContext context,
            ActorCapabilitySnapshotPayload payload,
            out ActorCapabilityRestoreCompatibility compatibility);

        bool TryRestoreSnapshot(
            ActorCapabilityContributionContext context,
            ActorCapabilitySnapshotPayload payload,
            out ActorCapabilityRestoreResult result);
    }

    public interface IActorCapabilityReleaseEndpoint
    {
        bool TryRelease(
            ActorCapabilityContributionContext context,
            out ActorCapabilityReleaseResult result);
    }

    public interface IActorSetupContributionProvider
    {
        bool TryCreateSetupContribution(
            ActorCapabilityContributionContext context,
            out IActorSetupContribution contribution);
    }

    public interface IActorBindingContributionProvider
    {
        bool TryCreateBindingContribution(
            ActorCapabilityContributionContext context,
            out IActorBindingContribution contribution);
    }

    public interface IActorPermissionReceiverContributionProvider
    {
        bool TryCreatePermissionReceiverContribution(
            ActorCapabilityContributionContext context,
            out IActorPermissionReceiverContribution contribution);
    }

    public interface IActorResetContributionProvider
    {
        bool TryCreateResetContribution(
            ActorCapabilityContributionContext context,
            out IActorResetContribution contribution);
    }

    public interface IActorSnapshotContributionProvider
    {
        bool TryCreateSnapshotContribution(
            ActorCapabilityContributionContext context,
            out IActorSnapshotContribution contribution);
    }

    public interface IActorRestoreContributionProvider
    {
        bool TryCreateRestoreContribution(
            ActorCapabilityContributionContext context,
            out IActorRestoreContribution contribution);
    }

    public interface IActorReleaseContributionProvider
    {
        bool TryCreateReleaseContribution(
            ActorCapabilityContributionContext context,
            out IActorReleaseContribution contribution);
    }
}
