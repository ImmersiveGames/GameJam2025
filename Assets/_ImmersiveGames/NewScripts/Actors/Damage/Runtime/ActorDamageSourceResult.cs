using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    [Serializable]
    public readonly struct ActorDamageSourceResult
    {
        public ActorDamageSourceOutcome Outcome { get; }
        public ActorId SourceActorId { get; }
        public ActorInstanceRuntimeId SourceActorInstanceRuntimeId { get; }
        public ActorId TargetActorId { get; }
        public ActorInstanceRuntimeId TargetActorInstanceRuntimeId { get; }
        public float RawDamageAmount { get; }
        public bool HasDamageResult { get; }
        public ActorDamageResult DamageResult { get; }
        public string Reason { get; }

        public bool Emitted => Outcome == ActorDamageSourceOutcome.Emitted;
        public bool Rejected => Outcome == ActorDamageSourceOutcome.Rejected;
        public bool Failed => Outcome == ActorDamageSourceOutcome.Failed;
        public bool HasChangedFact => HasDamageResult && DamageResult.HasChangedFact;
        public bool HasThresholdFacts => HasDamageResult && DamageResult.HasThresholdFacts;

        private ActorDamageSourceResult(
            ActorDamageSourceOutcome outcome,
            ActorId sourceActorId,
            ActorInstanceRuntimeId sourceActorInstanceRuntimeId,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            float rawDamageAmount,
            bool hasDamageResult,
            ActorDamageResult damageResult,
            string reason)
        {
            Outcome = outcome;
            SourceActorId = sourceActorId;
            SourceActorInstanceRuntimeId = sourceActorInstanceRuntimeId;
            TargetActorId = targetActorId;
            TargetActorInstanceRuntimeId = targetActorInstanceRuntimeId;
            RawDamageAmount = rawDamageAmount;
            HasDamageResult = hasDamageResult;
            DamageResult = damageResult;
            Reason = Normalize(reason);
        }

        public static ActorDamageSourceResult EmittedResult(
            ActorDamageSourceIntent intent,
            ActorDamageResult damageResult)
        {
            return new ActorDamageSourceResult(
                ActorDamageSourceOutcome.Emitted,
                intent.SourceActorId,
                intent.SourceActorInstanceRuntimeId,
                intent.TargetActorId,
                intent.TargetActorInstanceRuntimeId,
                intent.RawDamageAmount,
                true,
                damageResult,
                string.Empty);
        }

        public static ActorDamageSourceResult Reject(
            ActorDamageSourceIntent intent,
            string reason)
        {
            return new ActorDamageSourceResult(
                ActorDamageSourceOutcome.Rejected,
                intent.SourceActorId,
                intent.SourceActorInstanceRuntimeId,
                intent.TargetActorId,
                intent.TargetActorInstanceRuntimeId,
                intent.RawDamageAmount,
                false,
                default,
                reason);
        }

        public static ActorDamageSourceResult Fail(
            ActorDamageSourceIntent intent,
            string reason)
        {
            return new ActorDamageSourceResult(
                ActorDamageSourceOutcome.Failed,
                intent.SourceActorId,
                intent.SourceActorInstanceRuntimeId,
                intent.TargetActorId,
                intent.TargetActorInstanceRuntimeId,
                intent.RawDamageAmount,
                false,
                default,
                reason);
        }

        public static ActorDamageSourceResult Reject(
            ActorId sourceActorId,
            ActorInstanceRuntimeId sourceActorInstanceRuntimeId,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            float rawDamageAmount,
            string reason)
        {
            return new ActorDamageSourceResult(
                ActorDamageSourceOutcome.Rejected,
                sourceActorId,
                sourceActorInstanceRuntimeId,
                targetActorId,
                targetActorInstanceRuntimeId,
                rawDamageAmount,
                false,
                default,
                reason);
        }

        public static ActorDamageSourceResult Fail(
            ActorId sourceActorId,
            ActorInstanceRuntimeId sourceActorInstanceRuntimeId,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            float rawDamageAmount,
            string reason)
        {
            return new ActorDamageSourceResult(
                ActorDamageSourceOutcome.Failed,
                sourceActorId,
                sourceActorInstanceRuntimeId,
                targetActorId,
                targetActorInstanceRuntimeId,
                rawDamageAmount,
                false,
                default,
                reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
