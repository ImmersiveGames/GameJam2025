using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class DefaultProgressionSlotContextResolver : IProgressionSlotContextResolver
    {
        public bool TryResolveForRouteActivitySave(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            out ProgressionSlotContext slotContext,
            out string failureReason)
        {
            slotContext = null;

            if (!DependencyManager.Provider.TryGetGlobal<ISaveStateService>(out var saveStateService) ||
                saveStateService == null)
            {
                failureReason = "save_state_service_missing";
                return false;
            }

            if (!saveStateService.HasCurrent)
            {
                failureReason = "current_save_missing";
                return false;
            }

            var currentState = saveStateService.CurrentState;
            if (currentState == null || !currentState.IsValid)
            {
                failureReason = "current_state_invalid";
                return false;
            }

            string currentSnapshotId = currentState.CurrentSnapshotId.TrimToEmpty();
            string snapshotPointerSource = string.IsNullOrWhiteSpace(currentSnapshotId)
                ? "revision_fallback"
                : "current_snapshot_id";

            try
            {
                var slotId = new SaveSlotId(currentState.SlotId);
                string snapshotPointer = string.IsNullOrWhiteSpace(currentSnapshotId)
                    ? $"snapshot-rev-{currentState.Revision}"
                    : currentSnapshotId;
                var snapshotId = new SaveSnapshotId(snapshotPointer);
                slotContext = new ProgressionSlotContext(
                    currentState.ProfileId,
                    slotId,
                    SaveSlotKind.Current,
                    snapshotId);
            }
            catch (Exception ex)
            {
                failureReason = $"slot_context_invalid:{ex.GetType().Name}";
                return false;
            }

            if (!slotContext.IsValid)
            {
                failureReason = "slot_context_invalid";
                slotContext = null;
                return false;
            }

            DebugUtility.LogVerbose(typeof(DefaultProgressionSlotContextResolver),
                $"ProgressionSlotContextResolved routeIdentity='{routeIdentity.TrimToEmpty()}' routeOperationId='{routeOperationId.TrimToEmpty()}' transitionId='{transitionId.TrimToEmpty()}' routeSequence='{routeSequence}' profileId='{slotContext.ProfileId}' slotId='{slotContext.SlotId}' slotKind='{slotContext.SlotKind}' snapshotId='{slotContext.SnapshotId}' snapshotPointerSource='{snapshotPointerSource}' currentSnapshotIdRaw='{currentSnapshotId.TrimToEmpty()}' currentRevision='{currentState.Revision}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Info);

            failureReason = "resolved";
            return true;
        }
    }
}
