using System;
using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Save.Contracts;
using ImmersiveGames.GameJam2025.Experience.Save.Models;
namespace ImmersiveGames.GameJam2025.Experience.Save.Checkpoint.Backends
{
    public sealed class InMemoryCheckpointBackend : ICheckpointBackend
    {
        private readonly Dictionary<(string checkpointId, string profileId, string slotId), CheckpointSnapshot> _savedSnapshots = new();

        public string BackendId => "InMemoryCheckpointBackend";

        public bool IsBackendAvailable => true;

        public bool TryLoad(
            CheckpointIdentity identity,
            out CheckpointSnapshot snapshot,
            out string reason)
        {
            ValidateIdentity(identity);

            var key = (identity.CheckpointId.Trim(), identity.ProfileId.Trim(), identity.SlotId.Trim());
            if (_savedSnapshots.TryGetValue(key, out snapshot) && snapshot != null)
            {
                reason = "loaded_from_in_memory_backend";
                DebugUtility.Log(typeof(InMemoryCheckpointBackend),
                    $"[OBS][Save] CheckpointBackendLoad backend='{BackendId}' decision='load' identity={snapshot} reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            snapshot = null;
            reason = "no_saved_data";
            DebugUtility.LogVerbose(typeof(InMemoryCheckpointBackend),
                $"[OBS][Save] CheckpointBackendLoad backend='{BackendId}' decision='no_op' identity='{identity}' reason='{reason}'.",
                DebugUtility.Colors.Info);
            return false;
        }

        public bool TrySave(
            CheckpointSnapshot snapshot,
            out string reason)
        {
            if (snapshot == null)
            {
                reason = "snapshot_null";
                throw new ArgumentNullException(nameof(snapshot));
            }

            var key = (snapshot.CheckpointId.Trim(), snapshot.ProfileId.Trim(), snapshot.SlotId.Trim());
            _savedSnapshots[key] = snapshot;
            reason = "save_executed";

            DebugUtility.Log(typeof(InMemoryCheckpointBackend),
                $"[OBS][Save] CheckpointBackendSave backend='{BackendId}' decision='save' identity={snapshot} reason='{reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private static void ValidateIdentity(CheckpointIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (string.IsNullOrWhiteSpace(identity.CheckpointId))
            {
                throw new ArgumentException("Identity is required.", nameof(identity));
            }

            if (string.IsNullOrWhiteSpace(identity.ProfileId))
            {
                throw new ArgumentException("Identity is required.", nameof(identity));
            }

            if (string.IsNullOrWhiteSpace(identity.SlotId))
            {
                throw new ArgumentException("Identity is required.", nameof(identity));
            }
        }
    }
}

