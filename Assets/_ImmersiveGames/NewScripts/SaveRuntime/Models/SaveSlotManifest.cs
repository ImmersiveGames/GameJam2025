using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public sealed class SaveSlotManifest
    {
        public SaveSlotManifest(
            string profileId,
            SaveSlotId currentSlotId,
            IReadOnlyList<SaveSlotDescriptor> slots)
        {
            ProfileId = NormalizeRequired(profileId, nameof(profileId));
            CurrentSlotId = currentSlotId;
            Slots = slots ?? Array.Empty<SaveSlotDescriptor>();
        }

        public string ProfileId { get; }
        public SaveSlotId CurrentSlotId { get; }
        public IReadOnlyList<SaveSlotDescriptor> Slots { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ProfileId) && CurrentSlotId.IsValid;

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
