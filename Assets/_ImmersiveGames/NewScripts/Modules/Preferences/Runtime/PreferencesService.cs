using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using _ImmersiveGames.NewScripts.Modules.Preferences.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Preferences.Runtime
{
    public sealed class PreferencesService : IPreferencesStateService, IPreferencesSaveService
    {
        private readonly IPreferencesBackend _backend;
        private readonly IAudioSettingsService _audioSettings;
        private readonly AudioDefaultsAsset _audioDefaults;
        private AudioPreferencesSnapshot _currentSnapshot;
        private AudioPreferencesSnapshot _lastCommittedSnapshot;

        public PreferencesService(
            IPreferencesBackend backend,
            IAudioSettingsService audioSettings,
            AudioDefaultsAsset audioDefaults)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _audioSettings = audioSettings ?? throw new ArgumentNullException(nameof(audioSettings));
            _audioDefaults = audioDefaults ?? throw new ArgumentNullException(nameof(audioDefaults));
        }

        public string BackendId => _backend.BackendId;
        public bool IsBackendAvailable => _backend.IsAvailable;
        public bool HasSnapshot => _currentSnapshot != null;

        public AudioPreferencesSnapshot CurrentSnapshot =>
            _currentSnapshot ?? throw new InvalidOperationException("[FATAL][Preferences] Current snapshot requested before initialization.");

        public void SetCurrent(AudioPreferencesSnapshot snapshot, string reason)
        {
            _currentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _lastCommittedSnapshot = snapshot;

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] snapshot set. reason='{NormalizeReason(reason)}' snapshot={snapshot}.",
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
                throw new InvalidOperationException("[FATAL][Preferences] Apply requested before any snapshot was seeded.");
            }

            CurrentSnapshot.ApplyTo(audioSettings);

            DebugUtility.LogVerbose<PreferencesService>(
                $"[Preferences] snapshot applied to audio runtime. reason='{NormalizeReason(reason)}' snapshot={CurrentSnapshot}.",
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

            ApplyCurrentSnapshotToAudioRuntime(reason);

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
                LogCommitState(
                    result: saveReason,
                    field: resolvedField,
                    committedFrom: _lastCommittedSnapshot,
                    committedTo: CurrentSnapshot);

                return true;
            }

            changed = true;
            var committedFrom = _lastCommittedSnapshot;

            bool saved = TrySaveCurrent(out saveReason);
            if (!saved)
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] audio commit failed. reason='{NormalizeReason(reason)}' snapshot={CurrentSnapshot} saveReason='{saveReason}'.");
                return false;
            }

            _lastCommittedSnapshot = CurrentSnapshot;

            saveReason = "save_executed";
            LogCommitState(
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
            ApplyCurrentSnapshotToAudioRuntime(reason);

            string restoreField = "RestoreDefaults";
            var committedFrom = _lastCommittedSnapshot;
            if (HasSameAudioValues(restoredSnapshot, _lastCommittedSnapshot))
            {
                saveReason = "no_change";
                LogCommitState(
                    result: saveReason,
                    field: restoreField,
                    committedFrom: _lastCommittedSnapshot,
                    committedTo: restoredSnapshot);

                return true;
            }

            bool saved = TrySaveCurrent(out saveReason);
            if (!saved)
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] restore defaults failed. reason='{NormalizeReason(reason)}' snapshot={restoredSnapshot} saveReason='{saveReason}'.");
                return false;
            }

            _lastCommittedSnapshot = restoredSnapshot;

            saveReason = "save_executed";
            LogCommitState(
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
                    $"[Preferences] load skipped. backend='{BackendId}' profile='{profileId}' slot='{slotId}' reason='{reason}'.");

                return false;
            }

            if (_backend.TryLoad(profileId, slotId, out snapshot, out reason) && snapshot != null)
            {
                _currentSnapshot = snapshot;
                _lastCommittedSnapshot = snapshot;

                DebugUtility.Log(typeof(PreferencesService),
                    $"[Preferences] load executed. backend='{BackendId}' profile='{profileId}' slot='{slotId}' snapshot={snapshot}.",
                    DebugUtility.Colors.Info);

                return true;
            }

            if (string.Equals(reason, "no_saved_data", StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<PreferencesService>(
                    $"[Preferences] load skipped. backend='{BackendId}' profile='{profileId}' slot='{slotId}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] load failed. backend='{BackendId}' profile='{profileId}' slot='{slotId}' reason='{reason}'.");
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
                    $"[Preferences] save skipped. backend='{BackendId}' reason='{reason}'.");
                return false;
            }

            return TrySave(CurrentSnapshot, out reason);
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
                    $"[Preferences] save skipped. backend='{BackendId}' snapshot={snapshot} reason='{reason}'.");

                return false;
            }

            bool saved = _backend.TrySave(snapshot, out reason);
            if (!saved)
            {
                DebugUtility.LogWarning<PreferencesService>(
                    $"[Preferences] save failed. backend='{BackendId}' snapshot={snapshot} reason='{reason}'.");
                return false;
            }

            _lastCommittedSnapshot = snapshot;

            return true;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "Preferences/Unknown" : reason.Trim();
        }

        private void ApplyCurrentSnapshotToAudioRuntime(string reason)
        {
            CurrentSnapshot.ApplyTo(_audioSettings);
        }

        private static void LogCommitState(
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
                $"[Preferences] audio commit field='{field}' committed_from={FormatCommitSnapshot(committedFrom)} committed_to={FormatCommitSnapshot(committedTo)} result='{result}'.",
                DebugUtility.Colors.Info);
        }

        private static string NormalizeFieldHint(string fieldHint)
        {
            if (string.IsNullOrWhiteSpace(fieldHint))
            {
                throw new ArgumentException("[FATAL][Preferences] fieldHint obrigatorio para o commit canonico de Audio.");
            }

            return fieldHint.Trim();
        }

        private static string FormatCommitSnapshot(AudioPreferencesSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "null";
            }

            return $"master={snapshot.MasterVolume:0.###} bgm={snapshot.BgmVolume:0.###} sfx={snapshot.SfxVolume:0.###}";
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
    }
}
