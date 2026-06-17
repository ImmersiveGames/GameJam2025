using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Backends.InMemory
{
    public sealed class InMemorySaveBackend : ISaveBackend
    {
        private readonly Dictionary<(string profileId, string slotId), SaveRecord> _savedRecords = new();

        public string BackendId => "InMemorySaveBackend";

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

            if (_savedRecords.TryGetValue(ToKey(identity), out var stored) && stored != null)
            {
                record = CloneRecord(stored);
                reason = "loaded_from_in_memory_backend";
                return true;
            }

            record = null;
            reason = "no_saved_data";
            return false;
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

            _savedRecords[ToKey(record.Identity)] = CloneRecord(record);
            reason = "save_executed";
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

            exists = _savedRecords.ContainsKey(ToKey(identity));
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

            bool removed = _savedRecords.Remove(ToKey(identity));
            reason = removed ? "delete_executed" : "delete_no_op";
            return removed;
        }

        private static (string profileId, string slotId) ToKey(SaveIdentity identity)
        {
            return (identity.ProfileId.Trim(), identity.SlotId.Trim());
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

        private static SaveRecord CloneRecord(SaveRecord record)
        {
            Dictionary<string, string> entries = new(record.Entries, StringComparer.Ordinal);
            return new SaveRecord(
                new SaveIdentity(record.Identity.ProfileId, record.Identity.SlotId),
                record.SchemaVersion,
                record.Revision,
                record.SavedAtUtc,
                entries);
        }
    }
}

