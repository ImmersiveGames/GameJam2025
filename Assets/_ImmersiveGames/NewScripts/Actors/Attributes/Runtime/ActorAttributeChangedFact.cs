using System;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeChangedFact
    {
        public string PipelineIdentity { get; }
        public string ActivityIdentity { get; }
        public string ActorInstanceId { get; }
        public ActorAttributeId AttributeId { get; }
        public string AttributeStableId { get; }
        public ActorAttributeOperation Operation { get; }
        public float PreviousValue { get; }
        public float NewValue { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public bool Clamped { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool Changed => Math.Abs(NewValue - PreviousValue) > float.Epsilon;

        public ActorAttributeChangedFact(
            string pipelineIdentity,
            string activityIdentity,
            string actorInstanceId,
            ActorAttributeId attributeId,
            string attributeStableId,
            ActorAttributeOperation operation,
            float previousValue,
            float newValue,
            float minValue,
            float maxValue,
            bool clamped,
            string source,
            string reason)
        {
            PipelineIdentity = pipelineIdentity ?? string.Empty;
            ActivityIdentity = activityIdentity ?? string.Empty;
            ActorInstanceId = actorInstanceId ?? string.Empty;
            AttributeId = attributeId;
            AttributeStableId = attributeStableId ?? string.Empty;
            Operation = operation;
            PreviousValue = previousValue;
            NewValue = newValue;
            MinValue = minValue;
            MaxValue = maxValue;
            Clamped = clamped;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }
}
