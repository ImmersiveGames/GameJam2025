using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class SaveRecord
    {
        public SaveRecord(
            SaveIdentity identity,
            int schemaVersion,
            long revision,
            string savedAtUtc,
            Dictionary<string, string> entries)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));

            if (schemaVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), schemaVersion, "schemaVersion must be greater than zero.");
            }

            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), revision, "revision must be non-negative.");
            }

            SchemaVersion = schemaVersion;
            Revision = revision;
            SavedAtUtc = NormalizeTimestamp(savedAtUtc);
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public SaveIdentity Identity { get; }

        public int SchemaVersion { get; }

        public long Revision { get; }

        public string SavedAtUtc { get; }

        public Dictionary<string, string> Entries { get; }

        public bool IsValid =>
            Identity != null &&
            SchemaVersion > 0 &&
            Revision >= 0 &&
            !string.IsNullOrWhiteSpace(SavedAtUtc) &&
            Entries != null;

        public override string ToString()
        {
            return $"identity={Identity} schemaVersion={SchemaVersion} revision={Revision} savedAtUtc='{SavedAtUtc}' entries={Entries.Count}";
        }

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
