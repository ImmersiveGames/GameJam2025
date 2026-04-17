namespace ImmersiveGames.GameJam2025.Experience.Preferences.Contracts
{
    public interface IPreferencesBackend
    {
        string BackendId { get; }
        bool IsAvailable { get; }

        bool TryLoad(
            string profileId,
            string slotId,
            out AudioPreferencesSnapshot snapshot,
            out string reason);

        bool TrySave(
            AudioPreferencesSnapshot snapshot,
            out string reason);

        bool TryLoadVideo(
            string profileId,
            string slotId,
            out VideoPreferencesSnapshot snapshot,
            out string reason);

        bool TrySaveVideo(
            VideoPreferencesSnapshot snapshot,
            out string reason);
    }
}

