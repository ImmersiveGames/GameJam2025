using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Runtime
{
    public sealed class PreferencesSaveAdapter : Contracts.IPreferencesSaveAdapter
    {
        private const string ScopeEntryKey = "preferences.address.scope";
        private const string GroupEntryKey = "preferences.address.group";
        private const string OwnerEntryKey = "preferences.address.ownerId";
        private const string RecordEntryKey = "preferences.address.recordId";
        private const string SlotEntryKey = "preferences.address.slotId";
        private const string SchemaIdEntryKey = "preferences.address.schemaId";
        private const string SchemaVersionEntryKey = "preferences.address.schemaVersion";
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
            var identity = new SaveIdentity(profileId, slotId);
            if (!_saveService.TryLoad(identity, out SaveRecord record, out reason) || record == null)
            {
                snapshot = null;
                return false;
            }

            if (!TryValidateAddressEntries(record, SaveScope.Preferences, SaveGroup.PreferencesAudio, AudioSchemaId, out reason))
            {
                snapshot = null;
                return false;
            }

            if (!record.Entries.TryGetValue(PayloadAudioKey, out string payload) || string.IsNullOrWhiteSpace(payload))
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
            var identity = new SaveIdentity(profileId, slotId);
            if (!_saveService.TryLoad(identity, out SaveRecord record, out reason) || record == null)
            {
                snapshot = null;
                return false;
            }

            if (!TryValidateAddressEntries(record, SaveScope.Preferences, SaveGroup.PreferencesVideo, VideoSchemaId, out reason))
            {
                snapshot = null;
                return false;
            }

            if (!record.Entries.TryGetValue(PayloadVideoKey, out string payload) || string.IsNullOrWhiteSpace(payload))
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

            Dictionary<string, string> entries = BuildBaseEntries(address);
            entries[PayloadAudioKey] = JsonUtility.ToJson(new AudioPayloadDto
            {
                masterVolume = snapshot.MasterVolume,
                bgmVolume = snapshot.BgmVolume,
                sfxVolume = snapshot.SfxVolume,
            });

            SaveRecord record = new(
                identity: new SaveIdentity(snapshot.ProfileId, snapshot.SlotId),
                schemaVersion: address.SchemaVersion,
                revision: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                savedAtUtc: DateTime.UtcNow.ToString("O"),
                entries: entries);

            return _saveService.TrySave(record, out reason);
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

            Dictionary<string, string> entries = BuildBaseEntries(address);
            entries[PayloadVideoKey] = JsonUtility.ToJson(new VideoPayloadDto
            {
                width = snapshot.ResolutionWidth,
                height = snapshot.ResolutionHeight,
                fullscreen = snapshot.Fullscreen,
            });

            SaveRecord record = new(
                identity: new SaveIdentity(snapshot.ProfileId, snapshot.SlotId),
                schemaVersion: address.SchemaVersion,
                revision: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                savedAtUtc: DateTime.UtcNow.ToString("O"),
                entries: entries);

            return _saveService.TrySave(record, out reason);
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

        private static Dictionary<string, string> BuildBaseEntries(SaveAddress address)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ScopeEntryKey] = address.Scope.ToString(),
                [GroupEntryKey] = address.Group.ToString(),
                [OwnerEntryKey] = address.OwnerId,
                [RecordEntryKey] = address.RecordId,
                [SlotEntryKey] = address.SlotId,
                [SchemaIdEntryKey] = address.SchemaId,
                [SchemaVersionEntryKey] = address.SchemaVersion.ToString(),
            };
        }

        private static bool TryValidateAddressEntries(
            SaveRecord record,
            SaveScope expectedScope,
            SaveGroup expectedGroup,
            string expectedSchemaId,
            out string reason)
        {
            if (record?.Entries == null)
            {
                reason = "entries_missing";
                return false;
            }

            if (!record.Entries.TryGetValue(ScopeEntryKey, out string scopeText) ||
                !string.Equals(scopeText, expectedScope.ToString(), StringComparison.Ordinal))
            {
                reason = "scope_mismatch";
                return false;
            }

            if (!record.Entries.TryGetValue(GroupEntryKey, out string groupText) ||
                !string.Equals(groupText, expectedGroup.ToString(), StringComparison.Ordinal))
            {
                reason = "group_mismatch";
                return false;
            }

            if (!record.Entries.TryGetValue(SchemaIdEntryKey, out string schemaId) ||
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

