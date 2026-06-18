using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeApplyResult
    {
        private static readonly ActorAttributeThresholdCrossedFact[] EmptyThresholdFacts =
            Array.Empty<ActorAttributeThresholdCrossedFact>();

        public ActorAttributeApplyOutcome Outcome { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorAttributeId AttributeId { get; }
        public bool HasFact { get; }
        public ActorAttributeChangedFact Fact { get; }
        public IReadOnlyList<ActorAttributeThresholdCrossedFact> ThresholdFacts { get; }
        public string Reason { get; }

        public bool Applied => Outcome == ActorAttributeApplyOutcome.Applied;
        public bool Rejected => Outcome == ActorAttributeApplyOutcome.Rejected;
        public bool Failed => Outcome == ActorAttributeApplyOutcome.Failed;
        public bool HasThresholdFacts => ThresholdFacts is { Count: > 0 };
        public int ThresholdFactCount => ThresholdFacts == null ? 0 : ThresholdFacts.Count;

        private ActorAttributeApplyResult(
            ActorAttributeApplyOutcome outcome,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            bool hasFact,
            ActorAttributeChangedFact fact,
            IReadOnlyList<ActorAttributeThresholdCrossedFact> thresholdFacts,
            string reason)
        {
            Outcome = outcome;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            AttributeId = attributeId;
            HasFact = hasFact;
            Fact = fact;
            ThresholdFacts = thresholdFacts ?? EmptyThresholdFacts;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeApplyResult AppliedWithFact(ActorAttributeChangedFact fact)
        {
            return AppliedWithFact(fact, EmptyThresholdFacts);
        }

        public static ActorAttributeApplyResult AppliedWithFact(
            ActorAttributeChangedFact fact,
            IReadOnlyList<ActorAttributeThresholdCrossedFact> thresholdFacts)
        {
            return new ActorAttributeApplyResult(
                ActorAttributeApplyOutcome.Applied,
                fact.ActorInstanceRuntimeId,
                fact.AttributeId,
                true,
                fact,
                thresholdFacts,
                string.Empty);
        }

        public static ActorAttributeApplyResult Reject(ActorInstanceRuntimeId actorInstanceRuntimeId, ActorAttributeId attributeId, string reason)
        {
            return new ActorAttributeApplyResult(
                ActorAttributeApplyOutcome.Rejected,
                actorInstanceRuntimeId,
                attributeId,
                false,
                default,
                EmptyThresholdFacts,
                reason);
        }

        public static ActorAttributeApplyResult Fail(ActorInstanceRuntimeId actorInstanceRuntimeId, ActorAttributeId attributeId, string reason)
        {
            return new ActorAttributeApplyResult(
                ActorAttributeApplyOutcome.Failed,
                actorInstanceRuntimeId,
                attributeId,
                false,
                default,
                EmptyThresholdFacts,
                reason);
        }
    }
}
