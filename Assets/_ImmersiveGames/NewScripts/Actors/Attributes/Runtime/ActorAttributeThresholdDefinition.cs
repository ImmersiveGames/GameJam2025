using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public sealed class ActorAttributeThresholdDefinition
    {
        private const float DepletedNormalizedValue = 0f;
        private const float FullNormalizedValue = 1f;

        [SerializeField] private ActorAttributeThresholdPresetKind presetKind = ActorAttributeThresholdPresetKind.Custom;
        [SerializeField] private ActorAttributeThresholdId thresholdId;
        [SerializeField, Range(0f, 1f)] private float normalizedValue;
        [SerializeField] private ActorAttributeThresholdDirection direction = ActorAttributeThresholdDirection.Descending;
        [SerializeField] private bool emitOnInitialState;

        public ActorAttributeThresholdDefinition()
        {
        }

        public ActorAttributeThresholdDefinition(
            ActorAttributeThresholdId thresholdId,
            float normalizedValue,
            ActorAttributeThresholdDirection direction,
            bool emitOnInitialState = false)
            : this(
                ActorAttributeThresholdPresetKind.Custom,
                thresholdId,
                normalizedValue,
                direction,
                emitOnInitialState)
        {
        }

        public ActorAttributeThresholdDefinition(
            ActorAttributeThresholdPresetKind presetKind,
            ActorAttributeThresholdId thresholdId,
            float normalizedValue,
            ActorAttributeThresholdDirection direction,
            bool emitOnInitialState = false)
        {
            this.presetKind = presetKind;
            this.thresholdId = thresholdId;
            this.normalizedValue = Clamp01(normalizedValue);
            this.direction = direction;
            this.emitOnInitialState = emitOnInitialState;
            ApplyPresetShapeIfNeeded();
        }

        public ActorAttributeThresholdPresetKind PresetKind => presetKind;
        public ActorAttributeThresholdId ThresholdId => thresholdId;
        public float NormalizedValue => normalizedValue;
        public ActorAttributeThresholdDirection Direction => direction;
        public bool EmitOnInitialState => emitOnInitialState;

        public bool IsBoundaryPreset =>
            presetKind == ActorAttributeThresholdPresetKind.Depleted ||
            presetKind == ActorAttributeThresholdPresetKind.Full;

        public bool IsValid =>
            thresholdId.IsValid &&
            IsKnownPresetKind(presetKind) &&
            IsKnownDirection(direction) &&
            IsValidNormalizedValue(normalizedValue) &&
            IsPresetShapeValid();

        public bool TryValidate(out string reason)
        {
            if (!thresholdId.IsValid)
            {
                reason = "threshold_id_missing";
                return false;
            }

            if (!IsKnownPresetKind(presetKind))
            {
                reason = "threshold_preset_unknown";
                return false;
            }

            if (!IsKnownDirection(direction))
            {
                reason = "threshold_direction_unknown";
                return false;
            }

            if (!IsValidNormalizedValue(normalizedValue))
            {
                reason = "threshold_normalized_value_out_of_range";
                return false;
            }

            if (!IsPresetShapeValid())
            {
                reason = "threshold_preset_shape_invalid";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static ActorAttributeThresholdDefinition CreateDepleted(
            ActorAttributeId attributeId,
            bool emitOnInitialState = false)
        {
            return new ActorAttributeThresholdDefinition(
                ActorAttributeThresholdPresetKind.Depleted,
                BuildBoundaryThresholdId(attributeId, "depleted"),
                DepletedNormalizedValue,
                ActorAttributeThresholdDirection.Descending,
                emitOnInitialState);
        }

        public static ActorAttributeThresholdDefinition CreateFull(
            ActorAttributeId attributeId,
            bool emitOnInitialState = false)
        {
            return new ActorAttributeThresholdDefinition(
                ActorAttributeThresholdPresetKind.Full,
                BuildBoundaryThresholdId(attributeId, "full"),
                FullNormalizedValue,
                ActorAttributeThresholdDirection.Ascending,
                emitOnInitialState);
        }

        public static bool IsKnownDirection(ActorAttributeThresholdDirection candidate)
        {
            return candidate == ActorAttributeThresholdDirection.Descending ||
                   candidate == ActorAttributeThresholdDirection.Ascending;
        }

        public static bool IsKnownPresetKind(ActorAttributeThresholdPresetKind candidate)
        {
            return candidate == ActorAttributeThresholdPresetKind.Custom ||
                   candidate == ActorAttributeThresholdPresetKind.Depleted ||
                   candidate == ActorAttributeThresholdPresetKind.Full;
        }

        private bool IsPresetShapeValid()
        {
            switch (presetKind)
            {
                case ActorAttributeThresholdPresetKind.Custom:
                    return true;
                case ActorAttributeThresholdPresetKind.Depleted:
                    return direction == ActorAttributeThresholdDirection.Descending &&
                           NearlyEquals(normalizedValue, DepletedNormalizedValue);
                case ActorAttributeThresholdPresetKind.Full:
                    return direction == ActorAttributeThresholdDirection.Ascending &&
                           NearlyEquals(normalizedValue, FullNormalizedValue);
                default:
                    return false;
            }
        }

        private void ApplyPresetShapeIfNeeded()
        {
            switch (presetKind)
            {
                case ActorAttributeThresholdPresetKind.Depleted:
                    normalizedValue = DepletedNormalizedValue;
                    direction = ActorAttributeThresholdDirection.Descending;
                    break;
                case ActorAttributeThresholdPresetKind.Full:
                    normalizedValue = FullNormalizedValue;
                    direction = ActorAttributeThresholdDirection.Ascending;
                    break;
            }
        }

        private static ActorAttributeThresholdId BuildBoundaryThresholdId(
            ActorAttributeId attributeId,
            string suffix)
        {
            if (!attributeId.IsValid || string.IsNullOrWhiteSpace(suffix))
            {
                return ActorAttributeThresholdId.Empty;
            }

            return new ActorAttributeThresholdId($"{attributeId.Value}.{suffix.Trim()}");
        }

        private static bool IsValidNormalizedValue(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value) &&
                   value >= 0f &&
                   value <= 1f;
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

        private static bool NearlyEquals(float left, float right)
        {
            return Math.Abs(left - right) <= 0.0001f;
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            normalizedValue = Clamp01(normalizedValue);
            ApplyPresetShapeIfNeeded();
        }
#endif
    }
}
