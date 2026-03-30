using System;
namespace _ImmersiveGames.NewScripts.Experience.Save.Models
{
    public sealed class SaveIdentity
    {
        public SaveIdentity(string profileId, string slotId)
        {
            ProfileId = NormalizeIdentity(profileId, nameof(profileId));
            SlotId = NormalizeIdentity(slotId, nameof(slotId));
        }

        public string ProfileId { get; }

        public string SlotId { get; }

        public override string ToString()
        {
            return $"profile='{ProfileId}' slot='{SlotId}'";
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
