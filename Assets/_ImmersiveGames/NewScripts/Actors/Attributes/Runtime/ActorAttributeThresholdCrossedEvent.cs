using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeThresholdCrossedEvent
    {
        public ActorAttributeThresholdCrossedEvent(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            ActorAttributeThresholdId thresholdId,
            ActorAttributeThresholdPresetKind presetKind,
            ActorAttributeThresholdDirection direction,
            float thresholdNormalizedValue,
            float previousValue,
            float currentValue,
            float minValue,
            float maxValue,
            float previousNormalizedValue,
            float currentNormalizedValue,
            string source,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            AttributeId = attributeId;
            Operation = operation;
            ThresholdId = thresholdId;
            PresetKind = presetKind;
            Direction = direction;
            ThresholdNormalizedValue = Clamp01(thresholdNormalizedValue);
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            MinValue = minValue;
            MaxValue = maxValue;
            PreviousNormalizedValue = Clamp01(previousNormalizedValue);
            CurrentNormalizedValue = Clamp01(currentNormalizedValue);
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorAttributeId AttributeId { get; }
        public ActorAttributeOperation Operation { get; }
        public ActorAttributeThresholdId ThresholdId { get; }
        public ActorAttributeThresholdPresetKind PresetKind { get; }
        public ActorAttributeThresholdDirection Direction { get; }
        public float ThresholdNormalizedValue { get; }
        public float PreviousValue { get; }
        public float CurrentValue { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public float PreviousNormalizedValue { get; }
        public float CurrentNormalizedValue { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            AttributeId.IsValid &&
            ThresholdId.IsValid &&
            ActorAttributeThresholdDefinition.IsKnownPresetKind(PresetKind) &&
            ActorAttributeThresholdDefinition.IsKnownDirection(Direction) &&
            IsFinite01(ThresholdNormalizedValue) &&
            IsFinite01(PreviousNormalizedValue) &&
            IsFinite01(CurrentNormalizedValue);

        private static bool IsFinite01(float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value) &&
                value is >= 0f and <= 1f;
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
