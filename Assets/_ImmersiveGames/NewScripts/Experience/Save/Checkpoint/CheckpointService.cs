using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Save.Contracts;
using _ImmersiveGames.NewScripts.Experience.Save.Models;
namespace _ImmersiveGames.NewScripts.Experience.Save.Checkpoint
{
    public sealed class CheckpointService : ICheckpointService
    {
        private readonly ICheckpointBackend _backend;
        private readonly CheckpointIdentity _requiredIdentity;
        private CheckpointSnapshot _currentSnapshot;

        public CheckpointService(
            CheckpointIdentity requiredIdentity,
            ICheckpointBackend backend)
        {
            _requiredIdentity = requiredIdentity ?? throw new ArgumentNullException(nameof(requiredIdentity));
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public string BackendId => _backend.BackendId;

        public bool IsBackendAvailable => _backend.IsBackendAvailable;

        public CheckpointIdentity RequiredIdentity => _requiredIdentity;

        public bool HasSnapshot => _currentSnapshot != null;

        public CheckpointSnapshot CurrentSnapshot =>
            _currentSnapshot ?? throw new InvalidOperationException("[FATAL][Save] Current checkpoint snapshot requested before initialization.");

        public void SetCurrent(
            CheckpointSnapshot snapshot,
            string reason)
        {
            _currentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

            EnsureMatchesRequiredIdentity(snapshot.Identity);

            DebugUtility.LogVerbose<CheckpointService>(
                $"[OBS][Save] CheckpointCurrentSet reason='{NormalizeReason(reason)}' snapshot={snapshot}.",
                DebugUtility.Colors.Info);
        }

        public bool TryLoad(
            CheckpointIdentity identity,
            out CheckpointSnapshot snapshot,
            out string reason)
        {
            if (!MatchesRequiredIdentity(identity))
            {
                snapshot = null;
                reason = "identity_mismatch";

                DebugUtility.LogWarning<CheckpointService>(
                    $"[OBS][Save] CheckpointLoad backend='{BackendId}' decision='no_op' reason='{reason}' required='{RequiredIdentity}' requested='{NormalizeIdentity(identity)}'.");
                return false;
            }

            bool loaded = _backend.TryLoad(identity, out snapshot, out reason);

            DebugUtility.Log(typeof(CheckpointService),
                $"[OBS][Save] CheckpointLoad backend='{BackendId}' decision='{(loaded ? "load" : "no_op")}' identity='{NormalizeIdentity(identity)}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            if (loaded && snapshot != null)
            {
                _currentSnapshot = snapshot;
            }

            return loaded;
        }

        public bool TrySaveCurrent(out string reason)
        {
            if (!HasSnapshot)
            {
                reason = "missing_current_snapshot";
                DebugUtility.LogWarning<CheckpointService>(
                    $"[OBS][Save] CheckpointSave origin='Current' decision='no_op' reason='{reason}'.");
                return false;
            }

            return TrySave(CurrentSnapshot, out reason);
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

            if (!MatchesRequiredIdentity(snapshot.Identity))
            {
                reason = "identity_mismatch";

                DebugUtility.LogWarning<CheckpointService>(
                    $"[OBS][Save] CheckpointSave backend='{BackendId}' decision='no_op' reason='{reason}' required='{RequiredIdentity}' snapshot={snapshot}.");
                return false;
            }

            bool saved = _backend.TrySave(snapshot, out reason);

            DebugUtility.Log(typeof(CheckpointService),
                $"[OBS][Save] CheckpointSave backend='{BackendId}' decision='{(saved ? "save" : "no_op")}' identity={snapshot} reason='{reason}'.",
                DebugUtility.Colors.Info);

            return saved;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        }

        private static string NormalizeIdentity(CheckpointIdentity identity)
        {
            return identity == null ? "n/a" : identity.ToString();
        }

        private bool MatchesRequiredIdentity(CheckpointIdentity identity)
        {
            if (identity == null)
            {
                return false;
            }

            return string.Equals(identity.CheckpointId, _requiredIdentity.CheckpointId, StringComparison.Ordinal)
                && string.Equals(identity.ProfileId, _requiredIdentity.ProfileId, StringComparison.Ordinal)
                && string.Equals(identity.SlotId, _requiredIdentity.SlotId, StringComparison.Ordinal);
        }

        private void EnsureMatchesRequiredIdentity(CheckpointIdentity identity)
        {
            if (!MatchesRequiredIdentity(identity))
            {
                throw new InvalidOperationException($"[FATAL][Save] Checkpoint identity mismatch. required='{RequiredIdentity}' received='{NormalizeIdentity(identity)}'.");
            }
        }
    }
}
