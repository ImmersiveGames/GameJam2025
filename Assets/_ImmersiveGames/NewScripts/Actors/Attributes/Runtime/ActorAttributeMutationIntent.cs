using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeMutationIntent
    {
        public SessionActivityIdentity ActivityIdentity { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorAttributeId AttributeId { get; }
        public ActorAttributeOperation Operation { get; }
        public float Amount { get; }
        public float SetValue { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActivityIdentity.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            AttributeId.IsValid &&
            IsKnownOperation(Operation) &&
            HasValidPayload(Operation, Amount, SetValue);

        public ActorAttributeMutationIntent(
            SessionActivityIdentity activityIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            float amount,
            float setValue,
            string source,
            string reason)
        {
            ActivityIdentity = activityIdentity;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            AttributeId = attributeId;
            Operation = operation;
            Amount = amount;
            SetValue = setValue;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActorAttributeCommand ToCommand()
        {
            return new ActorAttributeCommand(
                ActivityIdentity,
                ActorInstanceRuntimeId,
                AttributeId,
                Operation,
                Amount,
                SetValue,
                Source,
                Reason);
        }

        public static ActorAttributeMutationIntent FromCommand(
            ActorId actorId,
            ActorAttributeCommand command)
        {
            return new ActorAttributeMutationIntent(
                command.ActivityIdentity,
                actorId,
                command.ActorInstanceRuntimeId,
                command.AttributeId,
                command.Operation,
                command.Amount,
                command.SetValue,
                command.Source,
                command.Reason);
        }

        public static ActorAttributeMutationIntent Set(
            SessionActivityIdentity activityIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            float value,
            string source,
            string reason)
        {
            return new ActorAttributeMutationIntent(
                activityIdentity,
                actorId,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.Set,
                0f,
                value,
                source,
                reason);
        }

        public static ActorAttributeMutationIntent Add(
            SessionActivityIdentity activityIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            float amount,
            string source,
            string reason)
        {
            return new ActorAttributeMutationIntent(
                activityIdentity,
                actorId,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.Add,
                amount,
                0f,
                source,
                reason);
        }

        public static ActorAttributeMutationIntent Subtract(
            SessionActivityIdentity activityIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            float amount,
            string source,
            string reason)
        {
            return new ActorAttributeMutationIntent(
                activityIdentity,
                actorId,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.Subtract,
                amount,
                0f,
                source,
                reason);
        }

        public static ActorAttributeMutationIntent ResetToInitial(
            SessionActivityIdentity activityIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            string source,
            string reason)
        {
            return new ActorAttributeMutationIntent(
                activityIdentity,
                actorId,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.ResetToInitial,
                0f,
                0f,
                source,
                reason);
        }

        public static ActorAttributeMutationIntent RestoreToMax(
            SessionActivityIdentity activityIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            string source,
            string reason)
        {
            return new ActorAttributeMutationIntent(
                activityIdentity,
                actorId,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.RestoreToMax,
                0f,
                0f,
                source,
                reason);
        }

        private static bool IsKnownOperation(ActorAttributeOperation operation)
        {
            return operation == ActorAttributeOperation.Set ||
                operation == ActorAttributeOperation.Add ||
                operation == ActorAttributeOperation.Subtract ||
                operation == ActorAttributeOperation.ResetToInitial ||
                operation == ActorAttributeOperation.RestoreToMax;
        }

        private static bool HasValidPayload(ActorAttributeOperation operation, float amount, float setValue)
        {
            if (!IsFinite(amount) || !IsFinite(setValue))
            {
                return false;
            }

            return operation switch
            {
                ActorAttributeOperation.Add => amount >= 0f,
                ActorAttributeOperation.Subtract => amount >= 0f,
                _ => true
            };
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
