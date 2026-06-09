using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Contracts
{
    public readonly struct ActorProjectileFireEndpointId : IEquatable<ActorProjectileFireEndpointId>
    {
        public ActorProjectileFireEndpointId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorProjectileFireEndpointId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorProjectileFireEndpointId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActorProjectileFireEndpointId left, ActorProjectileFireEndpointId right) => left.Equals(right);
        public static bool operator !=(ActorProjectileFireEndpointId left, ActorProjectileFireEndpointId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public enum ActorProjectileFireEndpointReadinessKind
    {
        Unknown = 0,
        Prepared = 1,
        SkippedOptional = 2,
        MissingRequired = 3,
        MissingProfile = 4,
        MissingFireMode = 5,
        MissingMuzzle = 6,
        InvalidCommand = 7,
        NotExecutable = 8,
        Failed = 9,
    }

    public readonly struct ActorProjectileFireEndpointDescriptor
    {
        public ActorProjectileFireEndpointDescriptor(
            ActorProjectileFireEndpointId endpointId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorProjectileProfileId profileId,
            ActorProjectileFireModeId defaultFireModeId,
            ActorCommandId boundCommandId,
            bool required,
            string source,
            string reason)
        {
            EndpointId = endpointId;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ProfileId = profileId;
            DefaultFireModeId = defaultFireModeId;
            BoundCommandId = boundCommandId;
            Required = required;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorProjectileFireEndpointId EndpointId { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorProjectileProfileId ProfileId { get; }
        public ActorProjectileFireModeId DefaultFireModeId { get; }
        public ActorCommandId BoundCommandId { get; }
        public bool Required { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            EndpointId.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            ProfileId.IsValid &&
            DefaultFireModeId.IsValid &&
            BoundCommandId.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorProjectileFireEndpointReadiness
    {
        public ActorProjectileFireEndpointReadiness(
            ActorProjectileFireEndpointReadinessKind kind,
            ActorProjectileFireEndpointDescriptor descriptor,
            ActorCommandId commandId,
            ActorProjectileFireModeId fireModeId,
            ActorProjectileFireBlockedReasonKind blockedReason,
            string reason,
            string message)
        {
            Kind = kind;
            Descriptor = descriptor;
            CommandId = commandId;
            FireModeId = fireModeId;
            BlockedReason = blockedReason;
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActorProjectileFireEndpointReadinessKind Kind { get; }
        public ActorProjectileFireEndpointDescriptor Descriptor { get; }
        public ActorCommandId CommandId { get; }
        public ActorProjectileFireModeId FireModeId { get; }
        public ActorProjectileFireBlockedReasonKind BlockedReason { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid => Kind != ActorProjectileFireEndpointReadinessKind.Unknown;
        public bool IsPrepared => Kind == ActorProjectileFireEndpointReadinessKind.Prepared;
        public bool IsSkipped => Kind == ActorProjectileFireEndpointReadinessKind.SkippedOptional;
        public bool IsBlocked => IsValid && !IsPrepared && !IsSkipped;

        public static ActorProjectileFireEndpointReadiness Prepared(
            ActorProjectileFireEndpointDescriptor descriptor,
            ActorCommandId commandId,
            ActorProjectileFireModeId fireModeId,
            string reason)
        {
            return new ActorProjectileFireEndpointReadiness(
                ActorProjectileFireEndpointReadinessKind.Prepared,
                descriptor,
                commandId,
                fireModeId,
                ActorProjectileFireBlockedReasonKind.Unknown,
                reason,
                string.Empty);
        }

        public static ActorProjectileFireEndpointReadiness SkippedOptional(
            ActorProjectileFireEndpointDescriptor descriptor,
            ActorCommandId commandId,
            string reason)
        {
            return new ActorProjectileFireEndpointReadiness(
                ActorProjectileFireEndpointReadinessKind.SkippedOptional,
                descriptor,
                commandId,
                default,
                ActorProjectileFireBlockedReasonKind.Unknown,
                reason,
                string.Empty);
        }

        public static ActorProjectileFireEndpointReadiness Blocked(
            ActorProjectileFireEndpointReadinessKind kind,
            ActorProjectileFireEndpointDescriptor descriptor,
            ActorCommandId commandId,
            ActorProjectileFireModeId fireModeId,
            ActorProjectileFireBlockedReasonKind blockedReason,
            string reason,
            string message)
        {
            return new ActorProjectileFireEndpointReadiness(
                kind,
                descriptor,
                commandId,
                fireModeId,
                blockedReason,
                reason,
                message);
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IActorProjectileFireEndpoint : IActorCommandSink
    {
        ActorProjectileFireEndpointId EndpointId { get; }
        ActorId ActorId { get; }
        ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        ActorProjectileProfileId ProfileId { get; }
        ActorProjectileFireModeId DefaultFireModeId { get; }
        bool IsRequired { get; }
        bool IsProjectileFireEnabled { get; }
        bool HasSpawnAdapter { get; }
        string SpawnAdapterName { get; }

        void ConfigureSpawnAdapter(
            IActorProjectileSpawnAdapter spawnAdapter,
            string source,
            string reason);

        void SetProjectileFireEnabled(bool enabled);

        bool TryGetReadiness(
            ActorCommandId commandId,
            out ActorProjectileFireEndpointReadiness readiness);

        bool TryBuildFireCommand(
            ActorCommandEnvelope commandEnvelope,
            ActorProjectileFireModeId fireModeId,
            Vector3 origin,
            Vector3 direction,
            out ActorProjectileFireCommand command,
            out ActorProjectileFireEndpointReadiness readiness);
    }
}
