using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Authoring
{
    [CreateAssetMenu(
        fileName = "ActorAttributeProfile",
        menuName = "ImmersiveGames/Actors/Attributes/Attribute Profile")]
    public sealed class ActorAttributeProfileAsset : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            private const int ThresholdNormalizedKeyScale = 10000;

            [SerializeField] private ActorAttributeDefinitionAsset definition;
            [SerializeField] private float initialValue;
            [SerializeField] private float minValue;
            [SerializeField] private float maxValue = 100f;
            [SerializeField] private ActorAttributeBoundaryThresholdMode boundaryThresholdMode =
                ActorAttributeBoundaryThresholdMode.IncludeDefaultBoundaryThresholds;
            [SerializeField] private List<ActorAttributeThresholdDefinition> thresholds = new List<ActorAttributeThresholdDefinition>();

            public ActorAttributeDefinitionAsset Definition => definition;
            public float InitialValue => initialValue;
            public float MinValue => minValue;
            public float MaxValue => maxValue;
            public ActorAttributeBoundaryThresholdMode BoundaryThresholdMode => boundaryThresholdMode;
            public IReadOnlyList<ActorAttributeThresholdDefinition> Thresholds => thresholds;

            public bool TryValidate(out string reason)
            {
                if (definition == null)
                {
                    reason = "definition_missing";
                    return false;
                }

                if (!definition.TryValidate(out var definitionReason))
                {
                    reason = $"definition_invalid:{definitionReason}";
                    return false;
                }

                if (minValue > maxValue)
                {
                    reason = "min_value_greater_than_max_value";
                    return false;
                }

                if (initialValue < minValue || initialValue > maxValue)
                {
                    reason = "initial_value_out_of_range";
                    return false;
                }

                if (!IsKnownBoundaryThresholdMode(boundaryThresholdMode))
                {
                    reason = "boundary_threshold_mode_unknown";
                    return false;
                }

                if (!TryValidateThresholds(out var thresholdReason))
                {
                    reason = thresholdReason;
                    return false;
                }

                reason = string.Empty;
                return true;
            }

            public ActorAttributeState CreateState(string actorInstanceId)
            {
                return new ActorAttributeState(
                    actorInstanceId,
                    definition,
                    initialValue,
                    minValue,
                    maxValue,
                    ResolveThresholdDefinitions());
            }

            private IReadOnlyList<ActorAttributeThresholdDefinition> ResolveThresholdDefinitions()
            {
                var explicitCount = thresholds == null ? 0 : thresholds.Count;
                var resolved = new List<ActorAttributeThresholdDefinition>(explicitCount + 2);

                if (explicitCount > 0)
                {
                    for (var i = 0; i < thresholds.Count; i++)
                    {
                        var threshold = thresholds[i];
                        if (threshold != null)
                        {
                            resolved.Add(threshold);
                        }
                    }
                }

                if (boundaryThresholdMode == ActorAttributeBoundaryThresholdMode.IncludeDefaultBoundaryThresholds)
                {
                    var attributeId = ActorAttributeId.FromDefinition(definition);
                    AddBoundaryThresholdIfMissing(resolved, ActorAttributeThresholdDefinition.CreateDepleted(attributeId));
                    AddBoundaryThresholdIfMissing(resolved, ActorAttributeThresholdDefinition.CreateFull(attributeId));
                }

                if (resolved.Count == 0)
                {
                    return Array.Empty<ActorAttributeThresholdDefinition>();
                }

                return resolved.ToArray();
            }

            private bool TryValidateThresholds(out string reason)
            {
                var seenThresholds = new HashSet<string>(StringComparer.Ordinal);
                var seenThresholdBoundaries = new HashSet<string>(StringComparer.Ordinal);

                if (thresholds != null && thresholds.Count > 0)
                {
                    for (var i = 0; i < thresholds.Count; i++)
                    {
                        var threshold = thresholds[i];
                        if (threshold == null)
                        {
                            reason = $"threshold_null:index={i}";
                            return false;
                        }

                        if (!threshold.TryValidate(out var thresholdReason))
                        {
                            reason = $"threshold_invalid:index={i};reason={thresholdReason}";
                            return false;
                        }

                        var identityKey = BuildThresholdIdentityKey(threshold.ThresholdId, threshold.Direction);
                        if (!seenThresholds.Add(identityKey))
                        {
                            reason = $"duplicate_threshold:index={i};thresholdId={threshold.ThresholdId};direction={threshold.Direction}";
                            return false;
                        }

                        var boundaryKey = BuildThresholdBoundaryKey(threshold.NormalizedValue, threshold.Direction);
                        if (!seenThresholdBoundaries.Add(boundaryKey))
                        {
                            reason = $"duplicate_threshold_boundary:index={i};normalizedValue={threshold.NormalizedValue};direction={threshold.Direction}";
                            return false;
                        }
                    }
                }

                reason = string.Empty;
                return true;
            }

            private static void AddBoundaryThresholdIfMissing(
                List<ActorAttributeThresholdDefinition> resolved,
                ActorAttributeThresholdDefinition boundaryThreshold)
            {
                if (boundaryThreshold == null || !boundaryThreshold.IsValid)
                {
                    return;
                }

                for (var i = 0; i < resolved.Count; i++)
                {
                    var existing = resolved[i];
                    if (existing == null)
                    {
                        continue;
                    }

                    if (AreEquivalentBoundaryThresholds(existing, boundaryThreshold))
                    {
                        return;
                    }
                }

                resolved.Add(boundaryThreshold);
            }

            private static bool AreEquivalentBoundaryThresholds(
                ActorAttributeThresholdDefinition left,
                ActorAttributeThresholdDefinition right)
            {
                if (left == null || right == null)
                {
                    return false;
                }

                return left.Direction == right.Direction &&
                       BuildThresholdBoundaryKey(left.NormalizedValue, left.Direction) ==
                       BuildThresholdBoundaryKey(right.NormalizedValue, right.Direction);
            }

            private static bool IsKnownBoundaryThresholdMode(ActorAttributeBoundaryThresholdMode candidate)
            {
                return candidate == ActorAttributeBoundaryThresholdMode.IncludeDefaultBoundaryThresholds ||
                       candidate == ActorAttributeBoundaryThresholdMode.ExplicitOnly;
            }

            private static string BuildThresholdIdentityKey(
                ActorAttributeThresholdId thresholdId,
                ActorAttributeThresholdDirection direction)
            {
                return $"{thresholdId.Value}|{(int)direction}";
            }

            private static string BuildThresholdBoundaryKey(
                float normalizedValue,
                ActorAttributeThresholdDirection direction)
            {
                var scaledValue = (int)Math.Round(
                    normalizedValue * ThresholdNormalizedKeyScale,
                    MidpointRounding.AwayFromZero);
                return $"{scaledValue}|{(int)direction}";
            }

#if UNITY_EDITOR
            public void OnValidate()
            {
                if (!IsKnownBoundaryThresholdMode(boundaryThresholdMode))
                {
                    boundaryThresholdMode = ActorAttributeBoundaryThresholdMode.IncludeDefaultBoundaryThresholds;
                }

                if (thresholds == null)
                {
                    thresholds = new List<ActorAttributeThresholdDefinition>();
                    return;
                }

                for (var i = 0; i < thresholds.Count; i++)
                {
                    thresholds[i]?.OnValidate();
                }
            }
#endif
        }

        [SerializeField] private string profileId = string.Empty;
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public string ProfileId => profileId;
        public IReadOnlyList<Entry> Entries => entries;

        public bool TryValidate(out string reason)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                reason = "profile_id_missing";
                return false;
            }

            var seenAttributeIds = new HashSet<ActorAttributeId>();

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    reason = $"entry_null:index={i}";
                    return false;
                }

                if (!entry.TryValidate(out var entryReason))
                {
                    reason = $"entry_invalid:index={i};reason={entryReason}";
                    return false;
                }

                var attributeId = entry.Definition.ToRuntimeId();
                if (!seenAttributeIds.Add(attributeId))
                {
                    reason = $"duplicate_attribute_definition:index={i};attributeId={attributeId}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public ActorAttributeState[] CreateStates(string actorInstanceId)
        {
            var states = new ActorAttributeState[entries.Count];

            for (var i = 0; i < entries.Count; i++)
            {
                states[i] = entries[i].CreateState(actorInstanceId);
            }

            return states;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(profileId))
            {
                profileId = profileId.Trim();
            }

            if (entries == null)
            {
                entries = new List<Entry>();
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                entries[i]?.OnValidate();
            }
        }
#endif
    }
}
