using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public static class ActorAttributeThresholdEvaluator
    {
        private static readonly ActorAttributeThresholdCrossedFact[] EmptyFacts =
            Array.Empty<ActorAttributeThresholdCrossedFact>();

        public static IReadOnlyList<ActorAttributeThresholdCrossedFact> EvaluateCrossedThresholds(
            SessionActivityIdentity activityIdentity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            float previousValue,
            float currentValue,
            float minValue,
            float maxValue,
            IReadOnlyList<ActorAttributeThresholdDefinition> thresholdDefinitions,
            string source,
            string reason)
        {
            if (!actorInstanceRuntimeId.IsValid ||
                !attributeId.IsValid ||
                thresholdDefinitions == null ||
                thresholdDefinitions.Count == 0)
            {
                return EmptyFacts;
            }

            if (Math.Abs(previousValue - currentValue) <= float.Epsilon)
            {
                return EmptyFacts;
            }

            if (!TryNormalize(previousValue, minValue, maxValue, out var previousNormalizedValue) ||
                !TryNormalize(currentValue, minValue, maxValue, out var currentNormalizedValue))
            {
                return EmptyFacts;
            }

            var crossedFacts = new List<ActorAttributeThresholdCrossedFact>();
            for (var i = 0; i < thresholdDefinitions.Count; i++)
            {
                var definition = thresholdDefinitions[i];
                if (definition == null || !definition.IsValid)
                {
                    continue;
                }

                if (!DidCross(definition, previousNormalizedValue, currentNormalizedValue))
                {
                    continue;
                }

                crossedFacts.Add(new ActorAttributeThresholdCrossedFact(
                    activityIdentity,
                    actorInstanceRuntimeId,
                    attributeId,
                    operation,
                    definition.ThresholdId,
                    definition.PresetKind,
                    definition.Direction,
                    definition.NormalizedValue,
                    previousValue,
                    currentValue,
                    minValue,
                    maxValue,
                    previousNormalizedValue,
                    currentNormalizedValue,
                    source,
                    reason));
            }

            if (crossedFacts.Count == 0)
            {
                return EmptyFacts;
            }

            return crossedFacts.ToArray();
        }

        public static ActorAttributeThresholdEvaluationResult Evaluate(
            ActorAttributeThresholdDefinition definition,
            float previousValue,
            float currentValue,
            float minValue,
            float maxValue)
        {
            if (definition == null || !definition.IsValid)
            {
                return ActorAttributeThresholdEvaluationResult.Invalid("threshold_definition_invalid");
            }

            if (!TryNormalize(previousValue, minValue, maxValue, out var previousNormalizedValue) ||
                !TryNormalize(currentValue, minValue, maxValue, out var currentNormalizedValue))
            {
                return ActorAttributeThresholdEvaluationResult.Invalid("attribute_range_invalid");
            }

            if (Math.Abs(previousValue - currentValue) <= float.Epsilon)
            {
                return ActorAttributeThresholdEvaluationResult.NotCrossed(
                    definition,
                    previousNormalizedValue,
                    currentNormalizedValue,
                    "attribute_value_unchanged");
            }

            if (!DidCross(definition, previousNormalizedValue, currentNormalizedValue))
            {
                return ActorAttributeThresholdEvaluationResult.NotCrossed(
                    definition,
                    previousNormalizedValue,
                    currentNormalizedValue,
                    "threshold_not_crossed");
            }

            return ActorAttributeThresholdEvaluationResult.CrossedResult(
                definition,
                previousNormalizedValue,
                currentNormalizedValue);
        }

        private static bool DidCross(
            ActorAttributeThresholdDefinition definition,
            float previousNormalizedValue,
            float currentNormalizedValue)
        {
            var threshold = definition.NormalizedValue;
            switch (definition.Direction)
            {
                case ActorAttributeThresholdDirection.Descending:
                    return previousNormalizedValue > threshold && currentNormalizedValue <= threshold;
                case ActorAttributeThresholdDirection.Ascending:
                    return previousNormalizedValue < threshold && currentNormalizedValue >= threshold;
                default:
                    return false;
            }
        }

        private static bool TryNormalize(float value, float minValue, float maxValue, out float normalizedValue)
        {
            normalizedValue = 0f;
            var denominator = maxValue - minValue;
            if (float.IsNaN(value) ||
                float.IsInfinity(value) ||
                float.IsNaN(minValue) ||
                float.IsInfinity(minValue) ||
                float.IsNaN(maxValue) ||
                float.IsInfinity(maxValue) ||
                denominator <= float.Epsilon)
            {
                return false;
            }

            normalizedValue = (value - minValue) / denominator;
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
    }
}
