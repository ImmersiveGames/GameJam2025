using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Config;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Runtime
{
    public sealed class PreferencesService : IPreferencesStateService
    {
        private readonly IAudioSettingsService _audioSettings;
        private readonly AudioDefaultsAsset _audioDefaults;
        private readonly VideoDefaultsAsset _videoDefaults;
        private AudioPreferencesSnapshot _currentSnapshot;
        private VideoPreferencesSnapshot _currentVideoSnapshot;

        public PreferencesService(
            IAudioSettingsService audioSettings,
            AudioDefaultsAsset audioDefaults,
            VideoDefaultsAsset videoDefaults)
        {
            _audioSettings = audioSettings ?? throw new ArgumentNullException(nameof(audioSettings));
            _audioDefaults = audioDefaults ?? throw new ArgumentNullException(nameof(audioDefaults));
            _videoDefaults = videoDefaults ?? throw new ArgumentNullException(nameof(videoDefaults));
        }

        public bool HasSnapshot => _currentSnapshot != null;
        public bool HasVideoSnapshot => _currentVideoSnapshot != null;
        public AudioDefaultsAsset AudioDefaults => _audioDefaults;
        public VideoDefaultsAsset VideoDefaults => _videoDefaults;

        public AudioPreferencesSnapshot CurrentSnapshot =>
            _currentSnapshot ?? throw new InvalidOperationException("[FATAL][Preferences] Current audio snapshot requested before initialization.");

        public VideoPreferencesSnapshot CurrentVideoSnapshot =>
            _currentVideoSnapshot ?? throw new InvalidOperationException("[FATAL][Preferences] Current video snapshot requested before initialization.");

        public void SetCurrent(AudioPreferencesSnapshot snapshot, string reason)
        {
            _currentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] audio snapshot set. reason='{reason.TrimToOrDefault("Preferences/Unknown")}' snapshot={snapshot}.",
                DebugUtility.Colors.Info);
        }

        public void SetCurrent(VideoPreferencesSnapshot snapshot, string reason)
        {
            _currentVideoSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] video snapshot set. reason='{reason.TrimToOrDefault("Preferences/Unknown")}' snapshot={snapshot}.",
                DebugUtility.Colors.Info);
        }

        public void ApplyTo(IAudioSettingsService audioSettings, string reason)
        {
            if (audioSettings == null)
            {
                throw new ArgumentNullException(nameof(audioSettings));
            }

            if (!HasSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Apply requested before any audio snapshot was seeded.");
            }

            CurrentSnapshot.ApplyTo(audioSettings);

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] audio snapshot applied to runtime. reason='{reason.TrimToOrDefault("Preferences/Unknown")}' snapshot={CurrentSnapshot}.",
                DebugUtility.Colors.Info);
        }

        public void ApplyCurrentVideoToRuntime(string reason)
        {
            if (!HasVideoSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Apply requested before any video snapshot was seeded.");
            }

            var snapshot = CurrentVideoSnapshot;
            var mode = snapshot.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(snapshot.ResolutionWidth, snapshot.ResolutionHeight, mode);

            DebugUtility.Log(typeof(PreferencesService),
                $"[Preferences] video runtime apply resolution={snapshot.ResolutionWidth}x{snapshot.ResolutionHeight} mode='{mode}' reason='{reason.TrimToOrDefault("Preferences/Unknown")}'.",
                DebugUtility.Colors.Info);
        }

        public bool TryPreviewAudioVolumes(
            float masterVolume,
            float bgmVolume,
            float sfxVolume,
            string reason,
            out bool changed)
        {
            if (!HasSnapshot)
            {
                changed = false;
                throw new InvalidOperationException("[FATAL][Preferences] Audio volume update requested before any snapshot was seeded.");
            }

            var nextSnapshot = new AudioPreferencesSnapshot(
                CurrentSnapshot.ProfileId,
                CurrentSnapshot.SlotId,
                masterVolume,
                bgmVolume,
                sfxVolume);

            if (HasSameAudioValues(CurrentSnapshot, nextSnapshot))
            {
                changed = false;
                return false;
            }

            _currentSnapshot = nextSnapshot;
            changed = true;
            CurrentSnapshot.ApplyTo(_audioSettings);
            return true;
        }

        public bool TryPreviewVideoResolution(
            int width,
            int height,
            bool fullscreen,
            string reason,
            out bool changed)
        {
            if (!HasVideoSnapshot)
            {
                changed = false;
                throw new InvalidOperationException("[FATAL][Preferences] Video resolution update requested before any snapshot was seeded.");
            }

            var nextSnapshot = ResolveSupportedVideoSnapshot(
                CurrentVideoSnapshot.ProfileId,
                CurrentVideoSnapshot.SlotId,
                width,
                height,
                fullscreen,
                reason);

            if (HasSameVideoValues(CurrentVideoSnapshot, nextSnapshot))
            {
                changed = false;
                return false;
            }

            _currentVideoSnapshot = nextSnapshot;
            changed = true;
            ApplyCurrentVideoToRuntime(reason);
            return true;
        }

        public IReadOnlyList<Vector2Int> GetVideoResolutionPresets()
        {
            return BuildSupportedVideoResolutionPresets();
        }

        private VideoPreferencesSnapshot ResolveSupportedVideoSnapshot(
            string profileId,
            string slotId,
            int width,
            int height,
            bool fullscreen,
            string reason)
        {
            Vector2Int requested = new(width, height);
            if (IsSupportedVideoResolution(requested))
            {
                return new VideoPreferencesSnapshot(profileId, slotId, width, height, fullscreen);
            }

            var resolved = ResolveFallbackVideoResolution();
            DebugUtility.Log<PreferencesService>(
                $"[Preferences] video resolution normalized. reason='{reason.TrimToOrDefault("Preferences/Unknown")}' requested={width}x{height} selected={resolved.x}x{resolved.y}.",
                DebugUtility.Colors.Info);

            return new VideoPreferencesSnapshot(profileId, slotId, resolved.x, resolved.y, fullscreen);
        }

        private IReadOnlyList<Vector2Int> BuildSupportedVideoResolutionPresets()
        {
            var supported = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();

            foreach (var preset in _videoDefaults.ResolutionPresets)
            {
                if (!IsSupportedVideoResolution(preset))
                {
                    continue;
                }

                if (seen.Add(preset))
                {
                    supported.Add(preset);
                }
            }

            if (supported.Count == 0)
            {
                supported.Add(ResolveFallbackVideoResolution());
            }

            return supported;
        }

        private Vector2Int ResolveFallbackVideoResolution()
        {
            var defaultPreset = new Vector2Int(
                _videoDefaults.DefaultResolutionWidth,
                _videoDefaults.DefaultResolutionHeight);

            if (IsSupportedVideoResolution(defaultPreset))
            {
                return defaultPreset;
            }

            IReadOnlyList<Vector2Int> supported = BuildSupportedVideoResolutionPresetsInternal();
            if (supported.Count > 0)
            {
                return supported[0];
            }

            var current = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
            if (current is { x: > 0, y: > 0 })
            {
                return current;
            }

            return defaultPreset;
        }

        private IReadOnlyList<Vector2Int> BuildSupportedVideoResolutionPresetsInternal()
        {
            var supported = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();

            foreach (var preset in _videoDefaults.ResolutionPresets)
            {
                if (!IsSupportedVideoResolution(preset))
                {
                    continue;
                }

                if (seen.Add(preset))
                {
                    supported.Add(preset);
                }
            }

            return supported;
        }

        private static bool IsSupportedVideoResolution(Vector2Int preset)
        {
            if (preset.x <= 0 || preset.y <= 0)
            {
                return false;
            }

            Resolution[] resolutions = Screen.resolutions;
            for (int i = 0; i < resolutions.Length; i++)
            {
                var resolution = resolutions[i];
                if (resolution.width == preset.x && resolution.height == preset.y)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSameAudioValues(AudioPreferencesSnapshot left, AudioPreferencesSnapshot right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return string.Equals(left.ProfileId, right.ProfileId, StringComparison.Ordinal)
                && string.Equals(left.SlotId, right.SlotId, StringComparison.Ordinal)
                && Mathf.Approximately(left.MasterVolume, right.MasterVolume)
                && Mathf.Approximately(left.BgmVolume, right.BgmVolume)
                && Mathf.Approximately(left.SfxVolume, right.SfxVolume);
        }

        private static bool HasSameVideoValues(VideoPreferencesSnapshot left, VideoPreferencesSnapshot right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return string.Equals(left.ProfileId, right.ProfileId, StringComparison.Ordinal)
                && string.Equals(left.SlotId, right.SlotId, StringComparison.Ordinal)
                && left.ResolutionWidth == right.ResolutionWidth
                && left.ResolutionHeight == right.ResolutionHeight
                && left.Fullscreen == right.Fullscreen;
        }
    }
}
