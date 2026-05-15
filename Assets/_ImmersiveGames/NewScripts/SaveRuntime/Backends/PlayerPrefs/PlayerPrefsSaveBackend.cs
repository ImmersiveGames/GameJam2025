using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Backends.PlayerPrefs
{
    public sealed class PlayerPrefsSaveBackend : ISaveBackend
    {
        private const string RootPrefix = "NewScripts.SaveRuntime.v1";
        private const string ProfileSegment = "profile";
        private const string SlotSegment = "slot";
        private const string RecordSegment = "record";

        public string BackendId => "PlayerPrefsSaveBackend";

        public bool TryLoad(
            SaveIdentity identity,
            out SaveRecord record,
            out string reason)
        {
            if (!TryValidateIdentity(identity, out reason))
            {
                record = null;
                return false;
            }

            string key = BuildRecordKey(identity);
            if (!global::UnityEngine.PlayerPrefs.HasKey(key))
            {
                record = null;
                reason = "no_saved_data";
                return false;
            }

            string payload = global::UnityEngine.PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(payload))
            {
                record = null;
                reason = "payload_empty";
                return false;
            }

            if (!TryDeserialize(payload, out record, out reason) || record == null)
            {
                record = null;
                return false;
            }

            if (!IsMatchingIdentity(identity, record.Identity))
            {
                record = null;
                reason = "identity_mismatch";
                return false;
            }

            reason = "loaded";
            return true;
        }

        public bool TrySave(
            SaveRecord record,
            out string reason)
        {
            if (record == null)
            {
                reason = "record_null";
                return false;
            }

            if (!TryValidateIdentity(record.Identity, out reason))
            {
                return false;
            }

            if (record.SchemaVersion <= 0)
            {
                reason = "invalid_schema_version";
                return false;
            }

            string key = BuildRecordKey(record.Identity);
            string payload = Serialize(record);

            global::UnityEngine.PlayerPrefs.SetString(key, payload);
            global::UnityEngine.PlayerPrefs.Save();
            reason = "saved";
            return true;
        }

        public bool TryExists(
            SaveIdentity identity,
            out bool exists,
            out string reason)
        {
            if (!TryValidateIdentity(identity, out reason))
            {
                exists = false;
                return false;
            }

            exists = global::UnityEngine.PlayerPrefs.HasKey(BuildRecordKey(identity));
            reason = "exists_checked";
            return true;
        }

        public bool TryDelete(
            SaveIdentity identity,
            out string reason)
        {
            if (!TryValidateIdentity(identity, out reason))
            {
                return false;
            }

            string key = BuildRecordKey(identity);
            if (!global::UnityEngine.PlayerPrefs.HasKey(key))
            {
                reason = "delete_no_op";
                return false;
            }

            global::UnityEngine.PlayerPrefs.DeleteKey(key);
            global::UnityEngine.PlayerPrefs.Save();
            reason = "delete_executed";
            return true;
        }

        private static bool TryDeserialize(string payload, out SaveRecord record, out string reason)
        {
            try
            {
                SaveRecordDto dto = JsonUtility.FromJson<SaveRecordDto>(payload);
                if (dto == null)
                {
                    record = null;
                    reason = "deserialize_failed";
                    return false;
                }

                var identity = new SaveIdentity(dto.profileId, dto.slotId);
                var entries = new Dictionary<string, string>(StringComparer.Ordinal);

                if (dto.entries != null)
                {
                    for (int i = 0; i < dto.entries.Length; i++)
                    {
                        EntryDto entry = dto.entries[i];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                        {
                            continue;
                        }

                        entries[entry.key.Trim()] = entry.value ?? string.Empty;
                    }
                }

                record = new SaveRecord(
                    identity,
                    dto.schemaVersion,
                    dto.revision,
                    dto.savedAtUtc,
                    entries);

                reason = "deserialized";
                return true;
            }
            catch (Exception ex)
            {
                _ = ex;
                record = null;
                reason = "deserialize_exception";
                return false;
            }
        }

        private static string Serialize(SaveRecord record)
        {
            var dto = new SaveRecordDto
            {
                profileId = record.Identity.ProfileId,
                slotId = record.Identity.SlotId,
                schemaVersion = record.SchemaVersion,
                revision = record.Revision,
                savedAtUtc = record.SavedAtUtc,
                entries = BuildEntries(record.Entries),
            };

            return JsonUtility.ToJson(dto);
        }

        private static EntryDto[] BuildEntries(Dictionary<string, string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<EntryDto>();
            }

            var list = new List<EntryDto>(source.Count);
            foreach (var pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                list.Add(new EntryDto
                {
                    key = pair.Key.Trim(),
                    value = pair.Value ?? string.Empty,
                });
            }

            return list.ToArray();
        }

        private static bool TryValidateIdentity(SaveIdentity identity, out string reason)
        {
            if (identity == null)
            {
                reason = "identity_null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(identity.ProfileId) || string.IsNullOrWhiteSpace(identity.SlotId))
            {
                reason = "identity_invalid";
                return false;
            }

            reason = "identity_ok";
            return true;
        }

        private static bool IsMatchingIdentity(SaveIdentity requested, SaveIdentity loaded)
        {
            if (requested == null || loaded == null)
            {
                return false;
            }

            return string.Equals(requested.ProfileId, loaded.ProfileId, StringComparison.Ordinal)
                && string.Equals(requested.SlotId, loaded.SlotId, StringComparison.Ordinal);
        }

        private static string BuildRecordKey(SaveIdentity identity)
        {
            string profile = NormalizeSegment(identity.ProfileId);
            string slot = NormalizeSegment(identity.SlotId);
            return $"{RootPrefix}.{ProfileSegment}.{profile}.{SlotSegment}.{slot}.{RecordSegment}";
        }

        private static string NormalizeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identity segment is required.");
            }

            string trimmed = value.Trim().ToLowerInvariant();
            char[] chars = trimmed.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        [Serializable]
        private sealed class SaveRecordDto
        {
            public string profileId;
            public string slotId;
            public int schemaVersion;
            public long revision;
            public string savedAtUtc;
            public EntryDto[] entries;
        }

        [Serializable]
        private sealed class EntryDto
        {
            public string key;
            public string value;
        }
    }
}
