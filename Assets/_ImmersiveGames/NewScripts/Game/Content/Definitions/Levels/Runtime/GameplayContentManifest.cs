using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime
{
    [Serializable]
    public sealed class GameplayContentManifest
    {
        [SerializeField] private List<GameplayContentEntry> entries = new();

        public IReadOnlyList<GameplayContentEntry> Entries => entries;

        public bool TryValidateRuntime(out string error)
        {
            error = string.Empty;

            if (entries == null)
            {
                error = "Gameplay content entries list is null.";
                return false;
            }

            HashSet<string> seenIds = new(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                GameplayContentEntry entry = entries[i];
                if (entry == null)
                {
                    error = $"Gameplay content entry is null at index={i}.";
                    return false;
                }

                if (!entry.TryValidateRuntime(out string entryError))
                {
                    error = $"Gameplay content entry invalid at index={i}. detail='{entryError}'";
                    return false;
                }

                string entryId = entry.EntryId;
                if (!seenIds.Add(entryId))
                {
                    error = $"Duplicate gameplay content entry id '{entryId}' at index={i}.";
                    return false;
                }
            }

            return true;
        }

        public void ValidateOrFailFast(string context, string ownerLabel)
        {
            if (TryValidateRuntime(out string error))
            {
                return;
            }

            string normalizedOwnerLabel = string.IsNullOrWhiteSpace(ownerLabel) ? "<unnamed-level>" : ownerLabel.Trim();
            HardFailFastH1.Trigger(typeof(GameplayContentManifest),
                $"[FATAL][H1][LevelFlow] Invalid GameplayContentManifest '{normalizedOwnerLabel}'. context='{context}' detail='{error}'");
        }
    }
}
