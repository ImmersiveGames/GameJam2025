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
    }

    public interface IActorSnapshotContribution : IActorCapabilityContribution
    {
        string SchemaId { get; }
        int SchemaVersion { get; }
    }

    public interface IActorRestoreContribution : IActorCapabilityContribution
    {
        string SchemaId { get; }
        int SchemaVersion { get; }
    }

    public interface IActorReleaseContribution : IActorCapabilityContribution
    {
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
