namespace _ImmersiveGames.NewScripts.Modules.Preferences.Contracts
{
    public interface IPreferencesSaveService
    {
        string BackendId { get; }
        bool IsBackendAvailable { get; }

        bool TryLoad(
            string profileId,
            string slotId,
            out AudioPreferencesSnapshot snapshot,
            out string reason);

        bool TrySaveCurrent(out string reason);

        bool TrySave(
            AudioPreferencesSnapshot snapshot,
            out string reason);

        bool TryPreviewAudioVolumes(
            float masterVolume,
            float bgmVolume,
            float sfxVolume,
            string reason,
            out bool changed);

        bool TryCommitCurrentAudioVolumes(
            string reason,
            string fieldHint,
            out bool changed,
            out string saveReason);
    }
}
