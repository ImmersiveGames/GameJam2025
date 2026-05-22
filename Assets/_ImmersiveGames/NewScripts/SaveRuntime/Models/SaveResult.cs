using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public enum SaveResultKind
    {
        None = 0,
        Saved = 1,
        Loaded = 2,
        Skipped = 3,
        Failed = 4,
        Deleted = 5,
    }

    public sealed class SaveResult
    {
        public SaveResult(
            SaveResultKind kind,
            string reason,
            IReadOnlyDictionary<string, string> entries = null,
            int schemaVersion = 0,
            long revision = 0,
            string savedAtUtc = null)
        {
            Kind = kind;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Entries = CloneEntries(entries);
            SchemaVersion = schemaVersion;
            Revision = revision;
            SavedAtUtc = string.IsNullOrWhiteSpace(savedAtUtc) ? string.Empty : savedAtUtc.Trim();
        }

        public SaveResultKind Kind { get; }
        public string Reason { get; }
        public IReadOnlyDictionary<string, string> Entries { get; }
        public int SchemaVersion { get; }
        public long Revision { get; }
        public string SavedAtUtc { get; }

        public bool HasEntries => Entries != null && Entries.Count > 0;

        public bool IsSuccess =>
            Kind == SaveResultKind.Saved ||
            Kind == SaveResultKind.Loaded ||
            Kind == SaveResultKind.Deleted;

        private static IReadOnlyDictionary<string, string> CloneEntries(IReadOnlyDictionary<string, string> entries)
        {
            if (entries == null)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            Dictionary<string, string> clone = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in entries)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                clone[pair.Key.Trim()] = pair.Value ?? string.Empty;
            }

            return clone;
        }
    }
}
