using System;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeThresholdEvaluationResult
    {
        private ActorAttributeThresholdEvaluationResult(
            ActorAttributeThresholdId thresholdId,
            ActorAttributeThresholdDirection direction,
            float thresholdNormalizedValue,
            float previousNormalizedValue,
            float currentNormalizedValue,
            bool crossed,
            string reason)
        {
            ThresholdId = thresholdId;
            Direction = direction;
            ThresholdNormalizedValue = thresholdNormalizedValue;
            PreviousNormalizedValue = previousNormalizedValue;
            CurrentNormalizedValue = currentNormalizedValue;
            Crossed = crossed;
            Reason = reason.TrimToEmpty();
        }

        public ActorAttributeThresholdId ThresholdId { get; }
        public ActorAttributeThresholdDirection Direction { get; }
        public float ThresholdNormalizedValue { get; }
        public float PreviousNormalizedValue { get; }
        public float CurrentNormalizedValue { get; }
        public bool Crossed { get; }
        public string Reason { get; }

        public bool IsValid =>
            ThresholdId.IsValid &&
            ActorAttributeThresholdDefinition.IsKnownDirection(Direction) &&
            IsFinite01(ThresholdNormalizedValue) &&
            IsFinite01(PreviousNormalizedValue) &&
            IsFinite01(CurrentNormalizedValue);

        public static ActorAttributeThresholdEvaluationResult CrossedResult(
            ActorAttributeThresholdDefinition definition,
            float previousNormalizedValue,
            float currentNormalizedValue)
        {
            return FromDefinition(
                definition,
                previousNormalizedValue,
                currentNormalizedValue,
                true,
                "threshold_crossed");
        }

        public static ActorAttributeThresholdEvaluationResult NotCrossed(
            ActorAttributeThresholdDefinition definition,
            float previousNormalizedValue,
            float currentNormalizedValue,
            string reason)
        {
            return FromDefinition(
                definition,
                previousNormalizedValue,
                currentNormalizedValue,
                false,
                reason);
        }

        public static ActorAttributeThresholdEvaluationResult Invalid(string reason)
        {
            return new ActorAttributeThresholdEvaluationResult(
                ActorAttributeThresholdId.Empty,
                ActorAttributeThresholdDirection.Unknown,
                0f,
                0f,
                0f,
                false,
                reason);
        }

        private static ActorAttributeThresholdEvaluationResult FromDefinition(
            ActorAttributeThresholdDefinition definition,
            float previousNormalizedValue,
            float currentNormalizedValue,
            bool crossed,
            string reason)
        {
            if (definition == null || !definition.IsValid)
            {
                return Invalid("threshold_definition_invalid");
            }

            return new ActorAttributeThresholdEvaluationResult(
                definition.ThresholdId,
                definition.Direction,
                definition.NormalizedValue,
                Clamp01(previousNormalizedValue),
                Clamp01(currentNormalizedValue),
                crossed,
                reason);
        }

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
