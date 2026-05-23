using System;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeCommand
    {
        public string PipelineIdentity { get; }
        public string ActivityIdentity { get; }
        public string ActorInstanceId { get; }
        public ActorAttributeId AttributeId { get; }
        public ActorAttributeOperation Operation { get; }
        public float Amount { get; }
        public float SetValue { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasValidTarget => AttributeId.IsValid && !string.IsNullOrWhiteSpace(ActorInstanceId);

        public ActorAttributeCommand(
            string pipelineIdentity,
            string activityIdentity,
            string actorInstanceId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            float amount,
            float setValue,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity ?? string.Empty;
            ActivityIdentity = activityIdentity ?? string.Empty;
            ActorInstanceId = actorInstanceId ?? string.Empty;
            AttributeId = attributeId;
            Operation = operation;
            Amount = amount;
            SetValue = setValue;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public static ActorAttributeCommand Set(
            string pipelineIdentity,
            string activityIdentity,
            string actorInstanceId,
            ActorAttributeId attributeId,
            float value,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                pipelineIdentity,
                activityIdentity,
                actorInstanceId,
                attributeId,
                ActorAttributeOperation.Set,
                0f,
                value,
                source,
                reason);
        }

        public static ActorAttributeCommand Add(
            string pipelineIdentity,
            string activityIdentity,
            string actorInstanceId,
            ActorAttributeId attributeId,
            float amount,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                pipelineIdentity,
                activityIdentity,
                actorInstanceId,
                attributeId,
                ActorAttributeOperation.Add,
                amount,
                0f,
                source,
                reason);
        }

        public static ActorAttributeCommand Subtract(
            string pipelineIdentity,
            string activityIdentity,
            string actorInstanceId,
            ActorAttributeId attributeId,
            float amount,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                pipelineIdentity,
                activityIdentity,
                actorInstanceId,
                attributeId,
                ActorAttributeOperation.Subtract,
                amount,
                0f,
                source,
                reason);
        }

        public static ActorAttributeCommand ResetToInitial(
            string pipelineIdentity,
            string activityIdentity,
            string actorInstanceId,
            ActorAttributeId attributeId,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                pipelineIdentity,
                activityIdentity,
                actorInstanceId,
                attributeId,
                ActorAttributeOperation.ResetToInitial,
                0f,
                0f,
                source,
                reason);
        }

        public static ActorAttributeCommand RestoreToMax(
            string pipelineIdentity,
            string activityIdentity,
            string actorInstanceId,
            ActorAttributeId attributeId,
            string source,
            string reason)
        {
            return new ActorAttributeCommand(
                pipelineIdentity,
                activityIdentity,
                actorInstanceId,
                attributeId,
                ActorAttributeOperation.RestoreToMax,
                0f,
                0f,
                source,
                reason);
        }
    }
}
