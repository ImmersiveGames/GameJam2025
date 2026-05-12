using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Core
{
    public sealed class SaveCoreService : ISaveService, ISaveStateService
    {
        private readonly ISaveBackend _backend;
        private SaveRecord _currentRecord;

        public SaveCoreService(ISaveBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public bool HasCurrent => _currentRecord != null;

        public SaveRecord CurrentRecord =>
            _currentRecord ?? throw new InvalidOperationException("[FATAL][Save] Current save record requested before initialization.");

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

            bool loaded = _backend.TryLoad(identity, out record, out reason);
            if (!loaded || record == null)
            {
                record = null;
                return false;
            }

            if (!TryValidateRecord(record, out reason))
            {
                record = null;
                return false;
            }

            _currentRecord = CloneRecord(record);
            return true;
        }

        public bool TrySave(
            SaveRecord record,
            out string reason)
        {
            if (!TryValidateRecord(record, out reason))
            {
                return false;
            }

            bool saved = _backend.TrySave(record, out reason);
            if (!saved)
            {
                return false;
            }

            _currentRecord = CloneRecord(record);
            return true;
        }

        public bool TrySaveCurrent(out string reason)
        {
            if (!HasCurrent)
            {
                reason = "missing_current_record";
                return false;
            }

            return TrySave(CurrentRecord, out reason);
        }

        public bool TrySetCurrent(
            SaveRecord record,
            string reason,
            out string error)
        {
            _ = reason;
            if (!TryValidateRecord(record, out error))
            {
                return false;
            }

            _currentRecord = CloneRecord(record);
            error = "current_record_set";
            return true;
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

        private static bool TryValidateRecord(SaveRecord record, out string reason)
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

            if (record.Revision < 0)
            {
                reason = "invalid_revision";
                return false;
            }

            if (string.IsNullOrWhiteSpace(record.SavedAtUtc))
            {
                reason = "saved_at_utc_required";
                return false;
            }

            if (record.Entries == null)
            {
                reason = "entries_required";
                return false;
            }

            reason = "record_ok";
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

