using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Runtime
{
    public sealed class PreferencesSaveAdapter : Contracts.IPreferencesSaveAdapter
    {
        private const string ScopeEntryKey = "save.address.scope";
        private const string GroupEntryKey = "save.address.group";
        private const string SchemaIdEntryKey = "save.address.schemaId";
        private const string PayloadAudioKey = "preferences.payload.audio";
        private const string PayloadVideoKey = "preferences.payload.video";

        private const string AudioSchemaId = "preferences.audio";
        private const string VideoSchemaId = "preferences.video";
        private const string AudioRecordId = "audio";
        private const string VideoRecordId = "video";

        private readonly ISaveService _saveService;
        private readonly int _schemaVersion;

        public PreferencesSaveAdapter(ISaveService saveService, int schemaVersion)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            if (schemaVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), schemaVersion, "schemaVersion must be greater than zero.");
            }

            _schemaVersion = schemaVersion;
        }

        public bool TryLoadAudio(
            string profileId,
            string slotId,
            out Contracts.AudioPreferencesSnapshot snapshot,
            out string reason)
        {
            SaveAddress address = BuildAddress(
                SaveGroup.PreferencesAudio,
                profileId,
                AudioRecordId,
                slotId,
                AudioSchemaId);

            if (!_saveService.TryLoad(address, out SaveResult loadResult, out reason) ||
                loadResult == null ||
                !loadResult.HasEntries)
            {
                snapshot = null;
                return false;
            }

            if (!TryValidateAddressEntries(loadResult, SaveScope.Preferences, SaveGroup.PreferencesAudio, AudioSchemaId, out reason))
            {
                snapshot = null;
                return false;
            }

            if (!loadResult.Entries.TryGetValue(PayloadAudioKey, out string payload) || string.IsNullOrWhiteSpace(payload))
            {
                snapshot = null;
                reason = "payload_audio_missing";
                return false;
            }

            try
            {
                AudioPayloadDto dto = JsonUtility.FromJson<AudioPayloadDto>(payload);
                snapshot = new Contracts.AudioPreferencesSnapshot(
                    profileId,
                    slotId,
                    dto.masterVolume,
                    dto.bgmVolume,
                    dto.sfxVolume);
                reason = "loaded";
                return true;
            }
            catch (Exception)
            {
                snapshot = null;
                reason = "payload_audio_invalid";
                return false;
            }
        }

        public bool TryLoadVideo(
            string profileId,
            string slotId,
            out Contracts.VideoPreferencesSnapshot snapshot,
            out string reason)
        {
            SaveAddress address = BuildAddress(
                SaveGroup.PreferencesVideo,
                profileId,
                VideoRecordId,
                slotId,
                VideoSchemaId);

            if (!_saveService.TryLoad(address, out SaveResult loadResult, out reason) ||
                loadResult == null ||
                !loadResult.HasEntries)
            {
                snapshot = null;
                return false;
            }

            if (!TryValidateAddressEntries(loadResult, SaveScope.Preferences, SaveGroup.PreferencesVideo, VideoSchemaId, out reason))
            {
                snapshot = null;
                return false;
            }

            if (!loadResult.Entries.TryGetValue(PayloadVideoKey, out string payload) || string.IsNullOrWhiteSpace(payload))
            {
                snapshot = null;
                reason = "payload_video_missing";
                return false;
            }

            try
            {
                VideoPayloadDto dto = JsonUtility.FromJson<VideoPayloadDto>(payload);
                snapshot = new Contracts.VideoPreferencesSnapshot(
                    profileId,
                    slotId,
                    dto.width,
                    dto.height,
                    dto.fullscreen);
                reason = "loaded";
                return true;
            }
            catch (Exception)
            {
                snapshot = null;
                reason = "payload_video_invalid";
                return false;
            }
        }

        public bool TrySaveAudio(Contracts.AudioPreferencesSnapshot snapshot, out string reason)
        {
            if (snapshot == null)
            {
                reason = "snapshot_null";
                throw new ArgumentNullException(nameof(snapshot));
            }

            SaveAddress address = BuildAddress(
                SaveGroup.PreferencesAudio,
                snapshot.ProfileId,
                AudioRecordId,
                snapshot.SlotId,
                AudioSchemaId);

            Dictionary<string, string> entries = new(StringComparer.Ordinal)
            {
                [PayloadAudioKey] = JsonUtility.ToJson(new AudioPayloadDto
                {
                    masterVolume = snapshot.MasterVolume,
                    bgmVolume = snapshot.BgmVolume,
                    sfxVolume = snapshot.SfxVolume,
                }),
            };
            SaveRequest request = new SaveRequest(
                address,
                entries,
                revision: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                savedAtUtc: DateTime.UtcNow.ToString("O"));

            return _saveService.TrySave(request, out SaveResult _, out reason);
        }

        public bool TrySaveVideo(Contracts.VideoPreferencesSnapshot snapshot, out string reason)
        {
            if (snapshot == null)
            {
                reason = "snapshot_null";
                throw new ArgumentNullException(nameof(snapshot));
            }

            SaveAddress address = BuildAddress(
                SaveGroup.PreferencesVideo,
                snapshot.ProfileId,
                VideoRecordId,
                snapshot.SlotId,
                VideoSchemaId);

            Dictionary<string, string> entries = new(StringComparer.Ordinal)
            {
                [PayloadVideoKey] = JsonUtility.ToJson(new VideoPayloadDto
                {
                    width = snapshot.ResolutionWidth,
                    height = snapshot.ResolutionHeight,
                    fullscreen = snapshot.Fullscreen,
                }),
            };
            SaveRequest request = new SaveRequest(
                address,
                entries,
                revision: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                savedAtUtc: DateTime.UtcNow.ToString("O"));

            return _saveService.TrySave(request, out SaveResult _, out reason);
        }

        private SaveAddress BuildAddress(
            SaveGroup group,
            string ownerId,
            string recordId,
            string slotId,
            string schemaId)
        {
            return new SaveAddress(
                scope: SaveScope.Preferences,
                group: group,
                ownerId: ownerId,
                recordId: recordId,
                slotId: slotId,
                schemaId: schemaId,
                schemaVersion: _schemaVersion);
        }

        private static bool TryValidateAddressEntries(
            SaveResult result,
            SaveScope expectedScope,
            SaveGroup expectedGroup,
            string expectedSchemaId,
            out string reason)
        {
            if (result?.Entries == null)
            {
                reason = "entries_missing";
                return false;
            }

            if (!result.Entries.TryGetValue(ScopeEntryKey, out string scopeText) ||
                !string.Equals(scopeText, expectedScope.ToString(), StringComparison.Ordinal))
            {
                reason = "scope_mismatch";
                return false;
            }

            if (!result.Entries.TryGetValue(GroupEntryKey, out string groupText) ||
                !string.Equals(groupText, expectedGroup.ToString(), StringComparison.Ordinal))
            {
                reason = "group_mismatch";
                return false;
            }

            if (!result.Entries.TryGetValue(SchemaIdEntryKey, out string schemaId) ||
                !string.Equals(schemaId, expectedSchemaId, StringComparison.Ordinal))
            {
                reason = "schema_id_mismatch";
                return false;
            }

            reason = "address_ok";
            return true;
        }

        [Serializable]
        private sealed class AudioPayloadDto
        {
            public float masterVolume;
            public float bgmVolume;
            public float sfxVolume;
        }

        [Serializable]
        private sealed class VideoPayloadDto
        {
            public int width;
            public int height;
            public bool fullscreen;
        }
    }
}
