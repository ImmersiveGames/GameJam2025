using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityObjectExitCorrelationFreezeStage
    {
        private const string Owner = "ActivityObjectExitCorrelationFreezeStage";
        private const string TechnicalStateOwner = "ActivityObjectExitRuntimeState";
        private const string RefreshReason = "activity_object_exit_correlation_refresh_by_freeze_stage";
        private const string FreezeReason = "activity_object_exit_correlation_frozen_by_freeze_stage";

        public static void Execute(
            SessionActivityIdentity identity,
            ActivityObjectExitCorrelationBundle exitCorrelation,
            ActivityObjectExitRuntimeState runtimeState,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("Activity object exit correlation freeze identity is invalid.");
            }

            runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));

            string activityId = identity.ActivityId;
            int entrySequence = identity.EntrySequence;

            runtimeState.ClearAll(
                activityId,
                entrySequence,
                Owner,
                RefreshReason);

            var discoveryResult = exitCorrelation.ContributorDiscoveryResult;
            var inventoryPreview = exitCorrelation.InventoryPreview;
            var inventoryValidation = exitCorrelation.InventoryPreviewValidation;

            runtimeState.StoreContributorDiscoveryResult(
                discoveryResult,
                activityId,
                entrySequence,
                Owner,
                FreezeReason);
            runtimeState.StoreInventoryPreview(
                inventoryPreview,
                inventoryValidation,
                activityId,
                entrySequence,
                Owner,
                FreezeReason);

            DebugUtility.LogVerbose(
                typeof(ActivityObjectExitCorrelationFreezeStage),
                $"event='ActivityObjectExitCorrelationFrozen' owner='{Owner}' technicalStateOwner='{TechnicalStateOwner}' activityId='{activityId}' entrySequence='{entrySequence}' source='{Normalize(source)}' reason='{Normalize(reason)}' discoveryOwner='{TechnicalStateOwner}' discoveryValid='{discoveryResult.IsValid.ToString().ToLowerInvariant()}' discoveryCount='{(discoveryResult.IsValid ? discoveryResult.Reports.Count : 0)}' inventoryOwner='{TechnicalStateOwner}' inventoryValid='{inventoryPreview.IsValid.ToString().ToLowerInvariant()}' inventoryCapabilityCount='{(inventoryPreview.IsValid ? inventoryPreview.Capabilities.Count : 0)}' inventoryValidationValid='{inventoryValidation.IsValid.ToString().ToLowerInvariant()}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
