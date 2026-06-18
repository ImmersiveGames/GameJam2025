using System;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class SaveAddress
    {
        public SaveAddress(
            SaveScope scope,
            SaveGroup group,
            string ownerId,
            string recordId,
            string slotId,
            string schemaId,
            int schemaVersion)
        {
            Scope = scope;
            Group = group;
            OwnerId = NormalizeRequired(ownerId, nameof(ownerId));
            RecordId = NormalizeRequired(recordId, nameof(recordId));
            SlotId = NormalizeRequired(slotId, nameof(slotId));
            SchemaId = NormalizeRequired(schemaId, nameof(schemaId));

            if (schemaVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), schemaVersion, "schemaVersion must be greater than zero.");
            }

            SchemaVersion = schemaVersion;
        }

        public SaveScope Scope { get; }
        public SaveGroup Group { get; }
        public string OwnerId { get; }
        public string RecordId { get; }
        public string SlotId { get; }
        public string SchemaId { get; }
        public int SchemaVersion { get; }

        public override string ToString()
        {
            return $"scope='{Scope}' group='{Group}' ownerId='{OwnerId}' recordId='{RecordId}' slotId='{SlotId}' schemaId='{SchemaId}' schemaVersion='{SchemaVersion}'";
        }

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
