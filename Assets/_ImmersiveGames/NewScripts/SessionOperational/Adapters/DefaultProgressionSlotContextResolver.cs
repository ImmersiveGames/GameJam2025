using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;

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

            try
            {
                var slotId = new SaveSlotId(currentState.SlotId);
                string currentSnapshotId = Normalize(currentState.CurrentSnapshotId);
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

            DebugUtility.Log(typeof(DefaultProgressionSlotContextResolver),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] ProgressionSlotContextResolved routeIdentity='{Normalize(routeIdentity)}' routeOperationId='{Normalize(routeOperationId)}' transitionId='{Normalize(transitionId)}' routeSequence='{routeSequence}' profileId='{slotContext.ProfileId}' slotId='{slotContext.SlotId}' slotKind='{slotContext.SlotKind}' snapshotId='{slotContext.SnapshotId}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            failureReason = "resolved";
            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
