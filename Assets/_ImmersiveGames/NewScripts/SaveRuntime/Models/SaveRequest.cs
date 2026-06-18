using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class SaveRequest
    {
        public SaveRequest(
            SaveAddress address,
            IReadOnlyDictionary<string, string> entries,
            long revision,
            string savedAtUtc,
            string profileId = null)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));

            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), revision, "revision must be non-negative.");
            }

            Revision = revision;
            SavedAtUtc = NormalizeTimestamp(savedAtUtc);
            ProfileId = NormalizeOptional(profileId);
        }

        public SaveAddress Address { get; }
        public IReadOnlyDictionary<string, string> Entries { get; }
        public long Revision { get; }
        public string SavedAtUtc { get; }
        public string ProfileId { get; }

        private static string NormalizeTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.UtcNow.ToString("O");
            }

            return value.Trim();
        }

        private static string NormalizeOptional(string value)
        {
            return value.TrimToEmpty();
        }
    }
}
