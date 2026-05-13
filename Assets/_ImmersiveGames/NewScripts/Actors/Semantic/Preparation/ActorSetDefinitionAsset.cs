using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Semantic.Preparation
{
    [CreateAssetMenu(
        fileName = "ActorSetDefinition",
        menuName = "ImmersiveGames/NewScripts/Actors/Semantic/Actor Set Definition",
        order = 60)]
    public sealed class ActorSetDefinitionAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField] private ActorDefinitionAsset actorDefinition;
            [SerializeField] private bool required;

            public ActorDefinitionAsset ActorDefinition => actorDefinition;
            public string ActorId => actorDefinition != null ? actorDefinition.ActorId : string.Empty;
            public bool HasPrefabReference => actorDefinition != null && actorDefinition.PrefabReference != null;
            public ActorPlacementMode PlacementMode => actorDefinition != null ? actorDefinition.PlacementMode : ActorPlacementMode.None;
            public bool HasPlacementPlan => actorDefinition != null && actorDefinition.HasPlacementPlan;
            public bool Required => required;
        }

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => (IReadOnlyList<Entry>)entries ?? Array.Empty<Entry>();

        public bool TryValidate(out string errorMessage)
        {
            if (entries == null || entries.Count == 0)
            {
                errorMessage = string.Empty;
                return true;
            }

            HashSet<string> dedupe = new(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                ActorDefinitionAsset actorDefinition = entries[i].ActorDefinition;
                if (actorDefinition == null)
                {
                    errorMessage = $"entries[{i}].actorDefinition is required.";
                    return false;
                }

                if (!actorDefinition.TryValidate(out string actorDefinitionValidationError))
                {
                    errorMessage = $"entries[{i}].actorDefinition is invalid asset='{actorDefinition.name}' detail='{actorDefinitionValidationError}'.";
                    return false;
                }

                string actorId = entries[i].ActorId;
                if (string.IsNullOrWhiteSpace(actorId))
                {
                    errorMessage = $"entries[{i}].actorDefinition.actorId is required.";
                    return false;
                }

                if (!dedupe.Add(actorId))
                {
                    errorMessage = $"entries contains duplicate actorId='{actorId}'.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public IReadOnlyList<ActorSetEntry> ResolveEntriesOrFail(string owner)
        {
            if (!TryValidate(out string errorMessage))
            {
                string message = $"[FATAL][Config][ActorSetDefinition] owner='{Normalize(owner)}' asset='{name}' detail='{errorMessage}'.";
                DebugUtility.LogError<ActorSetDefinitionAsset>(message);
                throw new InvalidOperationException(message);
            }

            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<ActorSetEntry>();
            }

            List<ActorSetEntry> resolvedEntries = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                resolvedEntries.Add(new ActorSetEntry(
                    entries[i].ActorId,
                    entries[i].Required,
                    entries[i].HasPrefabReference,
                    entries[i].PlacementMode,
                    entries[i].HasPlacementPlan));
            }

            return resolvedEntries;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
