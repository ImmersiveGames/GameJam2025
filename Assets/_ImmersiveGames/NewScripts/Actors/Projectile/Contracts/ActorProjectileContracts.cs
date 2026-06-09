using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Contracts
{
    public readonly struct ActorProjectileFireModeId : IEquatable<ActorProjectileFireModeId>
    {
        public ActorProjectileFireModeId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorProjectileFireModeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorProjectileFireModeId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActorProjectileFireModeId left, ActorProjectileFireModeId right) => left.Equals(right);
        public static bool operator !=(ActorProjectileFireModeId left, ActorProjectileFireModeId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorProjectileProfileId : IEquatable<ActorProjectileProfileId>
    {
        public ActorProjectileProfileId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorProjectileProfileId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorProjectileProfileId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActorProjectileProfileId left, ActorProjectileProfileId right) => left.Equals(right);
        public static bool operator !=(ActorProjectileProfileId left, ActorProjectileProfileId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public enum ActorProjectileSpawnPatternKind
    {
        Unknown = 0,
        Single = 1,
        LinearBurst = 2,
        RadialArc = 3,
    }

    public enum ActorProjectileMuzzlePolicyKind
    {
        Unknown = 0,
        ActorForward = 1,
        NamedMuzzleSocket = 2,
    }

    public enum ActorProjectileSpreadPolicyKind
    {
        Unknown = 0,
        None = 1,
        FixedAngle = 2,
        RandomRange = 3,
    }

    public enum ActorProjectileFireBlockedReasonKind
    {
        Unknown = 0,
        InvalidCommand = 1,
        MissingFireMode = 2,
        MissingSpawnability = 3,
        MissingPoolOrigin = 4,
        MissingMuzzle = 5,
        CooldownActive = 6,
        NotExecutable = 7,
    }

    public enum ActorProjectileFireResultKind
    {
        Unknown = 0,
        Accepted = 1,
        Blocked = 2,
        Failed = 3,
    }

    public readonly struct ActorProjectileSpawnPattern
    {
        public ActorProjectileSpawnPattern(
            ActorProjectileSpawnPatternKind kind,
            int projectileCount,
            float radialArcDegrees)
        {
            Kind = kind;
            ProjectileCount = projectileCount < 0 ? 0 : projectileCount;
            RadialArcDegrees = radialArcDegrees < 0f ? 0f : radialArcDegrees;
        }

        public ActorProjectileSpawnPatternKind Kind { get; }
        public int ProjectileCount { get; }
        public float RadialArcDegrees { get; }
        public bool IsValid => Kind switch
        {
            ActorProjectileSpawnPatternKind.Single => ProjectileCount == 1,
            ActorProjectileSpawnPatternKind.LinearBurst => ProjectileCount > 1,
            ActorProjectileSpawnPatternKind.RadialArc => ProjectileCount > 1 && RadialArcDegrees > 0f,
            _ => false,
        };
    }

    public readonly struct ActorProjectileFireMode
    {
        public ActorProjectileFireMode(
            ActorProjectileFireModeId fireModeId,
            ActorCommandId acceptedCommandId,
            ActorSpawnability spawnability,
            ActorProjectileSpawnPattern spawnPattern,
            ActorProjectileMuzzlePolicyKind muzzlePolicy,
            ActorProjectileSpreadPolicyKind spreadPolicy,
            float cooldownSeconds,
            string reason)
        {
            FireModeId = fireModeId;
            AcceptedCommandId = acceptedCommandId;
            Spawnability = spawnability;
            SpawnPattern = spawnPattern;
            MuzzlePolicy = muzzlePolicy;
            SpreadPolicy = spreadPolicy;
            CooldownSeconds = cooldownSeconds < 0f ? 0f : cooldownSeconds;
            Reason = Normalize(reason);
        }

        public ActorProjectileFireModeId FireModeId { get; }
        public ActorCommandId AcceptedCommandId { get; }
        public ActorSpawnability Spawnability { get; }
        public ActorProjectileSpawnPattern SpawnPattern { get; }
        public ActorProjectileMuzzlePolicyKind MuzzlePolicy { get; }
        public ActorProjectileSpreadPolicyKind SpreadPolicy { get; }
        public float CooldownSeconds { get; }
        public string Reason { get; }
        public bool HasCooldown => CooldownSeconds > 0f;
        public bool IsValid =>
            FireModeId.IsValid &&
            AcceptedCommandId.IsValid &&
            AcceptedCommandId == ActorCommandId.FirePrimary &&
            Spawnability.IsValid &&
            Spawnability.IsSpawnable &&
            SpawnPattern.IsValid &&
            MuzzlePolicy != ActorProjectileMuzzlePolicyKind.Unknown &&
            SpreadPolicy != ActorProjectileSpreadPolicyKind.Unknown;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorProjectileFireCommand
    {
        public ActorProjectileFireCommand(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorCommandEnvelope commandEnvelope,
            ActorProjectileFireModeId fireModeId,
            Vector3 origin,
            Vector3 direction,
            string source,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            CommandEnvelope = commandEnvelope;
            FireModeId = fireModeId;
            Origin = origin;
            Direction = direction;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorCommandEnvelope CommandEnvelope { get; }
        public ActorProjectileFireModeId FireModeId { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool HasDirection => Direction.sqrMagnitude > 0f;
        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            CommandEnvelope.IsValid &&
            CommandEnvelope.CommandId == ActorCommandId.FirePrimary &&
            FireModeId.IsValid &&
            HasDirection &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorProjectileFireResult
    {
        public ActorProjectileFireResult(
            ActorProjectileFireResultKind kind,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorProjectileFireModeId fireModeId,
            ActorProjectileFireBlockedReasonKind blockedReason,
            string reason,
            string message)
        {
            Kind = kind;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            FireModeId = fireModeId;
            BlockedReason = blockedReason;
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActorProjectileFireResultKind Kind { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorProjectileFireModeId FireModeId { get; }
        public ActorProjectileFireBlockedReasonKind BlockedReason { get; }
        public string Reason { get; }
        public string Message { get; }
        public bool IsValid => Kind != ActorProjectileFireResultKind.Unknown;
        public bool IsAccepted => Kind == ActorProjectileFireResultKind.Accepted;
        public bool IsBlocked => Kind == ActorProjectileFireResultKind.Blocked;
        public bool IsFailed => Kind == ActorProjectileFireResultKind.Failed;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
