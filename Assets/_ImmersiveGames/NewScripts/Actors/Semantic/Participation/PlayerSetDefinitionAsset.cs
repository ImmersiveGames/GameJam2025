using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Participation
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
                PlayerSlotId playerSlotId,
                PlayerSelectionId playerSelectionId,
                ActorDefinitionId actorDefinitionId,
                ActorId actorId,
                bool required,
                ActorDefinitionAsset actorDefinition)
            {
                PlayerSlotId = playerSlotId;
                PlayerSelectionId = playerSelectionId;
                ActorDefinitionId = actorDefinitionId;
                ActorId = actorId;
                Required = required;
                ActorDefinition = actorDefinition;
            }

            public PlayerSlotId PlayerSlotId { get; }
            public PlayerSelectionId PlayerSelectionId { get; }
            public ActorDefinitionId ActorDefinitionId { get; }
            public ActorId ActorId { get; }
            public bool Required { get; }
            public ActorDefinitionAsset ActorDefinition { get; }
            public GameObject Prefab => ActorDefinition != null ? ActorDefinition.PrefabReference : null;
            public ActorPlacementMode PlacementMode => ActorDefinition != null ? ActorDefinition.PlacementMode : ActorPlacementMode.None;
            public string PlacementId => ActorDefinition != null ? ActorDefinition.PlacementKey : string.Empty;
            public Vector3 LocalPosition => ActorDefinition != null ? ActorDefinition.LocalPosition : Vector3.zero;
            public Vector3 LocalRotation => ActorDefinition != null ? ActorDefinition.LocalRotation : Vector3.zero;
            public bool IsValid =>
                PlayerSlotId.IsValid &&
                PlayerSelectionId.IsValid &&
                ActorDefinitionId.IsValid &&
                ActorId.IsValid &&
                ActorDefinition != null;
        }

        [Serializable]
        public struct Entry
        {
            [SerializeField] private string playerSlotId;
            [SerializeField] private string playerSelectionId;
            [SerializeField] private ActorDefinitionAsset actorDefinition;
            [SerializeField] private bool required;

            public PlayerSlotId PlayerSlotId => new(Normalize(playerSlotId));
            public PlayerSelectionId PlayerSelectionId => new(Normalize(playerSelectionId));
            public ActorDefinitionAsset ActorDefinition => actorDefinition;
            public ActorDefinitionId ActorDefinitionId => new(actorDefinition != null ? actorDefinition.ActorDefinitionId : string.Empty);
            public ActorId ActorId => new(actorDefinition != null ? actorDefinition.ActorId : string.Empty);
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

            HashSet<string> slotDedupe = new(StringComparer.Ordinal);
            HashSet<string> selectionDedupe = new(StringComparer.Ordinal);
            HashSet<string> actorDedupe = new(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                PlayerSlotId playerSlotId = entries[i].PlayerSlotId;
                if (!playerSlotId.IsValid)
                {
                    errorMessage = $"entries[{i}].playerSlotId is required.";
                    return false;
                }

                PlayerSelectionId playerSelectionId = entries[i].PlayerSelectionId;
                if (!playerSelectionId.IsValid)
                {
                    errorMessage = $"entries[{i}].playerSelectionId is required playerSlotId='{playerSlotId}'.";
                    return false;
                }

                ActorDefinitionAsset actorDefinition = entries[i].ActorDefinition;
                if (actorDefinition == null)
                {
                    errorMessage = $"entries[{i}].actorDefinition is required playerSlotId='{playerSlotId}'.";
                    return false;
                }

                if (!actorDefinition.TryValidate(out string actorDefinitionValidationError))
                {
                    errorMessage = $"entries[{i}].actorDefinition is invalid asset='{actorDefinition.name}' detail='{actorDefinitionValidationError}'.";
                    return false;
                }

                if (actorDefinition.ActorKind != ActorDefinitionKind.Player)
                {
                    errorMessage = $"entries[{i}].actorDefinition.actorKind must be Player for PlayerParticipation seed. playerSlotId='{playerSlotId}' actorDefinitionId='{actorDefinition.ActorDefinitionId}' actorId='{actorDefinition.ActorId}' actorKind='{actorDefinition.ActorKind}'.";
                    return false;
                }

                ActorDefinitionId actorDefinitionId = entries[i].ActorDefinitionId;
                ActorId actorId = entries[i].ActorId;
                if (!actorDefinitionId.IsValid)
                {
                    errorMessage = $"entries[{i}].actorDefinition.actorDefinitionId is required playerSlotId='{playerSlotId}'.";
                    return false;
                }

                if (!actorId.IsValid)
                {
                    errorMessage = $"entries[{i}].actorDefinition.actorId is required playerSlotId='{playerSlotId}' actorDefinitionId='{actorDefinitionId}'.";
                    return false;
                }

                if (string.Equals(playerSlotId.Value, actorDefinitionId.Value, StringComparison.Ordinal) ||
                    string.Equals(playerSlotId.Value, actorId.Value, StringComparison.Ordinal) ||
                    string.Equals(playerSelectionId.Value, actorDefinitionId.Value, StringComparison.Ordinal) ||
                    string.Equals(playerSelectionId.Value, actorId.Value, StringComparison.Ordinal) ||
                    string.Equals(actorDefinitionId.Value, actorId.Value, StringComparison.Ordinal))
                {
                    errorMessage = $"entries[{i}] ids must be domain-separated playerSlotId='{playerSlotId}' playerSelectionId='{playerSelectionId}' actorDefinitionId='{actorDefinitionId}' actorId='{actorId}'.";
                    return false;
                }

                if (!slotDedupe.Add(playerSlotId.Value))
                {
                    errorMessage = $"entries contains duplicate playerSlotId='{playerSlotId}'.";
                    return false;
                }

                if (!selectionDedupe.Add(playerSelectionId.Value))
                {
                    errorMessage = $"entries contains duplicate playerSelectionId='{playerSelectionId}'.";
                    return false;
                }

                if (!actorDedupe.Add(actorId.Value))
                {
                    errorMessage = $"entries contains duplicate actorId='{actorId}'.";
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
                    entries[i].PlayerSlotId,
                    entries[i].PlayerSelectionId,
                    entries[i].ActorDefinitionId,
                    entries[i].ActorId,
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
                    entries[i].PlayerSlotId,
                    entries[i].PlayerSelectionId,
                    entries[i].ActorDefinitionId,
                    entries[i].ActorId,
                    entries[i].Required,
                    entries[i].ActorDefinition));
            }

            return resolvedEntries;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
