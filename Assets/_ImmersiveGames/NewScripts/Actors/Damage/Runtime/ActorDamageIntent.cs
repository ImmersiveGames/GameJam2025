using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    [Serializable]
    public readonly struct ActorDamageIntent
    {
        public SessionActivityIdentity ActivityIdentity { get; }
        public ActorId TargetActorId { get; }
        public ActorInstanceRuntimeId TargetActorInstanceRuntimeId { get; }
        public ActorId SourceActorId { get; }
        public float RawDamageAmount { get; }
        public string DamageKind { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActivityIdentity.IsValid &&
            TargetActorId.IsValid &&
            TargetActorInstanceRuntimeId.IsValid &&
            IsFinite(RawDamageAmount) &&
            RawDamageAmount > 0f;

        public ActorDamageIntent(
            SessionActivityIdentity activityIdentity,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            ActorId sourceActorId,
            float rawDamageAmount,
            string damageKind,
            string source,
            string reason)
        {
            ActivityIdentity = activityIdentity;
            TargetActorId = targetActorId;
            TargetActorInstanceRuntimeId = targetActorInstanceRuntimeId;
            SourceActorId = sourceActorId;
            RawDamageAmount = rawDamageAmount;
            DamageKind = Normalize(damageKind);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public static ActorDamageIntent Direct(
            SessionActivityIdentity activityIdentity,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            float rawDamageAmount,
            string source,
            string reason)
        {
            return new ActorDamageIntent(
                activityIdentity,
                targetActorId,
                targetActorInstanceRuntimeId,
                default,
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
