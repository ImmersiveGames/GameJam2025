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
            [SerializeField] private ActorAttributeDefinitionAsset definition;
            [SerializeField] private float initialValue;
            [SerializeField] private float minValue;
            [SerializeField] private float maxValue = 100f;

            public ActorAttributeDefinitionAsset Definition => definition;
            public float InitialValue => initialValue;
            public float MinValue => minValue;
            public float MaxValue => maxValue;

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
                    maxValue);
            }
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
        }
#endif
    }
}
