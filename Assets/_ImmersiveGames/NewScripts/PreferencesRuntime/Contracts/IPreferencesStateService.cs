using System.Collections.Generic;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Config;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts
{
    public interface IPreferencesStateService
    {
        bool HasSnapshot { get; }
        AudioPreferencesSnapshot CurrentSnapshot { get; }
        bool HasVideoSnapshot { get; }
        VideoPreferencesSnapshot CurrentVideoSnapshot { get; }
        IReadOnlyList<Vector2Int> GetVideoResolutionPresets();
        VideoDefaultsAsset VideoDefaults { get; }

        void SetCurrent(
            AudioPreferencesSnapshot snapshot,
            string reason);

        void SetCurrent(
            VideoPreferencesSnapshot snapshot,
            string reason);

        void ApplyTo(
            IAudioSettingsService audioSettings,
            string reason);

        void ApplyCurrentVideoToRuntime(
            string reason);
    }
}

