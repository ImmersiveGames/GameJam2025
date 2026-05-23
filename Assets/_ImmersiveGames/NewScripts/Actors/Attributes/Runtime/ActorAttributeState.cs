using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public sealed class ActorAttributeState
    {
        public string ActorInstanceId { get; private set; }
        public ActorAttributeId AttributeId { get; private set; }
        public ActorAttributeDefinitionAsset Definition { get; private set; }
        public float InitialValue { get; private set; }
        public float MinValue { get; private set; }
        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }
        public bool IsReady { get; private set; }

        public ActorAttributeState(
            string actorInstanceId,
            ActorAttributeDefinitionAsset definition,
            float initialValue,
            float minValue,
            float maxValue)
        {
            ActorInstanceId = actorInstanceId ?? string.Empty;
            Definition = definition;
            AttributeId = ActorAttributeId.FromDefinition(definition);
            InitialValue = initialValue;
            MinValue = minValue;
            MaxValue = maxValue;
            CurrentValue = initialValue;
            IsReady = definition != null && AttributeId.IsValid;
        }

        public void SetCurrentValue(float value)
        {
            CurrentValue = Clamp(value);
        }

        public float Clamp(float value)
        {
            if (value < MinValue)
            {
                return MinValue;
            }

            if (value > MaxValue)
            {
                return MaxValue;
            }

            return value;
        }

        public void ResetToInitial()
        {
            CurrentValue = Clamp(InitialValue);
        }

        public void RestoreToMax()
        {
            CurrentValue = MaxValue;
        }

        public bool Matches(ActorAttributeId attributeId)
        {
            return AttributeId == attributeId;
        }
    }
}
