using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [CreateAssetMenu(
        fileName = "ActivityPauseContentProfile",
        menuName = "ImmersiveGames/SessionActivity/Activity Pause Content Profile",
        order = 55)]
    public sealed class ActivityPauseContentProfileAsset : ScriptableObject
    {
        [SerializeField] private string profileId = "activity.pause.content.default";
        [SerializeField] private List<ActivityPauseContentEntry> entries = new();

        public string ProfileId => profileId.TrimToEmpty();
        public IReadOnlyList<ActivityPauseContentEntry> Entries => entries;
        public bool HasEntries => entries != null && entries.Count > 0;

        public ActivityPauseContentProfile ToProfile()
        {
            ValidateOrThrow($"ActivityPauseContentProfileAsset:{name}");

            List<ActivityPauseContentContribution> contributions = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                contributions.Add(new ActivityPauseContentContribution(
                    new ActivityPauseContentSlotId(entry.SlotId),
                    entry.Prefab,
                    entry.Required));
            }

            return new ActivityPauseContentProfile(ProfileId, contributions);
        }

        public void ValidateOrThrow(string owner)
        {
            string normalizedOwner = owner.TrimToEmpty();
            if (string.IsNullOrWhiteSpace(normalizedOwner))
            {
                normalizedOwner = name;
            }

            if (string.IsNullOrWhiteSpace(ProfileId))
            {
                throw new InvalidOperationException($"{normalizedOwner} requires profileId.");
            }

            if (entries == null || entries.Count == 0)
            {
                throw new InvalidOperationException($"{normalizedOwner} requires at least one pause content entry.");
            }

            HashSet<string> slotIds = new(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    throw new InvalidOperationException($"{normalizedOwner} has null pause content entry at index '{i}'.");
                }

                entry.ValidateOrThrow(normalizedOwner, i);
                if (!slotIds.Add(entry.SlotId))
                {
                    throw new InvalidOperationException($"{normalizedOwner} has duplicate pause content slotId='{entry.SlotId}'.");
                }
            }
        }
    }

    [Serializable]
    public sealed class ActivityPauseContentEntry
    {
        [SerializeField] private string slotId = "pause.activity.content.root";
        [SerializeField] private GameObject prefab;
        [SerializeField] private bool required = true;

        public string SlotId => slotId.TrimToEmpty();
        public GameObject Prefab => prefab;
        public bool Required => required;

        public void ValidateOrThrow(string owner, int index)
        {
            if (string.IsNullOrWhiteSpace(SlotId))
            {
                throw new InvalidOperationException($"{owner} pause content entry index='{index}' requires slotId.");
            }

            if (prefab == null)
            {
                throw new InvalidOperationException($"{owner} pause content entry slotId='{SlotId}' requires prefab.");
            }
        }
    }
}
