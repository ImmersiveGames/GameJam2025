using System;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeApplyResult
    {
        public ActorAttributeApplyOutcome Outcome { get; }
        public string ActorInstanceId { get; }
        public ActorAttributeId AttributeId { get; }
        public bool HasFact { get; }
        public ActorAttributeChangedFact Fact { get; }
        public string Reason { get; }

        public bool Applied => Outcome == ActorAttributeApplyOutcome.Applied;
        public bool Rejected => Outcome == ActorAttributeApplyOutcome.Rejected;
        public bool Failed => Outcome == ActorAttributeApplyOutcome.Failed;

        private ActorAttributeApplyResult(
            ActorAttributeApplyOutcome outcome,
            string actorInstanceId,
            ActorAttributeId attributeId,
            bool hasFact,
            ActorAttributeChangedFact fact,
            string reason)
        {
            Outcome = outcome;
            ActorInstanceId = actorInstanceId ?? string.Empty;
            AttributeId = attributeId;
            HasFact = hasFact;
            Fact = fact;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeApplyResult AppliedWithFact(ActorAttributeChangedFact fact)
        {
            return new ActorAttributeApplyResult(
                ActorAttributeApplyOutcome.Applied,
                fact.ActorInstanceId,
                fact.AttributeId,
                true,
                fact,
                string.Empty);
        }

        public static ActorAttributeApplyResult Reject(string actorInstanceId, ActorAttributeId attributeId, string reason)
        {
            return new ActorAttributeApplyResult(
                ActorAttributeApplyOutcome.Rejected,
                actorInstanceId,
                attributeId,
                false,
                default,
                reason);
        }

        public static ActorAttributeApplyResult Fail(string actorInstanceId, ActorAttributeId attributeId, string reason)
        {
            return new ActorAttributeApplyResult(
                ActorAttributeApplyOutcome.Failed,
                actorInstanceId,
                attributeId,
                false,
                default,
                reason);
        }
    }
}
