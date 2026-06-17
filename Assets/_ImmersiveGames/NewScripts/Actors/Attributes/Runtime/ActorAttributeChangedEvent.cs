using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public readonly struct ActorAttributeChangedEvent
    {
        public ActorAttributeChangedEvent(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            float previousValue,
            float currentValue,
            float minValue,
            float maxValue,
            bool clamped,
            string source,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            AttributeId = attributeId;
            Operation = operation;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            MinValue = minValue;
            MaxValue = maxValue;
            Clamped = clamped;
            Source = Normalize(source);
            Reason = Normalize(reason);
            HasNormalizedValue = TryNormalize(currentValue, minValue, maxValue, out var normalizedValue);
            NormalizedValue = normalizedValue;
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorAttributeId AttributeId { get; }
        public ActorAttributeOperation Operation { get; }
        public float PreviousValue { get; }
        public float CurrentValue { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public bool Clamped { get; }
        public bool HasNormalizedValue { get; }
        public float NormalizedValue { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            AttributeId.IsValid;

        private static bool TryNormalize(float currentValue, float minValue, float maxValue, out float normalizedValue)
        {
            normalizedValue = 0f;
            float denominator = maxValue - minValue;
            if (denominator <= float.Epsilon)
            {
                return false;
            }

            normalizedValue = (currentValue - minValue) / denominator;
            if (normalizedValue < 0f)
            {
                normalizedValue = 0f;
            }
            else if (normalizedValue > 1f)
            {
                normalizedValue = 1f;
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
