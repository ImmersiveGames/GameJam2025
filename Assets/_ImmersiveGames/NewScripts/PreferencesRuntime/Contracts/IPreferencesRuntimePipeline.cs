namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts
{
    public interface IPreferencesRuntimePipeline
    {
        bool RequestBootstrapLoadAudio(string reason);

        bool RequestBootstrapLoadVideo(string reason);

        bool RequestAudioPreview(
            float masterVolume,
            float bgmVolume,
            float sfxVolume,
            string reason);

        bool RequestAudioCommit(
            string fieldHint,
            string reason);

        bool RequestAudioRestoreDefaults(string reason);

        bool RequestVideoPreview(
            int width,
            int height,
            bool fullscreen,
            string reason);

        bool RequestVideoCommit(
            string fieldHint,
            string reason);

        bool RequestVideoRestoreDefaults(string reason);
    }
}
