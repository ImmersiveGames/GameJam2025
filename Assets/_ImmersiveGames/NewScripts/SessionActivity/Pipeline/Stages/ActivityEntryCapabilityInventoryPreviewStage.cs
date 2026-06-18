using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using static _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages.ActivityEntryObjectSetupStageUtility;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryCapabilityInventoryPreviewStage
    {
        public static ActivityCapabilityInventoryBuildResult Execute(
            ActivityEntryObjectSetupCommand command,
            ActivityObjectContributorDiscoveryResult discoveryResult,
            IReadOnlyList<ActorScanTarget> actorTargets,
            ActivityEntryCapabilityInventoryBuildStage buildStage,
            IActivityEntryRuntimeBridge endpoint,
            ActivityEntryInventoryRuntimeState inventoryState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            int entrySequence = command.Identity.EntrySequence;
            var previewIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActivitySetupStarted);
            endpoint.SetCurrentIdentity(previewIdentity, SessionActivityStage.ActivitySetupStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewStarted,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity capability inventory preview started scannerId='{buildStage.ActivityObjectScannerId}'.");
            bool hasDiscoveryForCurrentEntry = IsDiscoveryResultForCurrentEntryForIdentity(discoveryResult, command.Identity, entrySequence, previewIdentity);
            bool hasActorTargets = actorTargets is { Count: > 0 };
            if (!hasDiscoveryForCurrentEntry && !hasActorTargets)
            {
                inventoryState.ClearCurrentActivityCapabilityInventoryPreview();
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityCapabilityInventoryPreviewSkippedNoDiscovery,
                    previewIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' activity capability inventory preview skipped reason='no_capability_sources' entrySequence='{entrySequence}' scannerId='{buildStage.ActivityObjectScannerId}'.");
                EmitEntryCapabilityInventoryLog(
                    SessionActivityFactKind.ActivityCapabilityInventoryPreviewSkippedNoDiscovery,
                    previewIdentity,
                    $"'{command.Identity.ActivityId}' activity capability inventory preview skipped reason='no_capability_sources' entrySequence='{entrySequence}' scannerId='{buildStage.ActivityObjectScannerId}'.");
                return default;
            }

            var buildResult = buildStage.BuildForEntry(
                previewIdentity,
                hasDiscoveryForCurrentEntry ? discoveryResult : default,
                actorTargets,
                command.Source,
                command.Reason);
            var inventory = buildResult.Inventory;
            inventoryState.SetCurrentActivityCameraBindingContributions(buildResult.CameraBindingContributions);
            inventoryState.SetCurrentActivityAttributeSetupContributions(buildResult.AttributeSetupContributions);
            inventoryState.SetCurrentActivityPresentationSetupContributions(buildResult.PresentationSetupContributions);
            inventoryState.SetCurrentActivityPermissionReceiverContributions(buildResult.PermissionReceiverContributions);
            string capabilityKindsSummary = FormatCapabilityKindsSummary(inventory.Capabilities);
            inventoryState.SetCurrentActivityCapabilityInventoryPreview(inventory);
            string activityObjectLifecycleCapabilityKinds = buildResult.ActivityObjectLifecycleCapabilityKindsSummary;
            string actorLifecycleCapabilityKinds = buildResult.ActorLifecycleCapabilityKindsSummary;

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewObserved,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' activityObjectLifecycleCapabilityCount='{buildResult.ActivityObjectLifecycleCapabilityCount}' activityObjectLifecycleCapabilityKinds='{activityObjectLifecycleCapabilityKinds}' actorLifecycleCapabilityCount='{buildResult.ActorLifecycleCapabilityCount}' actorLifecycleCapabilityKinds='{actorLifecycleCapabilityKinds}' unresolvedReports='{buildResult.UnresolvedReportCount}' scannerId='{buildStage.ActivityObjectScannerId}' actorScannerId='{buildStage.ActorLifecycleScannerId}'.");
            EmitEntryCapabilityInventoryLog(
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewObserved,
                previewIdentity,
                $"'{command.Identity.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' activityObjectLifecycleCapabilityCount='{buildResult.ActivityObjectLifecycleCapabilityCount}' activityObjectLifecycleCapabilityKinds='{activityObjectLifecycleCapabilityKinds}' actorLifecycleCapabilityCount='{buildResult.ActorLifecycleCapabilityCount}' actorLifecycleCapabilityKinds='{actorLifecycleCapabilityKinds}' unresolvedReports='{buildResult.UnresolvedReportCount}' scannerId='{buildStage.ActivityObjectScannerId}' actorScannerId='{buildStage.ActorLifecycleScannerId}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_capability_inventory_preview_observed",
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' activityObjectLifecycleCapabilityCount='{buildResult.ActivityObjectLifecycleCapabilityCount}' activityObjectLifecycleCapabilityKinds='{activityObjectLifecycleCapabilityKinds}' actorLifecycleCapabilityCount='{buildResult.ActorLifecycleCapabilityCount}' actorLifecycleCapabilityKinds='{actorLifecycleCapabilityKinds}' unresolvedReports='{buildResult.UnresolvedReportCount}' scannerId='{buildStage.ActivityObjectScannerId}' actorScannerId='{buildStage.ActorLifecycleScannerId}'.");

            return buildResult;
        }

        private static void EmitEntryCapabilityInventoryLog(
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string message)
        {
            DebugUtility.LogVerbose(typeof(ActivityEntryCapabilityInventoryPreviewStage),
                $"fact='{kind}' stage='{identity.Stage}' entrySequence='{identity.EntrySequence}' activity='{identity.ActivityId}' owner='ActivityEntryCapabilityInventoryPreviewStage' entryPipelineOwner='ActivityEntryPipeline' block='capability_inventory_preview' message=\"{message}\"");
        }
    }
}
