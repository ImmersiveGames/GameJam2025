using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class SaveRequest
    {
        public SaveRequest(
            SaveAddress address,
            IReadOnlyDictionary<string, string> entries,
            long revision,
            string savedAtUtc)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));

            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), revision, "revision must be non-negative.");
            }

            Revision = revision;
            SavedAtUtc = NormalizeTimestamp(savedAtUtc);
        }

        public SaveAddress Address { get; }
        public IReadOnlyDictionary<string, string> Entries { get; }
        public long Revision { get; }
        public string SavedAtUtc { get; }

        private static string NormalizeTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.UtcNow.ToString("O");
            }

            return value.Trim();
        }
    }
}

