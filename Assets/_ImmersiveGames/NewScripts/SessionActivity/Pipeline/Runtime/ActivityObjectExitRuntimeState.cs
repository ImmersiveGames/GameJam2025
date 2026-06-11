using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityObjectExitRuntimeState
    {
        private ActivityObjectContributorDiscoveryResult _currentContributorDiscoveryResult;
        private ActivityCapabilityInventory _currentInventoryPreview;
        private ActivityCapabilityInventoryValidationResult _currentInventoryPreviewValidation;
        private SessionActivitySnapshotPayload _snapshotPayloadForSaveOnExit;
        private bool _lastSnapshotCaptureFailedForSaveOnExit;
        private string _lastSnapshotCaptureFailureDetail;

        public ActivityObjectContributorDiscoveryResult CurrentContributorDiscoveryResult => _currentContributorDiscoveryResult;
        public ActivityCapabilityInventory CurrentInventoryPreview => _currentInventoryPreview;
        public ActivityCapabilityInventoryValidationResult CurrentInventoryPreviewValidation => _currentInventoryPreviewValidation;
        public SessionActivitySnapshotPayload SnapshotPayloadForSaveOnExit => _snapshotPayloadForSaveOnExit;

        public bool HasContributorDiscoveryResult => _currentContributorDiscoveryResult.IsValid;
        public bool HasInventoryPreview => _currentInventoryPreview.IsValid;
        public bool HasInventoryPreviewValidation => _currentInventoryPreviewValidation.IsValid;
        public bool HasSnapshotPayloadForSaveOnExit => _snapshotPayloadForSaveOnExit.IsValid;

        public void StoreContributorDiscoveryResult(
            ActivityObjectContributorDiscoveryResult result,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool hasDiscoveryBefore = HasContributorDiscoveryResult;
            _currentContributorDiscoveryResult = result;
            LogStateChanged(
                "ActivityObjectExitRuntimeStateContributorDiscoveryStored",
                activityId,
                entrySequence,
                hasDiscoveryBefore,
                HasContributorDiscoveryResult,
                HasInventoryPreview,
                HasInventoryPreview,
                HasInventoryPreviewValidation,
                HasInventoryPreviewValidation,
                HasSnapshotPayloadForSaveOnExit,
                HasSnapshotPayloadForSaveOnExit,
                result.IsValid ? result.Reports.Count : 0,
                CurrentInventoryCapabilityCount(),
                0,
                source,
                reason);
        }

        public void ClearContributorDiscoveryResult(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool hasDiscoveryBefore = HasContributorDiscoveryResult;
            int discoveredCount = _currentContributorDiscoveryResult.IsValid ? _currentContributorDiscoveryResult.Reports.Count : 0;
            _currentContributorDiscoveryResult = default;
            LogStateChanged(
                "ActivityObjectExitRuntimeStateContributorDiscoveryCleared",
                activityId,
                entrySequence,
                hasDiscoveryBefore,
                HasContributorDiscoveryResult,
                HasInventoryPreview,
                HasInventoryPreview,
                HasInventoryPreviewValidation,
                HasInventoryPreviewValidation,
                HasSnapshotPayloadForSaveOnExit,
                HasSnapshotPayloadForSaveOnExit,
                discoveredCount,
                CurrentInventoryCapabilityCount(),
                0,
                source,
                reason);
        }

        public void StoreInventoryPreview(
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool hasInventoryBefore = HasInventoryPreview;
            bool hasValidationBefore = HasInventoryPreviewValidation;
            _currentInventoryPreview = inventory;
            _currentInventoryPreviewValidation = validation;
            LogStateChanged(
                "ActivityObjectExitRuntimeStateInventoryPreviewStored",
                activityId,
                entrySequence,
                HasContributorDiscoveryResult,
                HasContributorDiscoveryResult,
                hasInventoryBefore,
                HasInventoryPreview,
                hasValidationBefore,
                HasInventoryPreviewValidation,
                HasSnapshotPayloadForSaveOnExit,
                HasSnapshotPayloadForSaveOnExit,
                CurrentDiscoveryCount(),
                CurrentInventoryCapabilityCount(),
                0,
                source,
                reason);
        }

        public void ClearInventoryState(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool hasInventoryBefore = HasInventoryPreview;
            bool hasValidationBefore = HasInventoryPreviewValidation;
            int capabilityCount = CurrentInventoryCapabilityCount();
            _currentInventoryPreview = default;
            _currentInventoryPreviewValidation = default;
            LogStateChanged(
                "ActivityObjectExitRuntimeStateInventoryCleared",
                activityId,
                entrySequence,
                HasContributorDiscoveryResult,
                HasContributorDiscoveryResult,
                hasInventoryBefore,
                HasInventoryPreview,
                hasValidationBefore,
                HasInventoryPreviewValidation,
                HasSnapshotPayloadForSaveOnExit,
                HasSnapshotPayloadForSaveOnExit,
                CurrentDiscoveryCount(),
                capabilityCount,
                0,
                source,
                reason);
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
            bool hasSnapshotBefore = HasSnapshotPayloadForSaveOnExit;
            _snapshotPayloadForSaveOnExit = payload;
            _lastSnapshotCaptureFailedForSaveOnExit = captureFailed;
            _lastSnapshotCaptureFailureDetail = string.IsNullOrWhiteSpace(failureDetail) ? string.Empty : failureDetail.Trim();
            LogStateChanged(
                "ActivityObjectExitRuntimeStateSnapshotPayloadStored",
                activityId,
                entrySequence,
                HasContributorDiscoveryResult,
                HasContributorDiscoveryResult,
                HasInventoryPreview,
                HasInventoryPreview,
                HasInventoryPreviewValidation,
                HasInventoryPreviewValidation,
                hasSnapshotBefore,
                HasSnapshotPayloadForSaveOnExit,
                CurrentDiscoveryCount(),
                CurrentInventoryCapabilityCount(),
                HasSnapshotPayloadForSaveOnExit ? 1 : 0,
                source,
                reason);
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
            bool hasSnapshotBefore = HasSnapshotPayloadForSaveOnExit;
            if (!HasSnapshotPayloadForSaveOnExit)
            {
                if (_lastSnapshotCaptureFailedForSaveOnExit)
                {
                    string detail = string.IsNullOrWhiteSpace(_lastSnapshotCaptureFailureDetail)
                        ? "snapshot_capture_failed"
                        : Normalize(_lastSnapshotCaptureFailureDetail);
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

                LogStateChanged(
                    "ActivityObjectExitRuntimeStateSnapshotPayloadRead",
                    activityId,
                    entrySequence,
                    HasContributorDiscoveryResult,
                    HasContributorDiscoveryResult,
                    HasInventoryPreview,
                    HasInventoryPreview,
                    HasInventoryPreviewValidation,
                    HasInventoryPreviewValidation,
                    hasSnapshotBefore,
                    HasSnapshotPayloadForSaveOnExit,
                    CurrentDiscoveryCount(),
                    CurrentInventoryCapabilityCount(),
                    0,
                    source,
                    failureReason);
                return false;
            }

            payload = _snapshotPayloadForSaveOnExit;
            failureReason = "snapshot_payload_resolved";
            LogStateChanged(
                "ActivityObjectExitRuntimeStateSnapshotPayloadRead",
                activityId,
                entrySequence,
                HasContributorDiscoveryResult,
                HasContributorDiscoveryResult,
                HasInventoryPreview,
                HasInventoryPreview,
                HasInventoryPreviewValidation,
                HasInventoryPreviewValidation,
                hasSnapshotBefore,
                HasSnapshotPayloadForSaveOnExit,
                CurrentDiscoveryCount(),
                CurrentInventoryCapabilityCount(),
                1,
                source,
                reason);
            return true;
        }

        public void ClearSnapshotPayloadForSaveOnExit(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool hasSnapshotBefore = HasSnapshotPayloadForSaveOnExit;
            _snapshotPayloadForSaveOnExit = default;
            _lastSnapshotCaptureFailedForSaveOnExit = false;
            _lastSnapshotCaptureFailureDetail = string.Empty;
            LogStateChanged(
                "ActivityObjectExitRuntimeStateSnapshotPayloadCleared",
                activityId,
                entrySequence,
                HasContributorDiscoveryResult,
                HasContributorDiscoveryResult,
                HasInventoryPreview,
                HasInventoryPreview,
                HasInventoryPreviewValidation,
                HasInventoryPreviewValidation,
                hasSnapshotBefore,
                HasSnapshotPayloadForSaveOnExit,
                CurrentDiscoveryCount(),
                CurrentInventoryCapabilityCount(),
                0,
                source,
                reason);
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

        private int CurrentDiscoveryCount()
        {
            return _currentContributorDiscoveryResult.IsValid ? _currentContributorDiscoveryResult.Reports.Count : 0;
        }

        private int CurrentInventoryCapabilityCount()
        {
            return _currentInventoryPreview.IsValid ? _currentInventoryPreview.Capabilities.Count : 0;
        }

        private static void LogStateChanged(
            string eventName,
            string activityId,
            int entrySequence,
            bool hasDiscoveryBefore,
            bool hasDiscoveryAfter,
            bool hasInventoryBefore,
            bool hasInventoryAfter,
            bool hasValidationBefore,
            bool hasValidationAfter,
            bool hasSnapshotBefore,
            bool hasSnapshotAfter,
            int discoveredCount,
            int inventoryCapabilityCount,
            int snapshotObjectCount,
            string source,
            string reason)
        {
            DebugUtility.Log(
                typeof(ActivityObjectExitRuntimeState),
                $"[OBS][ActivityObjectExitRuntimeState] event='{Normalize(eventName)}' owner='ActivityObjectExitRuntimeState' activityId='{Normalize(activityId)}' entrySequence='{entrySequence}' hasDiscoveryBefore='{ToLowerInvariant(hasDiscoveryBefore)}' hasDiscoveryAfter='{ToLowerInvariant(hasDiscoveryAfter)}' hasInventoryPreviewBefore='{ToLowerInvariant(hasInventoryBefore)}' hasInventoryPreviewAfter='{ToLowerInvariant(hasInventoryAfter)}' hasInventoryValidationBefore='{ToLowerInvariant(hasValidationBefore)}' hasInventoryValidationAfter='{ToLowerInvariant(hasValidationAfter)}' hasSnapshotPayloadBefore='{ToLowerInvariant(hasSnapshotBefore)}' hasSnapshotPayloadAfter='{ToLowerInvariant(hasSnapshotAfter)}' discoveredCount='{discoveredCount}' inventoryCapabilityCount='{inventoryCapabilityCount}' snapshotObjectCount='{snapshotObjectCount}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string ToLowerInvariant(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
