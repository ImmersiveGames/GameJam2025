using System;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class ProgressionSlotContext
    {
        public ProgressionSlotContext(
            string profileId,
            SaveSlotId slotId,
            SaveSlotKind slotKind,
            SaveSnapshotId snapshotId)
        {
            ProfileId = NormalizeRequired(profileId, nameof(profileId));
            SlotId = slotId;
            SlotKind = slotKind;
            SnapshotId = snapshotId;
        }

        public string ProfileId { get; }
        public SaveSlotId SlotId { get; }
        public SaveSlotKind SlotKind { get; }
        public SaveSnapshotId SnapshotId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProfileId) &&
            SlotId.IsValid &&
            SlotKind != SaveSlotKind.Unknown &&
            SnapshotId.IsValid;

        public override string ToString()
        {
            return $"profileId='{ProfileId}' slotId='{SlotId}' slotKind='{SlotKind}' snapshotId='{SnapshotId}'";
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
