using System;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class CheckpointIdentity
    {
        public const string BootstrapCheckpointId = "checkpoint";
        public const string BootstrapProfileId = "default";
        public const string BootstrapSlotId = "checkpoint";

        public CheckpointIdentity(string checkpointId, string profileId, string slotId)
        {
            CheckpointId = NormalizeIdentity(checkpointId, nameof(checkpointId));
            ProfileId = NormalizeIdentity(profileId, nameof(profileId));
            SlotId = NormalizeIdentity(slotId, nameof(slotId));
        }

        public string CheckpointId { get; }

        public string ProfileId { get; }

        public string SlotId { get; }

        public override string ToString()
        {
            return $"checkpointId='{CheckpointId}' profileId='{ProfileId}' slotId='{SlotId}'";
        }

        private static string NormalizeIdentity(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identity is required.", paramName);
            }

            return value.Trim();
        }
    }
}

