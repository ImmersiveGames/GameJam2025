using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    [Serializable]
    public readonly struct ActorDamageSourceIntent
    {
        public SessionActivityIdentity ActivityIdentity { get; }
        public ActorId SourceActorId { get; }
        public ActorInstanceRuntimeId SourceActorInstanceRuntimeId { get; }
        public ActorId TargetActorId { get; }
        public ActorInstanceRuntimeId TargetActorInstanceRuntimeId { get; }
        public float RawDamageAmount { get; }
        public string DamageKind { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActivityIdentity.IsValid &&
            SourceActorId.IsValid &&
            SourceActorInstanceRuntimeId.IsValid &&
            TargetActorId.IsValid &&
            TargetActorInstanceRuntimeId.IsValid &&
            IsFinite(RawDamageAmount) &&
            RawDamageAmount > 0f;

        public ActorDamageSourceIntent(
            SessionActivityIdentity activityIdentity,
            ActorId sourceActorId,
            ActorInstanceRuntimeId sourceActorInstanceRuntimeId,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            float rawDamageAmount,
            string damageKind,
            string source,
            string reason)
        {
            ActivityIdentity = activityIdentity;
            SourceActorId = sourceActorId;
            SourceActorInstanceRuntimeId = sourceActorInstanceRuntimeId;
            TargetActorId = targetActorId;
            TargetActorInstanceRuntimeId = targetActorInstanceRuntimeId;
            RawDamageAmount = rawDamageAmount;
            DamageKind = Normalize(damageKind);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public static ActorDamageSourceIntent Direct(
            SessionActivityIdentity activityIdentity,
            ActorId sourceActorId,
            ActorInstanceRuntimeId sourceActorInstanceRuntimeId,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            float rawDamageAmount,
            string source,
            string reason)
        {
            return new ActorDamageSourceIntent(
                activityIdentity,
                sourceActorId,
                sourceActorInstanceRuntimeId,
                targetActorId,
                targetActorInstanceRuntimeId,
                rawDamageAmount,
                "direct",
                source,
                reason);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
