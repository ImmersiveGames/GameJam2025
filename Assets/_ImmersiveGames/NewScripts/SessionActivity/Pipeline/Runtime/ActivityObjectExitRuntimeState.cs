using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityObjectExitRuntimeState
    {
        private ActivityObjectContributorDiscoveryResult _currentContributorDiscoveryResult;
        private ActivityCapabilityInventory _currentInventoryPreview;
        private SessionActivitySnapshotPayload _snapshotPayloadForSaveOnExit;
        private bool _lastSnapshotCaptureFailedForSaveOnExit;
        private string _lastSnapshotCaptureFailureDetail;

        public ActivityObjectContributorDiscoveryResult CurrentContributorDiscoveryResult => _currentContributorDiscoveryResult;
        public ActivityCapabilityInventory CurrentInventoryPreview => _currentInventoryPreview;
        public SessionActivitySnapshotPayload SnapshotPayloadForSaveOnExit => _snapshotPayloadForSaveOnExit;

        public bool HasContributorDiscoveryResult => _currentContributorDiscoveryResult.IsValid;
        public bool HasInventoryPreview => _currentInventoryPreview.IsValid;
        public bool HasSnapshotPayloadForSaveOnExit => _snapshotPayloadForSaveOnExit.IsValid;

        public void StoreContributorDiscoveryResult(
            ActivityObjectContributorDiscoveryResult result,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _currentContributorDiscoveryResult = result;
        }

        public void ClearContributorDiscoveryResult(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _currentContributorDiscoveryResult = default;
        }

        public void StoreInventoryPreview(
            ActivityCapabilityInventory inventory,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _currentInventoryPreview = inventory;
        }

        public void ClearInventoryState(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _currentInventoryPreview = default;
        }

        public void SetSnapshotPayloadForSaveOnExit(
            SessionActivitySnapshotPayload payload,
            bool captureFailed,
            string failureDetail,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _snapshotPayloadForSaveOnExit = payload;
            _lastSnapshotCaptureFailedForSaveOnExit = captureFailed;
            _lastSnapshotCaptureFailureDetail = failureDetail.TrimToEmpty();
        }

        public bool TryGetSnapshotPayloadForSaveOnExit(
            out SessionActivitySnapshotPayload payload,
            out string failureReason,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            payload = default;
            if (!HasSnapshotPayloadForSaveOnExit)
            {
                if (_lastSnapshotCaptureFailedForSaveOnExit)
                {
                    string detail = string.IsNullOrWhiteSpace(_lastSnapshotCaptureFailureDetail)
                        ? "snapshot_capture_failed"
                        : _lastSnapshotCaptureFailureDetail.TrimToEmpty();
                    failureReason = $"snapshot_capture_failed:{detail}";
                }
                else if (!HasContributorDiscoveryResult || _currentContributorDiscoveryResult.Reports.Count == 0)
                {
                    failureReason = "no_activity_content_contributors";
                }
                else
                {
                    failureReason = "exit_correlation_snapshot_payload_expected_but_missing";
                }
                return false;
            }

            payload = _snapshotPayloadForSaveOnExit;
            failureReason = "snapshot_payload_resolved";
            return true;
        }

        public void ClearSnapshotPayloadForSaveOnExit(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _snapshotPayloadForSaveOnExit = default;
            _lastSnapshotCaptureFailedForSaveOnExit = false;
            _lastSnapshotCaptureFailureDetail = string.Empty;
        }

        public void ClearAll(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            ClearContributorDiscoveryResult(activityId, entrySequence, source, reason);
            ClearInventoryState(activityId, entrySequence, source, reason);
            ClearSnapshotPayloadForSaveOnExit(activityId, entrySequence, source, reason);
        }
    }
}
