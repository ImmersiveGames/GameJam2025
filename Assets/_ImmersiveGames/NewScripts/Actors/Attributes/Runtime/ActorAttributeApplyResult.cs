using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeApplyResult
    {
        public ActorAttributeApplyOutcome Outcome { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorAttributeId AttributeId { get; }
        public bool HasFact { get; }
        public ActorAttributeChangedFact Fact { get; }
        public string Reason { get; }

        public bool Applied => Outcome == ActorAttributeApplyOutcome.Applied;
        public bool Rejected => Outcome == ActorAttributeApplyOutcome.Rejected;
        public bool Failed => Outcome == ActorAttributeApplyOutcome.Failed;

        private ActorAttributeApplyResult(
            ActorAttributeApplyOutcome outcome,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            bool hasFact,
            ActorAttributeChangedFact fact,
            string reason)
        {
            Outcome = outcome;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            AttributeId = attributeId;
            HasFact = hasFact;
            Fact = fact;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeApplyResult AppliedWithFact(ActorAttributeChangedFact fact)
        {
            return new ActorAttributeApplyResult(
                ActorAttributeApplyOutcome.Applied,
                fact.ActorInstanceRuntimeId,
                fact.AttributeId,
                true,
                fact,
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
                reason);
        }
    }
}
