using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Semantic.Preparation
{
    [CreateAssetMenu(
        fileName = "PlayerSetDefinition",
        menuName = "ImmersiveGames/NewScripts/Actors/Semantic/Player Set Definition",
        order = 60)]
    public sealed class PlayerSetDefinitionAsset : ScriptableObject
    {
        public readonly struct PlayerActorResolvedEntry
        {
            public PlayerActorResolvedEntry(
                string playerId,
                bool required,
                ActorDefinitionAsset actorDefinition)
            {
                PlayerId = playerId;
                Required = required;
                ActorDefinition = actorDefinition;
            }

            public string PlayerId { get; }
            public bool Required { get; }
            public ActorDefinitionAsset ActorDefinition { get; }
            public GameObject Prefab => ActorDefinition != null ? ActorDefinition.PrefabReference : null;
            public ActorPlacementMode PlacementMode => ActorDefinition != null ? ActorDefinition.PlacementMode : ActorPlacementMode.None;
            public string PlacementId => ActorDefinition != null ? ActorDefinition.PlacementKey : string.Empty;
            public Vector3 LocalPosition => ActorDefinition != null ? ActorDefinition.LocalPosition : Vector3.zero;
            public Vector3 LocalRotation => ActorDefinition != null ? ActorDefinition.LocalRotation : Vector3.zero;
            public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId) && ActorDefinition != null;
        }

        [Serializable]
        public struct Entry
        {
            [SerializeField] private ActorDefinitionAsset actorDefinition;
            [SerializeField] private bool required;

            public ActorDefinitionAsset ActorDefinition => actorDefinition;
            public string PlayerId => actorDefinition != null ? actorDefinition.ActorId : string.Empty;
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

                if (actorDefinition.ActorKind != ActorKind.Player)
                {
                    errorMessage = $"entries[{i}].actorDefinition.actorKind must be Player for PlayerPreparation rail. actorId='{actorDefinition.ActorId}' actorKind='{actorDefinition.ActorKind}'.";
                    return false;
                }

                string playerId = entries[i].PlayerId;
                if (string.IsNullOrWhiteSpace(playerId))
                {
                    errorMessage = $"entries[{i}].actorDefinition.actorId is required.";
                    return false;
                }

                if (!dedupe.Add(playerId))
                {
                    errorMessage = $"entries contains duplicate playerId='{playerId}'.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public IReadOnlyList<PlayerSetEntry> ResolveEntriesOrFail(string owner)
        {
            if (!TryValidate(out string errorMessage))
            {
                string message = $"[FATAL][Config][PlayerSetDefinition] owner='{Normalize(owner)}' asset='{name}' detail='{errorMessage}'.";
                DebugUtility.LogError<PlayerSetDefinitionAsset>(message);
                throw new InvalidOperationException(message);
            }

            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<PlayerSetEntry>();
            }

            List<PlayerSetEntry> resolvedEntries = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                resolvedEntries.Add(new PlayerSetEntry(
                    entries[i].PlayerId,
                    entries[i].Required,
                    entries[i].HasPrefabReference,
                    entries[i].PlacementMode,
                    entries[i].HasPlacementPlan));
            }

            return resolvedEntries;
        }

        public IReadOnlyList<PlayerActorResolvedEntry> ResolvePlayerActorEntriesOrFail(string owner)
        {
            if (!TryValidate(out string errorMessage))
            {
                string message = $"[FATAL][Config][PlayerSetDefinition] owner='{Normalize(owner)}' asset='{name}' detail='{errorMessage}'.";
                DebugUtility.LogError<PlayerSetDefinitionAsset>(message);
                throw new InvalidOperationException(message);
            }

            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<PlayerActorResolvedEntry>();
            }

            List<PlayerActorResolvedEntry> resolvedEntries = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                resolvedEntries.Add(new PlayerActorResolvedEntry(
                    entries[i].PlayerId,
                    entries[i].Required,
                    entries[i].ActorDefinition));
            }

            return resolvedEntries;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
