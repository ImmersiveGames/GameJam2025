using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Core
{
    public sealed class SaveCoreService : ISaveService, ISaveStateService
    {
        private const string AddressScopeEntryKey = "save.address.scope";
        private const string AddressGroupEntryKey = "save.address.group";
        private const string AddressOwnerEntryKey = "save.address.ownerId";
        private const string AddressRecordEntryKey = "save.address.recordId";
        private const string AddressSlotEntryKey = "save.address.slotId";
        private const string AddressSchemaIdEntryKey = "save.address.schemaId";
        private const string AddressSchemaVersionEntryKey = "save.address.schemaVersion";
        private const string AddressProfileEntryKey = "save.address.profileId";

        private readonly ISaveBackend _backend;
        private SaveCurrentState _currentState;

        public SaveCoreService(ISaveBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public bool HasCurrent => _currentState != null;

        public SaveCurrentState CurrentState =>
            _currentState ?? throw new InvalidOperationException("[FATAL][Save] Current save state requested before initialization.");

        public bool TryLoad(
            SaveAddress address,
            out SaveResult result,
            out string reason)
        {
            if (!TryValidateAddress(address, out reason))
            {
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            if (!TryResolveProfileIdForAddress(address, out string profileId, out reason))
            {
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            var identity = new SaveIdentity(profileId, address.SlotId);
            if (!TryLoadByIdentity(identity, ShouldUpdateCurrentState(address), out var record, out reason) || record == null)
            {
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            if (!TryValidateAddressEntries(record, address, out reason))
            {
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            result = BuildResult(SaveResultKind.Loaded, "loaded_by_address", record);
            return true;
        }

        public bool TrySave(
            SaveRequest request,
            out SaveResult result,
            out string reason)
        {
            if (request == null)
            {
                reason = "request_null";
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            if (!TryValidateAddress(request.Address, out reason))
            {
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            if (!TryResolveProfileIdForRequest(request, out string profileId, out reason))
            {
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            Dictionary<string, string> entries = BuildEntriesWithAddressMetadata(request, profileId);
            SaveRecord record = new(
                new SaveIdentity(profileId, request.Address.SlotId),
                request.Address.SchemaVersion,
                request.Revision,
                request.SavedAtUtc,
                entries);

            bool saved = TrySaveRecord(record, ShouldUpdateCurrentState(request.Address), out reason);
            result = saved
                ? BuildResult(SaveResultKind.Saved, reason, record)
                : new SaveResult(SaveResultKind.Failed, reason);
            return saved;
        }

        public bool TryDelete(
            SaveAddress address,
            out SaveResult result,
            out string reason)
        {
            if (!TryValidateAddress(address, out reason))
            {
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            if (!TryResolveProfileIdForAddress(address, out string profileId, out reason))
            {
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            var identity = new SaveIdentity(profileId, address.SlotId);
            bool deleted = _backend.TryDelete(identity, out reason);
            if (!deleted)
            {
                result = new SaveResult(SaveResultKind.Failed, reason);
                return false;
            }

            if (ShouldUpdateCurrentState(address) &&
                HasCurrent &&
                string.Equals(CurrentState.ProfileId, identity.ProfileId, StringComparison.Ordinal) &&
                string.Equals(CurrentState.SlotId, identity.SlotId, StringComparison.Ordinal))
            {
                _currentState = null;
            }

            result = new SaveResult(SaveResultKind.Deleted, "deleted_by_address");
            return true;
        }

        public bool TrySetCurrent(
            SaveCurrentState state,
            string reason,
            out string error)
        {
            _ = reason;
            if (!TryValidateCurrentState(state, out error))
            {
                return false;
            }

            _currentState = CloneCurrentState(state);
            error = "current_state_set";
            return true;
        }

        private bool TryLoadByIdentity(
            SaveIdentity identity,
            bool shouldUpdateCurrentState,
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

            if (shouldUpdateCurrentState)
            {
                _currentState = BuildCurrentState(record);
            }

            return true;
        }

        private bool TrySaveRecord(
            SaveRecord record,
            bool shouldUpdateCurrentState,
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

            if (shouldUpdateCurrentState)
            {
                _currentState = BuildCurrentState(record);
            }

            return true;
        }

        private static bool ShouldUpdateCurrentState(SaveAddress address)
        {
            return address != null && address.Scope != SaveScope.Preferences;
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

        private static bool TryValidateCurrentState(SaveCurrentState state, out string reason)
        {
            if (state == null)
            {
                reason = "current_state_null";
                return false;
            }

            if (!state.IsValid)
            {
                reason = "current_state_invalid";
                return false;
            }

            reason = "current_state_ok";
            return true;
        }

        private static SaveCurrentState BuildCurrentState(SaveRecord record)
        {
            string currentSnapshotId = string.Empty;
            if (record?.Entries != null &&
                record.Entries.TryGetValue(AddressRecordEntryKey, out string recordId))
            {
                currentSnapshotId = recordId;
            }

            return new SaveCurrentState(
                record.Identity.ProfileId,
                record.Identity.SlotId,
                record.SchemaVersion,
                record.Revision,
                record.SavedAtUtc,
                currentSnapshotId);
        }

        private static SaveCurrentState CloneCurrentState(SaveCurrentState state)
        {
            return new SaveCurrentState(
                state.ProfileId,
                state.SlotId,
                state.SchemaVersion,
                state.Revision,
                state.SavedAtUtc,
                state.CurrentSnapshotId);
        }

        private bool TryResolveProfileIdForAddress(SaveAddress address, out string profileId, out string reason)
        {
            if (address is { Scope: SaveScope.Preferences } && !string.IsNullOrWhiteSpace(address.OwnerId))
            {
                profileId = address.OwnerId.Trim();
                reason = "profile_id_from_preferences_address";
                return true;
            }

            return TryResolveProfileIdFromCurrent(out profileId, out reason);
        }

        private bool TryResolveProfileIdFromCurrent(out string profileId, out string reason)
        {
            if (!HasCurrent || CurrentState == null)
            {
                profileId = string.Empty;
                reason = "profile_id_missing_current_state";
                return false;
            }

            profileId = CurrentState.ProfileId;
            if (string.IsNullOrWhiteSpace(profileId))
            {
                profileId = string.Empty;
                reason = "profile_id_missing_current_state_profile";
                return false;
            }

            reason = "profile_id_from_current_state";
            return true;
        }

        private bool TryResolveProfileIdForRequest(SaveRequest request, out string profileId, out string reason)
        {
            if (!string.IsNullOrWhiteSpace(request?.ProfileId))
            {
                profileId = request.ProfileId;
                reason = "profile_id_from_request";
                return true;
            }

            return TryResolveProfileIdForAddress(request?.Address, out profileId, out reason);
        }

        private static bool TryValidateAddress(SaveAddress address, out string reason)
        {
            if (address == null)
            {
                reason = "address_null";
                return false;
            }

            if (address.Scope == SaveScope.Unknown)
            {
                reason = "address_scope_unknown";
                return false;
            }

            if (address.Group == SaveGroup.Unknown)
            {
                reason = "address_group_unknown";
                return false;
            }

            if (string.IsNullOrWhiteSpace(address.OwnerId) ||
                string.IsNullOrWhiteSpace(address.RecordId) ||
                string.IsNullOrWhiteSpace(address.SlotId) ||
                string.IsNullOrWhiteSpace(address.SchemaId) ||
                address.SchemaVersion <= 0)
            {
                reason = "address_invalid";
                return false;
            }

            reason = "address_ok";
            return true;
        }

        private static Dictionary<string, string> BuildEntriesWithAddressMetadata(SaveRequest request, string profileId)
        {
            Dictionary<string, string> entries = new(StringComparer.Ordinal);

            if (request.Entries != null)
            {
                foreach (KeyValuePair<string, string> pair in request.Entries)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                    {
                        continue;
                    }

                    entries[pair.Key.Trim()] = pair.Value ?? string.Empty;
                }
            }

            entries[AddressScopeEntryKey] = request.Address.Scope.ToString();
            entries[AddressGroupEntryKey] = request.Address.Group.ToString();
            entries[AddressOwnerEntryKey] = request.Address.OwnerId;
            entries[AddressRecordEntryKey] = request.Address.RecordId;
            entries[AddressSlotEntryKey] = request.Address.SlotId;
            entries[AddressSchemaIdEntryKey] = request.Address.SchemaId;
            entries[AddressSchemaVersionEntryKey] = request.Address.SchemaVersion.ToString();
            entries[AddressProfileEntryKey] = profileId;

            return entries;
        }

        private static bool TryValidateAddressEntries(SaveRecord record, SaveAddress address, out string reason)
        {
            if (record?.Entries == null)
            {
                reason = "entries_missing";
                return false;
            }

            if (!record.Entries.TryGetValue(AddressScopeEntryKey, out string scopeText) ||
                !string.Equals(scopeText, address.Scope.ToString(), StringComparison.Ordinal))
            {
                reason = "scope_mismatch";
                return false;
            }

            if (!record.Entries.TryGetValue(AddressGroupEntryKey, out string groupText) ||
                !string.Equals(groupText, address.Group.ToString(), StringComparison.Ordinal))
            {
                reason = "group_mismatch";
                return false;
            }

            if (!record.Entries.TryGetValue(AddressOwnerEntryKey, out string ownerId) ||
                !string.Equals(ownerId, address.OwnerId, StringComparison.Ordinal))
            {
                reason = "owner_id_mismatch";
                return false;
            }

            if (!record.Entries.TryGetValue(AddressRecordEntryKey, out string recordId) ||
                !string.Equals(recordId, address.RecordId, StringComparison.Ordinal))
            {
                reason = "record_id_mismatch";
                return false;
            }

            if (!record.Entries.TryGetValue(AddressSlotEntryKey, out string slotId) ||
                !string.Equals(slotId, address.SlotId, StringComparison.Ordinal))
            {
                reason = "slot_id_mismatch";
                return false;
            }

            if (!record.Entries.TryGetValue(AddressSchemaIdEntryKey, out string schemaId) ||
                !string.Equals(schemaId, address.SchemaId, StringComparison.Ordinal))
            {
                reason = "schema_id_mismatch";
                return false;
            }

            if (!record.Entries.TryGetValue(AddressSchemaVersionEntryKey, out string schemaVersionText) ||
                !int.TryParse(schemaVersionText, out int schemaVersion) ||
                schemaVersion != address.SchemaVersion)
            {
                reason = "schema_version_mismatch";
                return false;
            }

            reason = "address_entries_ok";
            return true;
        }

        private static SaveResult BuildResult(SaveResultKind kind, string reason, SaveRecord record)
        {
            return new SaveResult(
                kind,
                reason,
                record?.Entries,
                record?.SchemaVersion ?? 0,
                record?.Revision ?? 0,
                record?.SavedAtUtc);
        }
    }
}
