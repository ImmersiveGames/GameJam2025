using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class ProgressionSnapshotEnvelope
    {
        public ProgressionSnapshotEnvelope(
            ProgressionSlotContext slotContext,
            SaveSnapshotHeader header,
            IReadOnlyDictionary<string, string> entries)
        {
            SlotContext = slotContext ?? throw new ArgumentNullException(nameof(slotContext));
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public ProgressionSlotContext SlotContext { get; }
        public SaveSnapshotHeader Header { get; }
        public IReadOnlyDictionary<string, string> Entries { get; }

        public bool IsValid =>
            SlotContext.IsValid &&
            Header.IsValid &&
            Entries != null;
    }
}
