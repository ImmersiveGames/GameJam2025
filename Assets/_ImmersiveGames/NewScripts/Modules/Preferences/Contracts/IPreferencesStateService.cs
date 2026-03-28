using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Preferences.Contracts
{
    public interface IPreferencesStateService
    {
        bool HasSnapshot { get; }
        AudioPreferencesSnapshot CurrentSnapshot { get; }

        void SetCurrent(
            AudioPreferencesSnapshot snapshot,
            string reason);

        void ApplyTo(
            IAudioSettingsService audioSettings,
            string reason);
    }
}
