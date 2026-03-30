using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Save.Contracts;

namespace _ImmersiveGames.NewScripts.Modules.Save.Runtime
{
    public sealed class ProgressionService : IProgressionStateService, IProgressionSaveService
    {
        private readonly IProgressionBackend _backend;
        private ProgressionSnapshot _currentSnapshot;

        public ProgressionService(IProgressionBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public string BackendId => _backend.BackendId;

        public bool IsBackendAvailable => _backend.IsBackendAvailable;

        public bool HasSnapshot => _currentSnapshot != null;

        public ProgressionSnapshot CurrentSnapshot =>
            _currentSnapshot ?? throw new InvalidOperationException("[FATAL][Save] Current progression snapshot requested before initialization.");

        public void SetCurrent(ProgressionSnapshot snapshot, string reason)
        {
            _currentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

            DebugUtility.LogVerbose<ProgressionService>(
                $"[OBS][Save] ProgressionCurrentSet reason='{NormalizeReason(reason)}' identity={snapshot}.",
                DebugUtility.Colors.Info);
        }

        public bool TryLoad(
            string profileId,
            string slotId,
            out ProgressionSnapshot snapshot,
            out string reason)
        {
            bool loaded = _backend.TryLoad(profileId, slotId, out snapshot, out reason);

            DebugUtility.Log(typeof(ProgressionService),
                $"[OBS][Save] ProgressionLoad backend='{BackendId}' decision='{(loaded ? "load" : "no_op")}' identity='{NormalizeIdentity(profileId)}/{NormalizeIdentity(slotId)}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return loaded;
        }

        public bool TrySaveCurrent(out string reason)
        {
            if (!HasSnapshot)
            {
                reason = "missing_current_snapshot";
                DebugUtility.LogWarning<ProgressionService>(
                    $"[OBS][Save] ProgressionSave origin='Current' decision='no_op' reason='{reason}'.");
                return false;
            }

            return TrySave(CurrentSnapshot, out reason);
        }

        public bool TrySave(
            ProgressionSnapshot snapshot,
            out string reason)
        {
            bool saved = _backend.TrySave(snapshot, out reason);

            DebugUtility.Log(typeof(ProgressionService),
                $"[OBS][Save] ProgressionSave backend='{BackendId}' decision='{(saved ? "save" : "no_op")}' identity={snapshot} reason='{reason}'.",
                DebugUtility.Colors.Info);

            return saved;
        }

        private static string NormalizeIdentity(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "n/a" : value.Trim();
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        }
    }
}
