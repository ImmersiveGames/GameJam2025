using System;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class SaveSnapshotHeader
    {
        public SaveSnapshotHeader(
            SaveSnapshotId snapshotId,
            string schemaId,
            int schemaVersion,
            long revision,
            string savedAtUtc)
        {
            SnapshotId = snapshotId;
            SchemaId = NormalizeRequired(schemaId, nameof(schemaId));

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
            SavedAtUtc = NormalizeRequired(savedAtUtc, nameof(savedAtUtc));
        }

        public SaveSnapshotId SnapshotId { get; }
        public string SchemaId { get; }
        public int SchemaVersion { get; }
        public long Revision { get; }
        public string SavedAtUtc { get; }

        public bool IsValid =>
            SnapshotId.IsValid &&
            !string.IsNullOrWhiteSpace(SchemaId) &&
            SchemaVersion > 0 &&
            Revision >= 0 &&
            !string.IsNullOrWhiteSpace(SavedAtUtc);

        private static string NormalizeRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", paramName);
            }

            return value.Trim();
        }
    }
}
