using System;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class SaveCurrentState
    {
        public SaveCurrentState(
            string profileId,
            string slotId,
            int schemaVersion,
            long revision,
            string savedAtUtc,
            string currentSnapshotId = "")
        {
            ProfileId = NormalizeRequired(profileId, nameof(profileId));
            SlotId = NormalizeRequired(slotId, nameof(slotId));

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
            CurrentSnapshotId = NormalizeOptional(currentSnapshotId);
        }

        public string ProfileId { get; }
        public string SlotId { get; }
        public int SchemaVersion { get; }
        public long Revision { get; }
        public string SavedAtUtc { get; }
        public string CurrentSnapshotId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProfileId) &&
            !string.IsNullOrWhiteSpace(SlotId) &&
            SchemaVersion > 0 &&
            Revision >= 0 &&
            !string.IsNullOrWhiteSpace(SavedAtUtc);

        public override string ToString()
        {
            return $"profileId='{ProfileId}' slotId='{SlotId}' schemaVersion='{SchemaVersion}' revision='{Revision}' currentSnapshotId='{CurrentSnapshotId}' savedAtUtc='{SavedAtUtc}'";
        }

        private static string NormalizeRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", paramName);
            }

            return value.Trim();
        }

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
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
