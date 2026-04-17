using System;
using System.Collections.Generic;
namespace ImmersiveGames.GameJam2025.Experience.Save.Models
{
    public sealed class ProgressionSnapshot
    {
        public const string BootstrapProfileId = "default";
        public const string BootstrapSlotId = "progression";

        public ProgressionSnapshot(
            string profileId,
            string slotId,
            IReadOnlyDictionary<string, string> entries,
            int revision = 0)
        {
            ProfileId = NormalizeIdentity(profileId, nameof(profileId));
            SlotId = NormalizeIdentity(slotId, nameof(slotId));
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));

            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), revision, "Revision must be non-negative.");
            }

            Revision = revision;
        }

        public string ProfileId { get; }

        public string SlotId { get; }

        public IReadOnlyDictionary<string, string> Entries { get; }

        public int Revision { get; }

        public override string ToString()
        {
            return $"profile='{ProfileId}' slot='{SlotId}' revision={Revision} entries={Entries.Count}";
        }

        private static string NormalizeIdentity(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identity is required.", paramName);
            }

            return value.Trim();
        }
    }
}

