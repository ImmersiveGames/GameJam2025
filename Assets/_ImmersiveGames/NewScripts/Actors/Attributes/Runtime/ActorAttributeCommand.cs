using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeCommand
    {
        public SessionActivityIdentity ActivityIdentity { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorAttributeId AttributeId { get; }
        public ActorAttributeOperation Operation { get; }
        public float Amount { get; }
        public float SetValue { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasValidTarget => ActivityIdentity.IsValid && AttributeId.IsValid && ActorInstanceRuntimeId.IsValid;

        public ActorAttributeCommand(
            SessionActivityIdentity activityIdentity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            float amount,
            float setValue,
            string source,
            string reason)
        {
            ActivityIdentity = activityIdentity;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            AttributeId = attributeId;
            Operation = operation;
            Amount = amount;
            SetValue = setValue;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeCommand Set(
            SessionActivityIdentity activityIdentity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            float value,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                activityIdentity,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.Set,
                0f,
                value,
                source,
                reason);
        }

        public static ActorAttributeCommand Add(
            SessionActivityIdentity activityIdentity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            float amount,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                activityIdentity,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.Add,
                amount,
                0f,
                source,
                reason);
        }

        public static ActorAttributeCommand Subtract(
            SessionActivityIdentity activityIdentity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            float amount,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                activityIdentity,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.Subtract,
                amount,
                0f,
                source,
                reason);
        }

        public static ActorAttributeCommand ResetToInitial(
            SessionActivityIdentity activityIdentity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                activityIdentity,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.ResetToInitial,
                0f,
                0f,
                source,
                reason);
        }

        public static ActorAttributeCommand RestoreToMax(
            SessionActivityIdentity activityIdentity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                activityIdentity,
                actorInstanceRuntimeId,
                attributeId,
                ActorAttributeOperation.RestoreToMax,
                0f,
                0f,
                source,
                reason);
        }
    }
}
