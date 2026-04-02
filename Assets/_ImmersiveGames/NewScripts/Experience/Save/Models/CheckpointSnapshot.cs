using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.Experience.Save.Models
{
    public sealed class CheckpointSnapshot
    {
        public CheckpointSnapshot(
            CheckpointIdentity identity,
            IReadOnlyDictionary<string, string> entries,
            int revision = 0)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));

            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), revision, "Revision must be non-negative.");
            }

            Revision = revision;
        }

        public CheckpointIdentity Identity { get; }

        public string CheckpointId => Identity.CheckpointId;

        public string ProfileId => Identity.ProfileId;

        public string SlotId => Identity.SlotId;

        public IReadOnlyDictionary<string, string> Entries { get; }

        public int Revision { get; }

        public override string ToString()
        {
            return $"checkpointId='{CheckpointId}' profileId='{ProfileId}' slotId='{SlotId}' revision={Revision} entries={Entries.Count}";
        }
    }
}
