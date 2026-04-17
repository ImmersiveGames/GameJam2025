using System;
using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Save.Contracts;
using ImmersiveGames.GameJam2025.Experience.Save.Models;
namespace ImmersiveGames.GameJam2025.Experience.Save.Progression.Backends
{
    public sealed class InMemoryProgressionBackend : IProgressionBackend
    {
        private readonly Dictionary<(string profileId, string slotId), ProgressionSnapshot> _savedSnapshots = new();

        public string BackendId => "InMemoryProgressionBackend";

        public bool IsBackendAvailable => true;

        public bool TryLoad(
            string profileId,
            string slotId,
            out ProgressionSnapshot snapshot,
            out string reason)
        {
            ValidateIdentity(profileId, nameof(profileId));
            ValidateIdentity(slotId, nameof(slotId));

            if (_savedSnapshots.TryGetValue((profileId.Trim(), slotId.Trim()), out snapshot) && snapshot != null)
            {
                reason = "loaded_from_in_memory_backend";
                DebugUtility.Log(typeof(InMemoryProgressionBackend),
                    $"[OBS][Save] ProgressionBackendLoad backend='{BackendId}' decision='load' identity={snapshot} reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return true;
            }

            snapshot = null;
            reason = "no_saved_data";
            DebugUtility.LogVerbose(typeof(InMemoryProgressionBackend),
                $"[OBS][Save] ProgressionBackendLoad backend='{BackendId}' decision='no_op' profile='{profileId}' slot='{slotId}' reason='{reason}'.",
                DebugUtility.Colors.Info);
            return false;
        }

        public bool TrySave(
            ProgressionSnapshot snapshot,
            out string reason)
        {
            if (snapshot == null)
            {
                reason = "snapshot_null";
                throw new ArgumentNullException(nameof(snapshot));
            }

            _savedSnapshots[(snapshot.ProfileId, snapshot.SlotId)] = snapshot;
            reason = "save_executed";

            DebugUtility.Log(typeof(InMemoryProgressionBackend),
                $"[OBS][Save] ProgressionBackendSave backend='{BackendId}' decision='save' identity={snapshot} reason='{reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private static void ValidateIdentity(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identity is required.", paramName);
            }
        }
    }
}

