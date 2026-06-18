using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeMutationResult
    {
        public ActorAttributeMutationOutcome Outcome { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorAttributeId AttributeId { get; }
        public ActorAttributeOperation Operation { get; }
        public bool HasApplyResult { get; }
        public ActorAttributeApplyResult ApplyResult { get; }
        public string Reason { get; }

        public bool Applied => Outcome == ActorAttributeMutationOutcome.Applied;
        public bool Rejected => Outcome == ActorAttributeMutationOutcome.Rejected;
        public bool Failed => Outcome == ActorAttributeMutationOutcome.Failed;
        public bool HasChangedFact => HasApplyResult && ApplyResult.HasFact && ApplyResult.Fact.Changed;
        public bool HasThresholdFacts => HasApplyResult && ApplyResult.HasThresholdFacts;

        private ActorAttributeMutationResult(
            ActorAttributeMutationOutcome outcome,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            bool hasApplyResult,
            ActorAttributeApplyResult applyResult,
            string reason)
        {
            Outcome = outcome;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            AttributeId = attributeId;
            Operation = operation;
            HasApplyResult = hasApplyResult;
            ApplyResult = applyResult;
            Reason = reason.TrimToEmpty();
        }

        public static ActorAttributeMutationResult AppliedResult(
            ActorAttributeMutationIntent intent,
            ActorAttributeApplyResult applyResult)
        {
            return new ActorAttributeMutationResult(
                ActorAttributeMutationOutcome.Applied,
                intent.ActorId,
                intent.ActorInstanceRuntimeId,
                intent.AttributeId,
                intent.Operation,
                true,
                applyResult,
                string.Empty);
        }

        public static ActorAttributeMutationResult Reject(
            ActorAttributeMutationIntent intent,
            string reason)
        {
            return new ActorAttributeMutationResult(
                ActorAttributeMutationOutcome.Rejected,
                intent.ActorId,
                intent.ActorInstanceRuntimeId,
                intent.AttributeId,
                intent.Operation,
                false,
                default,
                reason);
        }

        public static ActorAttributeMutationResult Fail(
            ActorAttributeMutationIntent intent,
            string reason)
        {
            return new ActorAttributeMutationResult(
                ActorAttributeMutationOutcome.Failed,
                intent.ActorId,
                intent.ActorInstanceRuntimeId,
                intent.AttributeId,
                intent.Operation,
                false,
                default,
                reason);
        }

        public static ActorAttributeMutationResult Reject(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            string reason)
        {
            return new ActorAttributeMutationResult(
                ActorAttributeMutationOutcome.Rejected,
                actorId,
                actorInstanceRuntimeId,
                attributeId,
                operation,
                false,
                default,
                reason);
        }

        public static ActorAttributeMutationResult Fail(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            string reason)
        {
            return new ActorAttributeMutationResult(
                ActorAttributeMutationOutcome.Failed,
                actorId,
                actorInstanceRuntimeId,
                attributeId,
                operation,
                false,
                default,
                reason);
        }

    }
}
