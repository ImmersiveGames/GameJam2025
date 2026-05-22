namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class SaveSlotDescriptor
    {
        public SaveSlotDescriptor(
            SaveSlotId slotId,
            SaveSlotKind slotKind,
            string label,
            bool isCurrent)
        {
            SlotId = slotId;
            SlotKind = slotKind;
            Label = Normalize(label);
            IsCurrent = isCurrent;
        }

        public SaveSlotId SlotId { get; }
        public SaveSlotKind SlotKind { get; }
        public string Label { get; }
        public bool IsCurrent { get; }

        public bool IsValid => SlotId.IsValid && SlotKind != SaveSlotKind.Unknown;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
