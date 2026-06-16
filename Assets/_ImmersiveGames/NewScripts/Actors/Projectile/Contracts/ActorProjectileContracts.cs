using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
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

    public readonly struct ActorProjectileSpawnProfileId : IEquatable<ActorProjectileSpawnProfileId>
    {
        public ActorProjectileSpawnProfileId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorProjectileSpawnProfileId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorProjectileSpawnProfileId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActorProjectileSpawnProfileId left, ActorProjectileSpawnProfileId right) => left.Equals(right);
        public static bool operator !=(ActorProjectileSpawnProfileId left, ActorProjectileSpawnProfileId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public enum ActorProjectileSpawnPatternKind
    {
        [InspectorName("Não definido")]
        Unknown = 0,
        [InspectorName("Único (ativo)")]
        Single = 1,
        [InspectorName("Sequência linear (planejado)")]
        LinearBurst = 2,
        [InspectorName("Arco radial (planejado)")]
        RadialArc = 3,
    }

    public enum ActorProjectileMuzzlePolicyKind
    {
        [InspectorName("Não definido")]
        Unknown = 0,
        [InspectorName("Forward do Actor (ativo)")]
        ActorForward = 1,
        [InspectorName("Socket nomeado (planejado)")]
        NamedMuzzleSocket = 2,
    }

    public enum ActorProjectileSpreadPolicyKind
    {
        [InspectorName("Não definido")]
        Unknown = 0,
        [InspectorName("Sem variação (ativo)")]
        None = 1,
        [InspectorName("Ângulo fixo (planejado)")]
        FixedAngle = 2,
        [InspectorName("Faixa aleatória (planejado)")]
        RandomRange = 3,
    }

    public enum ActorProjectileFireBlockedReasonKind
    {
        Unknown = 0,
        InvalidCommand = 1,
        MissingFireMode = 2,
        MissingSpawnProfile = 3,
        MissingPoolDefinition = 4,
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
            ActorProjectileSpawnProfileId spawnProfileId,
            PoolDefinitionAsset poolDefinition,
            ActorRole spawnedActorRole,
            ActorScope spawnedActorScope,
            ActorProjectileSpawnPattern spawnPattern,
            ActorProjectileMuzzlePolicyKind muzzlePolicy,
            ActorProjectileSpreadPolicyKind spreadPolicy,
            PoolableSpawnOriginId spawnOriginId,
            PoolableSpawnOriginResolutionMode spawnOriginResolutionMode,
            AudioSfxCueAsset fireAudioCue,
            float fireAudioVolumeScale,
            float cooldownSeconds,
            string reason)
        {
            FireModeId = fireModeId;
            SpawnProfileId = spawnProfileId;
            PoolDefinition = poolDefinition;
            SpawnedActorRole = spawnedActorRole;
            SpawnedActorScope = spawnedActorScope;
            SpawnPattern = spawnPattern;
            MuzzlePolicy = muzzlePolicy;
            SpreadPolicy = spreadPolicy;
            SpawnOriginId = spawnOriginId;
            SpawnOriginResolutionMode = spawnOriginResolutionMode;
            FireAudioCue = fireAudioCue;
            FireAudioVolumeScale = fireAudioVolumeScale < 0f ? 0f : fireAudioVolumeScale;
            CooldownSeconds = cooldownSeconds < 0f ? 0f : cooldownSeconds;
            Reason = Normalize(reason);
        }

        public ActorProjectileFireModeId FireModeId { get; }
        public ActorProjectileSpawnProfileId SpawnProfileId { get; }
        public PoolDefinitionAsset PoolDefinition { get; }
        public ActorRole SpawnedActorRole { get; }
        public ActorScope SpawnedActorScope { get; }
        public ActorProjectileSpawnPattern SpawnPattern { get; }
        public ActorProjectileMuzzlePolicyKind MuzzlePolicy { get; }
        public ActorProjectileSpreadPolicyKind SpreadPolicy { get; }
        public PoolableSpawnOriginId SpawnOriginId { get; }
        public PoolableSpawnOriginResolutionMode SpawnOriginResolutionMode { get; }
        public AudioSfxCueAsset FireAudioCue { get; }
        public float FireAudioVolumeScale { get; }
        public float CooldownSeconds { get; }
        public string Reason { get; }
        public bool HasCooldown => CooldownSeconds > 0f;
        public bool IsValid =>
            FireModeId.IsValid &&
            SpawnProfileId.IsValid &&
            PoolDefinition != null &&
            SpawnedActorRole != ActorRole.Unknown &&
            SpawnedActorScope != ActorScope.Unknown &&
            SpawnPattern.IsValid &&
            MuzzlePolicy != ActorProjectileMuzzlePolicyKind.Unknown &&
            SpreadPolicy != ActorProjectileSpreadPolicyKind.Unknown &&
            SpawnOriginId.IsValid;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorProjectileFireCommand
    {
        public ActorProjectileFireCommand(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorCommandEnvelope commandEnvelope,
            ActorProjectileFireModeId fireModeId,
            ActorProjectileSpawnProfileId spawnProfileId,
            PoolDefinitionAsset poolDefinition,
            ActorRole spawnedActorRole,
            ActorScope spawnedActorScope,
            Vector3 origin,
            Vector3 direction,
            string source,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            CommandEnvelope = commandEnvelope;
            FireModeId = fireModeId;
            SpawnProfileId = spawnProfileId;
            PoolDefinition = poolDefinition;
            SpawnedActorRole = spawnedActorRole;
            SpawnedActorScope = spawnedActorScope;
            Origin = origin;
            Direction = direction;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorCommandEnvelope CommandEnvelope { get; }
        public ActorProjectileFireModeId FireModeId { get; }
        public ActorProjectileSpawnProfileId SpawnProfileId { get; }
        public PoolDefinitionAsset PoolDefinition { get; }
        public ActorRole SpawnedActorRole { get; }
        public ActorScope SpawnedActorScope { get; }
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
            SpawnProfileId.IsValid &&
            PoolDefinition != null &&
            SpawnedActorRole != ActorRole.Unknown &&
            SpawnedActorScope != ActorScope.Unknown &&
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
