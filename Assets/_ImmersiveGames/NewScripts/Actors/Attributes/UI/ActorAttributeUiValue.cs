using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    [Serializable]
    public readonly struct ActorAttributeUiValue
    {
        public ActorAttributeUiValue(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            float currentValue,
            float minValue,
            float maxValue,
            bool hasNormalizedValue,
            float normalizedValue,
            string source,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            AttributeId = attributeId;
            CurrentValue = currentValue;
            MinValue = minValue;
            MaxValue = maxValue;
            HasNormalizedValue = hasNormalizedValue;
            NormalizedValue = normalizedValue;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorAttributeId AttributeId { get; }
        public float CurrentValue { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public bool HasNormalizedValue { get; }
        public float NormalizedValue { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            AttributeId.IsValid &&
            IsFinite(CurrentValue) &&
            IsFinite(MinValue) &&
            IsFinite(MaxValue) &&
            (!HasNormalizedValue || IsFinite(NormalizedValue));

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
}
}
