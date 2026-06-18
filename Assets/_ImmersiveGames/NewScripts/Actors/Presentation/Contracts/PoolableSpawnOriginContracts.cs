using System;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Contracts
{
    public readonly struct PoolableSpawnOriginId : IEquatable<PoolableSpawnOriginId>
    {
        public PoolableSpawnOriginId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(PoolableSpawnOriginId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PoolableSpawnOriginId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(PoolableSpawnOriginId left, PoolableSpawnOriginId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(PoolableSpawnOriginId left, PoolableSpawnOriginId right)
        {
            return !left.Equals(right);
        }
    }

    public enum PoolableSpawnOriginKind
    {
        Unknown = 0,
        EmitterRoot = 1,
        Socket = 2,
        Bone = 3,
        Custom = 4
    }

    public enum PoolableSpawnOriginResolutionMode
    {
        RequireTypedOrigin = 0,
        PreferTypedOriginUseEmitterRoot = 1,
        UseEmitterRoot = 2
    }

    public readonly struct PoolableSpawnOriginResolved
    {
        public PoolableSpawnOriginResolved(
            PoolableSpawnOriginId originId,
            PoolableSpawnOriginKind originKind,
            Transform originTransform,
            string surfaceOwner,
            PoolableSpawnOriginResolutionMode resolutionMode,
            bool usedFallback,
            string source,
            string reason)
        {
            OriginId = originId;
            OriginKind = originKind;
            OriginTransform = originTransform;
            SurfaceOwner = surfaceOwner.TrimToEmpty();
            ResolutionMode = resolutionMode;
            UsedFallback = usedFallback;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public PoolableSpawnOriginId OriginId { get; }
        public PoolableSpawnOriginKind OriginKind { get; }
        public Transform OriginTransform { get; }
        public string SurfaceOwner { get; }
        public PoolableSpawnOriginResolutionMode ResolutionMode { get; }
        public bool UsedFallback { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            OriginId.IsValid &&
            OriginKind != PoolableSpawnOriginKind.Unknown &&
            OriginTransform != null &&
            !string.IsNullOrWhiteSpace(SurfaceOwner) &&
            !string.IsNullOrWhiteSpace(Source);

        public Vector3 Position => OriginTransform != null ? OriginTransform.position : default;
        public Vector3 Direction => OriginTransform != null ? OriginTransform.forward : Vector3.forward;
    }

    public interface IPoolableSpawnOriginSurface
    {
        bool TryResolve(PoolableSpawnOriginId originId, out PoolableSpawnOriginResolved resolved);
    }
}
