using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Contracts
{
    public readonly struct ActorProjectileFireModeId : IEquatable<ActorProjectileFireModeId>
    {
        public ActorProjectileFireModeId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorProjectileFireModeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is ActorProjectileFireModeId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ActorProjectileFireModeId left, ActorProjectileFireModeId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(ActorProjectileFireModeId left, ActorProjectileFireModeId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct ActorProjectileProfileId : IEquatable<ActorProjectileProfileId>
    {
        public ActorProjectileProfileId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorProjectileProfileId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is ActorProjectileProfileId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ActorProjectileProfileId left, ActorProjectileProfileId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(ActorProjectileProfileId left, ActorProjectileProfileId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct ActorProjectileSpawnProfileId : IEquatable<ActorProjectileSpawnProfileId>
    {
        public ActorProjectileSpawnProfileId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorProjectileSpawnProfileId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is ActorProjectileSpawnProfileId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ActorProjectileSpawnProfileId left, ActorProjectileSpawnProfileId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(ActorProjectileSpawnProfileId left, ActorProjectileSpawnProfileId right)
        {
            return !left.Equals(right);
        }
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
        RadialArc = 3
    }

    public enum ActorProjectileMuzzlePolicyKind
    {
        [InspectorName("Não definido")]
        Unknown = 0,
        [InspectorName("Forward do Actor (ativo)")]
        ActorForward = 1,
        [InspectorName("Socket nomeado (planejado)")]
        NamedMuzzleSocket = 2
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
        RandomRange = 3
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
        NotExecutable = 7
    }

    public enum ActorProjectileFireResultKind
    {
        Unknown = 0,
        Accepted = 1,
        Blocked = 2,
        Failed = 3
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
            _ => false
        };
    }

    public enum ActorProjectileMotionStrategyKind
    {
        [InspectorName("Não definido")]
        Unknown = 0,
        [InspectorName("Linear")]
        Linear = 1
    }

    public enum ActorProjectileSpawnLayerModeKind
    {
        [InspectorName("None")]
        None = 0,
        [InspectorName("Override")]
        Override = 1
    }

    public readonly struct ActorProjectileLayerBootstrap
    {
        public static ActorProjectileLayerBootstrap None => new(
            ActorProjectileSpawnLayerModeKind.None,
            -1,
            string.Empty,
            false);

        public ActorProjectileLayerBootstrap(
            ActorProjectileSpawnLayerModeKind mode,
            int layerIndex,
            string layerName,
            bool applyLayerToChildren)
        {
            Mode = mode;
            LayerIndex = mode == ActorProjectileSpawnLayerModeKind.None ? -1 : layerIndex;
            LayerName = layerName.TrimToEmpty();
            ApplyLayerToChildren = applyLayerToChildren;
        }

        public ActorProjectileSpawnLayerModeKind Mode { get; }
        public int LayerIndex { get; }
        public string LayerName { get; }
        public bool ApplyLayerToChildren { get; }
        public bool IsValid =>
            Mode == ActorProjectileSpawnLayerModeKind.None ||
            Mode == ActorProjectileSpawnLayerModeKind.Override && LayerIndex is >= 0 and <= 31 && !string.IsNullOrWhiteSpace(LayerName);
    }

    public readonly struct ActorProjectileMotionBootstrap
    {
        public ActorProjectileMotionBootstrap(
            Vector3 direction,
            float speed,
            ActorProjectileMotionStrategyKind strategy,
            string source,
            string reason)
        {
            Direction = direction;
            Speed = speed < 0f ? 0f : speed;
            Strategy = strategy;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public Vector3 Direction { get; }
        public float Speed { get; }
        public ActorProjectileMotionStrategyKind Strategy { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid =>
            Strategy == ActorProjectileMotionStrategyKind.Linear &&
            Direction.sqrMagnitude > 0f &&
            Speed > 0f &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);
    }

    public readonly struct ActorProjectileFireMode
    {
        public ActorProjectileFireMode(
            ActorProjectileFireModeId fireModeId,
            ActorProjectileSpawnProfileId spawnProfileId,
            ActorProjectileSpawnProfileAsset spawnProfileAsset,
            PoolDefinitionAsset poolDefinition,
            ActorRole spawnedActorRole,
            ActorScope spawnedActorScope,
            ActorProjectileSpawnPattern spawnPattern,
            ActorProjectileMuzzlePolicyKind muzzlePolicy,
            ActorProjectileSpreadPolicyKind spreadPolicy,
            ActorProjectileSpawnLayerModeKind spawnLayerMode,
            LayerMask spawnLayerMask,
            bool applyLayerToChildren,
            ActorProjectileMotionStrategyKind motionStrategy,
            float linearSpeed,
            PoolableSpawnOriginId spawnOriginId,
            PoolableSpawnOriginResolutionMode spawnOriginResolutionMode,
            AudioSfxCueAsset fireAudioCue,
            float fireAudioVolumeScale,
            float cooldownSeconds,
            string reason)
        {
            FireModeId = fireModeId;
            SpawnProfileId = spawnProfileId;
            SpawnProfileAsset = spawnProfileAsset;
            PoolDefinition = poolDefinition;
            SpawnedActorRole = spawnedActorRole;
            SpawnedActorScope = spawnedActorScope;
            SpawnPattern = spawnPattern;
            MuzzlePolicy = muzzlePolicy;
            SpreadPolicy = spreadPolicy;
            SpawnLayerMode = spawnLayerMode;
            SpawnLayerMask = spawnLayerMask;
            ApplyLayerToChildren = applyLayerToChildren;
            MotionStrategy = motionStrategy;
            LinearSpeed = linearSpeed < 0f ? 0f : linearSpeed;
            SpawnOriginId = spawnOriginId;
            SpawnOriginResolutionMode = spawnOriginResolutionMode;
            FireAudioCue = fireAudioCue;
            FireAudioVolumeScale = fireAudioVolumeScale < 0f ? 0f : fireAudioVolumeScale;
            CooldownSeconds = cooldownSeconds < 0f ? 0f : cooldownSeconds;
            Reason = reason.TrimToEmpty();
        }

        public ActorProjectileFireModeId FireModeId { get; }
        public ActorProjectileSpawnProfileId SpawnProfileId { get; }
        public ActorProjectileSpawnProfileAsset SpawnProfileAsset { get; }
        public PoolDefinitionAsset PoolDefinition { get; }
        public ActorRole SpawnedActorRole { get; }
        public ActorScope SpawnedActorScope { get; }
        public ActorProjectileSpawnPattern SpawnPattern { get; }
        public ActorProjectileMuzzlePolicyKind MuzzlePolicy { get; }
        public ActorProjectileSpreadPolicyKind SpreadPolicy { get; }
        public ActorProjectileSpawnLayerModeKind SpawnLayerMode { get; }
        public LayerMask SpawnLayerMask { get; }
        public bool ApplyLayerToChildren { get; }
        public ActorProjectileMotionStrategyKind MotionStrategy { get; }
        public float LinearSpeed { get; }
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
            SpawnProfileAsset != null &&
            SpawnProfileAsset.IsValid &&
            PoolDefinition != null &&
            SpawnedActorRole != ActorRole.Unknown &&
            SpawnedActorScope != ActorScope.Unknown &&
            SpawnPattern.IsValid &&
            MuzzlePolicy != ActorProjectileMuzzlePolicyKind.Unknown &&
            SpreadPolicy != ActorProjectileSpreadPolicyKind.Unknown &&
            (SpawnLayerMode == ActorProjectileSpawnLayerModeKind.None || SpawnLayerMode == ActorProjectileSpawnLayerModeKind.Override) &&
            MotionStrategy == ActorProjectileMotionStrategyKind.Linear &&
            LinearSpeed > 0f &&
            SpawnOriginId.IsValid;
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
            ActorProjectileMotionBootstrap motionBootstrap,
            ActorProjectileLayerBootstrap layerBootstrap,
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
            MotionBootstrap = motionBootstrap;
            LayerBootstrap = layerBootstrap;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
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
        public ActorProjectileMotionBootstrap MotionBootstrap { get; }
        public ActorProjectileLayerBootstrap LayerBootstrap { get; }
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
            MotionBootstrap.IsValid &&
            LayerBootstrap.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);
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
            Reason = reason.TrimToEmpty();
            Message = message.TrimToEmpty();
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
    }
}
