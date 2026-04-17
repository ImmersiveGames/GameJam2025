using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime;
using ImmersiveGames.GameJam2025.Experience.Preferences.Config;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Experience.Preferences.Contracts
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

