using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using _ImmersiveGames.NewScripts.Modules.Preferences.Config;
using _ImmersiveGames.NewScripts.Modules.Preferences.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Preferences.Runtime
{
    public sealed class PreferencesService : IPreferencesStateService, IPreferencesSaveService
    {
        private readonly IPreferencesBackend _backend;
        private readonly IAudioSettingsService _audioSettings;
        private readonly AudioDefaultsAsset _audioDefaults;
        private readonly VideoDefaultsAsset _videoDefaults;
        private AudioPreferencesSnapshot _currentSnapshot;
        private AudioPreferencesSnapshot _lastCommittedSnapshot;
        private VideoPreferencesSnapshot _currentVideoSnapshot;
        private VideoPreferencesSnapshot _lastCommittedVideoSnapshot;

        public PreferencesService(
            IPreferencesBackend backend,
            IAudioSettingsService audioSettings,
            AudioDefaultsAsset audioDefaults,
            VideoDefaultsAsset videoDefaults)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _audioSettings = audioSettings ?? throw new ArgumentNullException(nameof(audioSettings));
            _audioDefaults = audioDefaults ?? throw new ArgumentNullException(nameof(audioDefaults));
            _videoDefaults = videoDefaults ?? throw new ArgumentNullException(nameof(videoDefaults));
        }

        public string BackendId => _backend.BackendId;
        public bool IsBackendAvailable => _backend.IsAvailable;
        public bool HasSnapshot => _currentSnapshot != null;
        public bool HasVideoSnapshot => _currentVideoSnapshot != null;
        public VideoDefaultsAsset VideoDefaults => _videoDefaults;

        public AudioPreferencesSnapshot CurrentSnapshot =>
            _currentSnapshot ?? throw new InvalidOperationException("[FATAL][Preferences] Current audio snapshot requested before initialization.");

        public VideoPreferencesSnapshot CurrentVideoSnapshot =>
            _currentVideoSnapshot ?? throw new InvalidOperationException("[FATAL][Preferences] Current video snapshot requested before initialization.");

        public void SetCurrent(AudioPreferencesSnapshot snapshot, string reason)
        {
            _currentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _lastCommittedSnapshot = snapshot;

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] audio snapshot set. reason='{NormalizeReason(reason)}' snapshot={snapshot}.",
                DebugUtility.Colors.Info);
        }

        public void SetCurrent(VideoPreferencesSnapshot snapshot, string reason)
        {
            _currentVideoSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _lastCommittedVideoSnapshot = snapshot;

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] video snapshot set. reason='{NormalizeReason(reason)}' snapshot={snapshot}.",
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
                $"[Preferences] audio snapshot applied to runtime. reason='{NormalizeReason(reason)}' snapshot={CurrentSnapshot}.",
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
                $"[Preferences] video runtime apply resolution={snapshot.ResolutionWidth}x{snapshot.ResolutionHeight} mode='{mode}' reason='{NormalizeReason(reason)}'.",
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
                DebugUtility.LogVerbose<PreferencesService>(
                    $"[Preferences] audio preview skipped. reason='no_change' snapshot={CurrentSnapshot}.",
                    DebugUtility.Colors.Info);
                return false;
            }

            _currentSnapshot = nextSnapshot;
            changed = true;

            ApplyCurrentAudioSnapshotToRuntime(reason);

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] audio preview applied. reason='{NormalizeReason(reason)}' snapshot={CurrentSnapshot}.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool TryCommitCurrentAudioVolumes(
            string reason,
            string fieldHint,
            out bool changed,
            out string saveReason)
        {
            if (!HasSnapshot)
            {
                changed = false;
                saveReason = "missing_current_snapshot";
                throw new InvalidOperationException("[FATAL][Preferences] Audio volume commit requested before any snapshot was seeded.");
            }

            string resolvedField = NormalizeFieldHint(fieldHint);

            if (HasSameAudioValues(CurrentSnapshot, _lastCommittedSnapshot))
            {
                changed = false;
                saveReason = "no_change";
                LogAudioCommitState(
                    result: saveReason,
                    field: resolvedField,
                    committedFrom: _lastCommittedSnapshot,
                    committedTo: CurrentSnapshot);

                return true;
            }

            changed = true;
            var committedFrom = _lastCommittedSnapshot;

            bool saved = TrySaveCurrentAudio(out saveReason);
            if (!saved)
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] audio commit failed. reason='{NormalizeReason(reason)}' snapshot={CurrentSnapshot} saveReason='{saveReason}'.");
                return false;
            }

            _lastCommittedSnapshot = CurrentSnapshot;

            saveReason = "save_executed";
            LogAudioCommitState(
                result: saveReason,
                field: resolvedField,
                committedFrom: committedFrom,
                committedTo: CurrentSnapshot);

            return true;
        }

        public bool TryRestoreAudioDefaults(
            string reason,
            out string saveReason)
        {
            if (!HasSnapshot)
            {
                saveReason = "missing_current_snapshot";
                throw new InvalidOperationException("[FATAL][Preferences] Audio defaults restore requested before any snapshot was seeded.");
            }

            var restoredSnapshot = new AudioPreferencesSnapshot(
                CurrentSnapshot.ProfileId,
                CurrentSnapshot.SlotId,
                _audioDefaults.MasterVolume,
                _audioDefaults.BgmVolume,
                _audioDefaults.SfxVolume);

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] restore defaults requested. backend='{BackendId}' snapshot={restoredSnapshot}.",
                DebugUtility.Colors.Info);

            _currentSnapshot = restoredSnapshot;
            ApplyCurrentAudioSnapshotToRuntime(reason);

            string restoreField = "RestoreDefaults";
            var committedFrom = _lastCommittedSnapshot;
            if (HasSameAudioValues(restoredSnapshot, _lastCommittedSnapshot))
            {
                saveReason = "no_change";
                LogAudioCommitState(
                    result: saveReason,
                    field: restoreField,
                    committedFrom: _lastCommittedSnapshot,
                    committedTo: restoredSnapshot);

                return true;
            }

            bool saved = TrySaveCurrentAudio(out saveReason);
            if (!saved)
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] restore defaults failed. reason='{NormalizeReason(reason)}' snapshot={restoredSnapshot} saveReason='{saveReason}'.");
                return false;
            }

            _lastCommittedSnapshot = restoredSnapshot;

            saveReason = "save_executed";
            LogAudioCommitState(
                result: saveReason,
                field: restoreField,
                committedFrom: committedFrom,
                committedTo: restoredSnapshot);

            return true;
        }

        public bool TryRestoreVideoDefaults(
            string reason,
            out string saveReason)
        {
            if (!HasVideoSnapshot)
            {
                saveReason = "missing_current_snapshot";
                throw new InvalidOperationException("[FATAL][Preferences] Video defaults restore requested before any snapshot was seeded.");
            }

            var restoredSnapshot = new VideoPreferencesSnapshot(
                CurrentVideoSnapshot.ProfileId,
                CurrentVideoSnapshot.SlotId,
                _videoDefaults.DefaultResolutionWidth,
                _videoDefaults.DefaultResolutionHeight,
                _videoDefaults.DefaultFullscreen);

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] restore video defaults requested. backend='{BackendId}' snapshot={restoredSnapshot}.",
                DebugUtility.Colors.Info);

            _currentVideoSnapshot = restoredSnapshot;
            ApplyCurrentVideoToRuntime(reason);

            string restoreField = "RestoreDefaults";
            var committedFrom = _lastCommittedVideoSnapshot;
            if (HasSameVideoValues(restoredSnapshot, _lastCommittedVideoSnapshot))
            {
                saveReason = "no_change";
                LogVideoCommitState(
                    result: saveReason,
                    field: restoreField,
                    committedFrom: _lastCommittedVideoSnapshot,
                    committedTo: restoredSnapshot);

                return true;
            }

            bool saved = TrySaveCurrentVideo(out saveReason);
            if (!saved)
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] restore video defaults failed. reason='{NormalizeReason(reason)}' snapshot={restoredSnapshot} saveReason='{saveReason}'.");
                return false;
            }

            _lastCommittedVideoSnapshot = restoredSnapshot;

            saveReason = "save_executed";
            LogVideoCommitState(
                result: saveReason,
                field: restoreField,
                committedFrom: committedFrom,
                committedTo: restoredSnapshot);

            return true;
        }

        public bool TryLoad(
            string profileId,
            string slotId,
            out AudioPreferencesSnapshot snapshot,
            out string reason)
        {
            if (!IsBackendAvailable)
            {
                snapshot = null;
                reason = "backend_unavailable";

                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] audio load skipped. backend='{BackendId}' profile='{profileId}' slot='{slotId}' reason='{reason}'.");

                return false;
            }

            if (_backend.TryLoad(profileId, slotId, out snapshot, out reason) && snapshot != null)
            {
                _currentSnapshot = snapshot;
                _lastCommittedSnapshot = snapshot;

                DebugUtility.Log(typeof(PreferencesService),
                    $"[Preferences] audio load executed. backend='{BackendId}' profile='{profileId}' slot='{slotId}' snapshot={snapshot}.",
                    DebugUtility.Colors.Info);

                return true;
            }

            if (string.Equals(reason, "no_saved_data", StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<PreferencesService>(
                    $"[Preferences] audio load skipped. backend='{BackendId}' profile='{profileId}' slot='{slotId}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] audio load failed. backend='{BackendId}' profile='{profileId}' slot='{slotId}' reason='{reason}'.");
            }

            snapshot = null;
            return false;
        }

        public bool TryLoadVideo(
            string profileId,
            string slotId,
            out VideoPreferencesSnapshot snapshot,
            out string reason)
        {
            if (!IsBackendAvailable)
            {
                snapshot = null;
                reason = "backend_unavailable";

                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] video load skipped. backend='{BackendId}' profile='{profileId}' slot='{slotId}' reason='{reason}'.");

                return false;
            }

            if (_backend.TryLoadVideo(profileId, slotId, out snapshot, out reason) && snapshot != null)
            {
                snapshot = NormalizeLoadedVideoSnapshot(snapshot);
                _currentVideoSnapshot = snapshot;
                _lastCommittedVideoSnapshot = snapshot;

                DebugUtility.Log(typeof(PreferencesService),
                    $"[Preferences] video load executed. backend='{BackendId}' profile='{profileId}' slot='{slotId}' snapshot={snapshot}.",
                    DebugUtility.Colors.Info);

                return true;
            }

            if (string.Equals(reason, "no_saved_data", StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<PreferencesService>(
                    $"[Preferences] video load skipped. backend='{BackendId}' profile='{profileId}' slot='{slotId}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] video load failed. backend='{BackendId}' profile='{profileId}' slot='{slotId}' reason='{reason}'.");
            }

            snapshot = null;
            return false;
        }

        public bool TrySaveCurrent(out string reason)
        {
            if (!HasSnapshot)
            {
                reason = "missing_current_snapshot";
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] audio save skipped. backend='{BackendId}' reason='{reason}'.");
                return false;
            }

            return TrySaveCurrentAudio(out reason);
        }

        public bool TrySave(AudioPreferencesSnapshot snapshot, out string reason)
        {
            if (snapshot == null)
            {
                reason = "snapshot_null";
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (!IsBackendAvailable)
            {
                reason = "backend_unavailable";

                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] audio save skipped. backend='{BackendId}' snapshot={snapshot} reason='{reason}'.");

                return false;
            }

            bool saved = _backend.TrySave(snapshot, out reason);
            if (!saved)
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] audio save failed. backend='{BackendId}' snapshot={snapshot} reason='{reason}'.");
                return false;
            }

            _lastCommittedSnapshot = snapshot;

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
                profileId: CurrentVideoSnapshot.ProfileId,
                slotId: CurrentVideoSnapshot.SlotId,
                width: width,
                height: height,
                fullscreen: fullscreen,
                reason: reason);

            if (HasSameVideoValues(CurrentVideoSnapshot, nextSnapshot))
            {
                changed = false;
                DebugUtility.LogVerbose<PreferencesService>(
                    $"[Preferences] video preview skipped. reason='no_change' snapshot={CurrentVideoSnapshot}.",
                    DebugUtility.Colors.Info);
                return false;
            }

            _currentVideoSnapshot = nextSnapshot;
            changed = true;

            ApplyCurrentVideoToRuntime(reason);

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] video preview applied. reason='{NormalizeReason(reason)}' snapshot={CurrentVideoSnapshot}.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool TryCommitCurrentVideoResolution(
            string reason,
            string fieldHint,
            out bool changed,
            out string saveReason)
        {
            if (!HasVideoSnapshot)
            {
                changed = false;
                saveReason = "missing_current_snapshot";
                throw new InvalidOperationException("[FATAL][Preferences] Video resolution commit requested before any snapshot was seeded.");
            }

            string resolvedField = NormalizeFieldHint(fieldHint);

            if (HasSameVideoValues(CurrentVideoSnapshot, _lastCommittedVideoSnapshot))
            {
                changed = false;
                saveReason = "no_change";
                LogVideoCommitState(
                    result: saveReason,
                    field: resolvedField,
                    committedFrom: _lastCommittedVideoSnapshot,
                    committedTo: CurrentVideoSnapshot);

                return true;
            }

            changed = true;
            var committedFrom = _lastCommittedVideoSnapshot;

            bool saved = TrySaveCurrentVideo(out saveReason);
            if (!saved)
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] video commit failed. reason='{NormalizeReason(reason)}' snapshot={CurrentVideoSnapshot} saveReason='{saveReason}'.");
                return false;
            }

            _lastCommittedVideoSnapshot = CurrentVideoSnapshot;

            saveReason = "save_executed";
            LogVideoCommitState(
                result: saveReason,
                field: resolvedField,
                committedFrom: committedFrom,
                committedTo: CurrentVideoSnapshot);

            return true;
        }

        private bool TrySaveCurrentAudio(out string reason)
        {
            if (!HasSnapshot)
            {
                reason = "missing_current_snapshot";
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] audio save skipped. backend='{BackendId}' reason='{reason}'.");
                return false;
            }

            return TrySave(CurrentSnapshot, out reason);
        }

        private bool TrySaveCurrentVideo(out string reason)
        {
            if (!HasVideoSnapshot)
            {
                reason = "missing_current_snapshot";
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] video save skipped. backend='{BackendId}' reason='{reason}'.");
                return false;
            }

            return TrySaveVideo(CurrentVideoSnapshot, out reason);
        }

        private bool TrySaveVideo(VideoPreferencesSnapshot snapshot, out string reason)
        {
            if (snapshot == null)
            {
                reason = "snapshot_null";
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (!IsBackendAvailable)
            {
                reason = "backend_unavailable";

                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] video save skipped. backend='{BackendId}' snapshot={snapshot} reason='{reason}'.");

                return false;
            }

            bool saved = _backend.TrySaveVideo(snapshot, out reason);
            if (!saved)
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] video save failed. backend='{BackendId}' snapshot={snapshot} reason='{reason}'.");
                return false;
            }

            _lastCommittedVideoSnapshot = snapshot;

            return true;
        }

        private void ApplyCurrentAudioSnapshotToRuntime(string reason)
        {
            CurrentSnapshot.ApplyTo(_audioSettings);
        }

        private VideoPreferencesSnapshot NormalizeLoadedVideoSnapshot(VideoPreferencesSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var resolved = ResolveSupportedVideoSnapshot(
                snapshot.ProfileId,
                snapshot.SlotId,
                snapshot.ResolutionWidth,
                snapshot.ResolutionHeight,
                snapshot.Fullscreen,
                reason: "Preferences/LoadVideo");

            return resolved;
        }

        private VideoPreferencesSnapshot ResolveSupportedVideoSnapshot(
            string profileId,
            string slotId,
            int width,
            int height,
            bool fullscreen,
            string reason)
        {
            Vector2Int requested = new Vector2Int(width, height);
            if (IsSupportedVideoResolution(requested))
            {
                return new VideoPreferencesSnapshot(profileId, slotId, width, height, fullscreen);
            }

            Vector2Int fallback = ResolveFallbackVideoResolution();
            DebugUtility.Log<PreferencesService>(
                $"[Preferences] video resolution normalized. reason='{NormalizeReason(reason)}' requested={width}x{height} fallback={fallback.x}x{fallback.y}.",
                DebugUtility.Colors.Info);

            return new VideoPreferencesSnapshot(profileId, slotId, fallback.x, fallback.y, fullscreen);
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
            if (current.x > 0 && current.y > 0)
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

        private bool IsSupportedVideoResolution(Vector2Int preset)
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

        public IReadOnlyList<Vector2Int> GetVideoResolutionPresets()
        {
            return BuildSupportedVideoResolutionPresets();
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "Preferences/Unknown" : reason.Trim();
        }

        private void LogAudioCommitState(
            string result,
            string field,
            AudioPreferencesSnapshot committedFrom,
            AudioPreferencesSnapshot committedTo)
        {
            if (committedFrom == null || committedTo == null)
            {
                return;
            }

            DebugUtility.Log<PreferencesService>(
                $"[Preferences] audio commit field='{field}' committed_from={FormatAudioCommitSnapshot(committedFrom)} committed_to={FormatAudioCommitSnapshot(committedTo)} result='{result}'.",
                DebugUtility.Colors.Info);
        }

        private void LogVideoCommitState(
            string result,
            string field,
            VideoPreferencesSnapshot committedFrom,
            VideoPreferencesSnapshot committedTo)
        {
            if (committedFrom == null || committedTo == null)
            {
                return;
            }

            DebugUtility.Log<PreferencesService>(
                $"[Preferences] video commit field='{field}' committed_from={FormatVideoCommitSnapshot(committedFrom)} committed_to={FormatVideoCommitSnapshot(committedTo)} result='{result}'.",
                DebugUtility.Colors.Info);
        }

        private static string NormalizeFieldHint(string fieldHint)
        {
            if (string.IsNullOrWhiteSpace(fieldHint))
            {
                throw new ArgumentException("[FATAL][Preferences] fieldHint obrigatorio para o commit canonico.");
            }

            return fieldHint.Trim();
        }

        private static string FormatAudioCommitSnapshot(AudioPreferencesSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "null";
            }

            return $"master={snapshot.MasterVolume:0.###} bgm={snapshot.BgmVolume:0.###} sfx={snapshot.SfxVolume:0.###}";
        }

        private static string FormatVideoCommitSnapshot(VideoPreferencesSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "null";
            }

            return $"resolution={snapshot.ResolutionWidth}x{snapshot.ResolutionHeight} fullscreen={snapshot.Fullscreen}";
        }

        private static bool HasSameAudioValues(
            AudioPreferencesSnapshot left,
            AudioPreferencesSnapshot right)
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

        private static bool HasSameVideoValues(
            VideoPreferencesSnapshot left,
            VideoPreferencesSnapshot right)
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
