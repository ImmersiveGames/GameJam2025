using System;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts
{
    public sealed class VideoPreferencesSnapshot
    {
        public const string BootstrapProfileId = "default";
        public const string BootstrapSlotId = "video";

        public VideoPreferencesSnapshot(
            string profileId,
            string slotId,
            int resolutionWidth,
            int resolutionHeight,
            bool fullscreen)
        {
            ProfileId = NormalizeIdentity(profileId, nameof(profileId));
            SlotId = NormalizeIdentity(slotId, nameof(slotId));
            if (resolutionWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolutionWidth), resolutionWidth, "Resolution width must be positive.");
            }

            if (resolutionHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolutionHeight), resolutionHeight, "Resolution height must be positive.");
            }

            ResolutionWidth = resolutionWidth;
            ResolutionHeight = resolutionHeight;
            Fullscreen = fullscreen;
        }

        public string ProfileId { get; }
        public string SlotId { get; }
        public int ResolutionWidth { get; }
        public int ResolutionHeight { get; }
        public bool Fullscreen { get; }

        public override string ToString()
        {
            return $"profile='{ProfileId}' slot='{SlotId}' resolution={ResolutionWidth}x{ResolutionHeight} fullscreen={Fullscreen}";
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

