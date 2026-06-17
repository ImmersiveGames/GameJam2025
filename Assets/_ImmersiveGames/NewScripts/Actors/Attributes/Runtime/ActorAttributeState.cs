using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public sealed class ActorAttributeState
    {
        private static readonly ActorAttributeThresholdDefinition[] EmptyThresholds =
            Array.Empty<ActorAttributeThresholdDefinition>();

        public string ActorInstanceId { get; private set; }
        public ActorAttributeId AttributeId { get; private set; }
        public ActorAttributeDefinitionAsset Definition { get; private set; }
        public float InitialValue { get; private set; }
        public float MinValue { get; private set; }
        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }
        public IReadOnlyList<ActorAttributeThresholdDefinition> ThresholdDefinitions { get; private set; }
        public bool IsReady { get; private set; }

        public ActorAttributeState(
            string actorInstanceId,
            ActorAttributeDefinitionAsset definition,
            float initialValue,
            float minValue,
            float maxValue,
            IReadOnlyList<ActorAttributeThresholdDefinition> thresholdDefinitions = null)
        {
            ActorInstanceId = actorInstanceId ?? string.Empty;
            Definition = definition;
            AttributeId = ActorAttributeId.FromDefinition(definition);
            InitialValue = initialValue;
            MinValue = minValue;
            MaxValue = maxValue;
            CurrentValue = initialValue;
            ThresholdDefinitions = CopyThresholdDefinitions(thresholdDefinitions);
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

        private static IReadOnlyList<ActorAttributeThresholdDefinition> CopyThresholdDefinitions(
            IReadOnlyList<ActorAttributeThresholdDefinition> thresholdDefinitions)
        {
            if (thresholdDefinitions == null || thresholdDefinitions.Count == 0)
            {
                return EmptyThresholds;
            }

            var copy = new ActorAttributeThresholdDefinition[thresholdDefinitions.Count];
            for (var i = 0; i < thresholdDefinitions.Count; i++)
            {
                copy[i] = thresholdDefinitions[i];
            }

            return copy;
        }
    }
}
