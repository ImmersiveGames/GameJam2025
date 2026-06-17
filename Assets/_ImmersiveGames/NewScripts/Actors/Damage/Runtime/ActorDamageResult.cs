using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    [Serializable]
    public readonly struct ActorDamageResult
    {
        public ActorDamageOutcome Outcome { get; }
        public ActorId TargetActorId { get; }
        public ActorInstanceRuntimeId TargetActorInstanceRuntimeId { get; }
        public ActorAttributeId TargetAttributeId { get; }
        public float RawDamageAmount { get; }
        public float EffectiveDamageAmount { get; }
        public bool HasMutationResult { get; }
        public ActorAttributeMutationResult MutationResult { get; }
        public string Reason { get; }

        public bool Applied => Outcome == ActorDamageOutcome.Applied;
        public bool Rejected => Outcome == ActorDamageOutcome.Rejected;
        public bool Failed => Outcome == ActorDamageOutcome.Failed;
        public bool HasChangedFact => HasMutationResult && MutationResult.HasChangedFact;
        public bool HasThresholdFacts => HasMutationResult && MutationResult.HasThresholdFacts;

        private ActorDamageResult(
            ActorDamageOutcome outcome,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            ActorAttributeId targetAttributeId,
            float rawDamageAmount,
            float effectiveDamageAmount,
            bool hasMutationResult,
            ActorAttributeMutationResult mutationResult,
            string reason)
        {
            Outcome = outcome;
            TargetActorId = targetActorId;
            TargetActorInstanceRuntimeId = targetActorInstanceRuntimeId;
            TargetAttributeId = targetAttributeId;
            RawDamageAmount = rawDamageAmount;
            EffectiveDamageAmount = effectiveDamageAmount;
            HasMutationResult = hasMutationResult;
            MutationResult = mutationResult;
            Reason = Normalize(reason);
        }

        public static ActorDamageResult AppliedResult(
            ActorDamageIntent intent,
            ActorAttributeId targetAttributeId,
            float effectiveDamageAmount,
            ActorAttributeMutationResult mutationResult)
        {
            return new ActorDamageResult(
                ActorDamageOutcome.Applied,
                intent.TargetActorId,
                intent.TargetActorInstanceRuntimeId,
                targetAttributeId,
                intent.RawDamageAmount,
                effectiveDamageAmount,
                true,
                mutationResult,
                string.Empty);
        }

        public static ActorDamageResult Reject(
            ActorDamageIntent intent,
            ActorAttributeId targetAttributeId,
            float effectiveDamageAmount,
            string reason)
        {
            return new ActorDamageResult(
                ActorDamageOutcome.Rejected,
                intent.TargetActorId,
                intent.TargetActorInstanceRuntimeId,
                targetAttributeId,
                intent.RawDamageAmount,
                effectiveDamageAmount,
                false,
                default,
                reason);
        }

        public static ActorDamageResult Fail(
            ActorDamageIntent intent,
            ActorAttributeId targetAttributeId,
            float effectiveDamageAmount,
            string reason)
        {
            return new ActorDamageResult(
                ActorDamageOutcome.Failed,
                intent.TargetActorId,
                intent.TargetActorInstanceRuntimeId,
                targetAttributeId,
                intent.RawDamageAmount,
                effectiveDamageAmount,
                false,
                default,
                reason);
        }

        public static ActorDamageResult Reject(
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            ActorAttributeId targetAttributeId,
            float rawDamageAmount,
            float effectiveDamageAmount,
            string reason)
        {
            return new ActorDamageResult(
                ActorDamageOutcome.Rejected,
                targetActorId,
                targetActorInstanceRuntimeId,
                targetAttributeId,
                rawDamageAmount,
                effectiveDamageAmount,
                false,
                default,
                reason);
        }

        public static ActorDamageResult Fail(
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            ActorAttributeId targetAttributeId,
            float rawDamageAmount,
            float effectiveDamageAmount,
            string reason)
        {
            return new ActorDamageResult(
                ActorDamageOutcome.Failed,
                targetActorId,
                targetActorInstanceRuntimeId,
                targetAttributeId,
                rawDamageAmount,
                effectiveDamageAmount,
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
