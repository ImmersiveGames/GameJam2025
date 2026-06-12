using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Runtime
{
    public sealed class PreferencesRuntimePipeline : IPreferencesRuntimePipeline
    {
        private readonly IPreferencesStateService _stateService;
        private readonly IPreferencesSaveAdapter _saveAdapter;
        private AudioPreferencesSnapshot _lastCommittedAudioSnapshot;
        private VideoPreferencesSnapshot _lastCommittedVideoSnapshot;

        public PreferencesRuntimePipeline(
            IPreferencesStateService stateService,
            IPreferencesSaveAdapter saveAdapter)
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _saveAdapter = saveAdapter ?? throw new ArgumentNullException(nameof(saveAdapter));

            if (_stateService.HasSnapshot)
            {
                _lastCommittedAudioSnapshot = _stateService.CurrentSnapshot;
            }

            if (_stateService.HasVideoSnapshot)
            {
                _lastCommittedVideoSnapshot = _stateService.CurrentVideoSnapshot;
            }
        }

        public bool RequestBootstrapLoadAudio(string reason)
        {
            DebugUtility.Log(typeof(PreferencesRuntimePipeline),
                $"[OBS][Preferences][Pipeline] PreferencesAudioLoadRequested reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            bool loaded = _saveAdapter.TryLoadAudio(
                AudioPreferencesSnapshot.BootstrapProfileId,
                AudioPreferencesSnapshot.BootstrapSlotId,
                out var loadedSnapshot,
                out string loadReason);

            if (!loaded || loadedSnapshot == null)
            {
                DebugUtility.LogVerbose(typeof(PreferencesRuntimePipeline),
                    $"[Preferences] bootstrap kept installer seed for audio. reason='{Normalize(loadReason)}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            _stateService.SetCurrent(loadedSnapshot, "Preferences/BootstrapLoadAudio");
            _lastCommittedAudioSnapshot = loadedSnapshot;
            return true;
        }

        public bool RequestBootstrapLoadVideo(string reason)
        {
            DebugUtility.Log(typeof(PreferencesRuntimePipeline),
                $"[OBS][Preferences][Pipeline] PreferencesVideoLoadRequested reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            bool loaded = _saveAdapter.TryLoadVideo(
                VideoPreferencesSnapshot.BootstrapProfileId,
                VideoPreferencesSnapshot.BootstrapSlotId,
                out var loadedSnapshot,
                out string loadReason);

            if (!loaded || loadedSnapshot == null)
            {
                DebugUtility.LogVerbose(typeof(PreferencesRuntimePipeline),
                    $"[Preferences] bootstrap kept installer seed for video. reason='{Normalize(loadReason)}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            _stateService.SetCurrent(loadedSnapshot, "Preferences/BootstrapLoadVideo");
            _lastCommittedVideoSnapshot = loadedSnapshot;
            return true;
        }

        public bool RequestAudioPreview(float masterVolume, float bgmVolume, float sfxVolume, string reason)
        {
            DebugUtility.Log(typeof(PreferencesRuntimePipeline),
                $"[OBS][Preferences][Pipeline] PreferencesAudioPreviewRequested reason='{Normalize(reason)}' master={masterVolume:0.###} bgm={bgmVolume:0.###} sfx={sfxVolume:0.###}.",
                DebugUtility.Colors.Info);

            return _stateService.TryPreviewAudioVolumes(masterVolume, bgmVolume, sfxVolume, reason, out bool _);
        }

        public bool RequestAudioCommit(string fieldHint, string reason)
        {
            DebugUtility.Log(typeof(PreferencesRuntimePipeline),
                $"[OBS][Preferences][Pipeline] PreferencesAudioCommitRequested reason='{Normalize(reason)}' fieldHint='{Normalize(fieldHint)}'.",
                DebugUtility.Colors.Info);

            if (!_stateService.HasSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Audio commit requested before snapshot seed.");
            }

            var current = _stateService.CurrentSnapshot;
            if (HasSameAudioValues(current, _lastCommittedAudioSnapshot))
            {
                return true;
            }

            if (!_saveAdapter.TrySaveAudio(current, out string saveReason))
            {
                DebugUtility.LogWarning(typeof(PreferencesRuntimePipeline),
                    $"[Preferences] audio commit failed. reason='{Normalize(reason)}' saveReason='{Normalize(saveReason)}' snapshot={current}.");
                return false;
            }

            _lastCommittedAudioSnapshot = current;
            return true;
        }

        public bool RequestAudioRestoreDefaults(string reason)
        {
            DebugUtility.Log(typeof(PreferencesRuntimePipeline),
                $"[OBS][Preferences][Pipeline] PreferencesAudioRestoreDefaultsRequested reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            if (!_stateService.HasSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Audio restore defaults requested before snapshot seed.");
            }

            var current = _stateService.CurrentSnapshot;
            var restored = new AudioPreferencesSnapshot(
                current.ProfileId,
                current.SlotId,
                _stateService.AudioDefaults.MasterVolume,
                _stateService.AudioDefaults.BgmVolume,
                _stateService.AudioDefaults.SfxVolume);

            _stateService.SetCurrent(restored, "Preferences/AudioRestoreDefaults");
            _stateService.TryPreviewAudioVolumes(
                restored.MasterVolume,
                restored.BgmVolume,
                restored.SfxVolume,
                reason,
                out bool _);

            if (HasSameAudioValues(restored, _lastCommittedAudioSnapshot))
            {
                return true;
            }

            if (!_saveAdapter.TrySaveAudio(restored, out string saveReason))
            {
                DebugUtility.LogWarning(typeof(PreferencesRuntimePipeline),
                    $"[Preferences] audio restore defaults save failed. reason='{Normalize(reason)}' saveReason='{Normalize(saveReason)}' snapshot={restored}.");
                return false;
            }

            _lastCommittedAudioSnapshot = restored;
            return true;
        }

        public bool RequestVideoPreview(int width, int height, bool fullscreen, string reason)
        {
            DebugUtility.Log(typeof(PreferencesRuntimePipeline),
                $"[OBS][Preferences][Pipeline] PreferencesVideoPreviewRequested reason='{Normalize(reason)}' resolution={width}x{height} fullscreen={fullscreen}.",
                DebugUtility.Colors.Info);

            return _stateService.TryPreviewVideoResolution(width, height, fullscreen, reason, out bool _);
        }

        public bool RequestVideoCommit(string fieldHint, string reason)
        {
            DebugUtility.Log(typeof(PreferencesRuntimePipeline),
                $"[OBS][Preferences][Pipeline] PreferencesVideoCommitRequested reason='{Normalize(reason)}' fieldHint='{Normalize(fieldHint)}'.",
                DebugUtility.Colors.Info);

            if (!_stateService.HasVideoSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Video commit requested before snapshot seed.");
            }

            var current = _stateService.CurrentVideoSnapshot;
            if (HasSameVideoValues(current, _lastCommittedVideoSnapshot))
            {
                return true;
            }

            if (!_saveAdapter.TrySaveVideo(current, out string saveReason))
            {
                DebugUtility.LogWarning(typeof(PreferencesRuntimePipeline),
                    $"[Preferences] video commit failed. reason='{Normalize(reason)}' saveReason='{Normalize(saveReason)}' snapshot={current}.");
                return false;
            }

            _lastCommittedVideoSnapshot = current;
            return true;
        }

        public bool RequestVideoRestoreDefaults(string reason)
        {
            DebugUtility.Log(typeof(PreferencesRuntimePipeline),
                $"[OBS][Preferences][Pipeline] PreferencesVideoRestoreDefaultsRequested reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            if (!_stateService.HasVideoSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Video restore defaults requested before snapshot seed.");
            }

            var current = _stateService.CurrentVideoSnapshot;
            var restored = new VideoPreferencesSnapshot(
                current.ProfileId,
                current.SlotId,
                _stateService.VideoDefaults.DefaultResolutionWidth,
                _stateService.VideoDefaults.DefaultResolutionHeight,
                _stateService.VideoDefaults.DefaultFullscreen);

            _stateService.SetCurrent(restored, "Preferences/VideoRestoreDefaults");
            _stateService.ApplyCurrentVideoToRuntime(reason);

            if (HasSameVideoValues(restored, _lastCommittedVideoSnapshot))
            {
                return true;
            }

            if (!_saveAdapter.TrySaveVideo(restored, out string saveReason))
            {
                DebugUtility.LogWarning(typeof(PreferencesRuntimePipeline),
                    $"[Preferences] video restore defaults save failed. reason='{Normalize(reason)}' saveReason='{Normalize(saveReason)}' snapshot={restored}.");
                return false;
            }

            _lastCommittedVideoSnapshot = restored;
            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Preferences/Unknown" : value.Trim();
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
