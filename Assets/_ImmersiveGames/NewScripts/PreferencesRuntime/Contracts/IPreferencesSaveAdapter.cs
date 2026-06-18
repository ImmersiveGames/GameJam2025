namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts
{
    public interface IPreferencesSaveAdapter
    {
        bool TryLoadAudio(
            string profileId,
            string slotId,
            out AudioPreferencesSnapshot snapshot,
            out string reason);

        bool TryLoadVideo(
            string profileId,
            string slotId,
            out VideoPreferencesSnapshot snapshot,
            out string reason);

        bool TrySaveAudio(
            AudioPreferencesSnapshot snapshot,
            out string reason);

        bool TrySaveVideo(
            VideoPreferencesSnapshot snapshot,
            out string reason);
    }
}
