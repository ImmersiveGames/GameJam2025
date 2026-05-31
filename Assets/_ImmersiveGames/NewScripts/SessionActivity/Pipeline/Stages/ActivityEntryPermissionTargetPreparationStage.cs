using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryPermissionTargetPreparationStage
    {
        public static ActivityEntryPermissionTargetPreparationResult Execute(
            ActivityEntryPermissionTargetPreparationCommand command,
            IActivityEntryRuntimeEndpoint endpoint,
            IActivityEntryPermissionTargetRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPermissionTargetPreparationCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PermissionTargetPreparationStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' permission target preparation started registerReceivers='{command.RegisterReceivers}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "permission_target_preparation_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' permission target preparation started registerReceivers='{command.RegisterReceivers}'.");

            bridge.BeginPermissionScope(startedIdentity);

            ActivityCapabilityInventory inventory = bridge.GetCurrentActivityCapabilityInventoryPreview();
            if (!inventory.IsValid || !inventory.HasCapabilities)
            {
                bridge.ReplacePermissionReceivers(Array.Empty<ActivityCapabilityPermissionReceiverReference>());
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.PermissionTargetPreparationSkippedNoReceivers,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' permission target preparation skipped because capability inventory is empty.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "permission_target_preparation_skipped_no_inventory",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' permission target preparation skipped because capability inventory is empty.");
                return new ActivityEntryPermissionTargetPreparationResult(
                    completed: true,
                    startedIdentity,
                    receiverCount: 0,
                    skipped: true,
                    reason: "no_capability_inventory");
            }

            IReadOnlyList<ActivityCapabilityPermissionReceiverReference> receivers = ResolvePermissionReceivers(inventory, startedIdentity, definition.ActivityId);
            if (receivers.Count == 0)
            {
                bridge.ReplacePermissionReceivers(Array.Empty<ActivityCapabilityPermissionReceiverReference>());
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.PermissionTargetPreparationSkippedNoReceivers,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' permission target preparation skipped because no permission receivers were discovered.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "permission_target_preparation_skipped_no_receivers",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' permission target preparation skipped because no permission receivers were discovered.");
                return new ActivityEntryPermissionTargetPreparationResult(
                    completed: true,
                    startedIdentity,
                    receiverCount: 0,
                    skipped: true,
                    reason: "no_permission_receivers");
            }

            if (command.RegisterReceivers)
            {
                bridge.ReplacePermissionReceivers(receivers);
            }

            for (int index = 0; index < receivers.Count; index++)
            {
                ActivityCapabilityPermissionReceiverReference receiver = receivers[index];
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.PermissionTargetReceiverResolved,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' permission receiver resolved receiverId='{receiver.ReceiverId}' actorId='{receiver.ActorId}' actorInstanceRuntimeId='{receiver.ActorInstanceRuntimeId}' playerActorId='{receiver.PlayerActorId}' playerSlotId='{receiver.PlayerSlotId}'.");
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PermissionTargetPreparationCompleted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' permission target preparation completed receivers='{receivers.Count}' registered='{command.RegisterReceivers}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "permission_target_preparation_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' permission target preparation completed receivers='{receivers.Count}' registered='{command.RegisterReceivers}'.");

            return new ActivityEntryPermissionTargetPreparationResult(
                completed: true,
                startedIdentity,
                receiverCount: receivers.Count,
                skipped: false,
                reason: command.RegisterReceivers ? "receivers_registered" : "receivers_resolved");
        }

        private static IReadOnlyList<ActivityCapabilityPermissionReceiverReference> ResolvePermissionReceivers(
            ActivityCapabilityInventory inventory,
            SessionActivityIdentity activeIdentity,
            string activityId)
        {
            List<ActivityCapabilityPermissionReceiverReference> receivers = new();
            IReadOnlyList<ActivityCapabilityDescriptor> capabilities = inventory.Capabilities ?? Array.Empty<ActivityCapabilityDescriptor>();
            for (int index = 0; index < capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = capabilities[index];
                if (!capability.IsValid || capability.CapabilityKind != ActivityCapabilityKind.PermissionTarget)
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference(capability.CapabilityId, out ActivityCapabilityPermissionReceiverReference runtimeReference) ||
                    runtimeReference is not { IsValid: true })
                {
                    if (capability.Required)
                    {
                        throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][PermissionTarget] Missing runtime reference for required permission target capabilityId='{capability.CapabilityId}' activityId='{activityId}' entrySequence='{activeIdentity.EntrySequence}'.");
                    }

                    continue;
                }

                if (!IsSamePermissionScope(activeIdentity, runtimeReference.Identity))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][ActivityEntryPipeline][PermissionTarget] permission_receiver_identity_mismatch activityId='{activityId}' entrySequence='{activeIdentity.EntrySequence}' receiverId='{runtimeReference.ReceiverId}' receiverIdentity='{runtimeReference.Identity}'.");
                }

                receivers.Add(runtimeReference);
            }

            return receivers;
        }

        private static bool IsSamePermissionScope(
            SessionActivityIdentity identity,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity)
        {
            return identity.IsValid &&
                   receiverIdentity.IsValid &&
                   string.Equals(identity.PipelineId, receiverIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(identity.SessionId, receiverIdentity.SessionStateId, StringComparison.Ordinal) &&
                   string.Equals(identity.ActivityId, receiverIdentity.ActivityId, StringComparison.Ordinal) &&
                   identity.EntrySequence == receiverIdentity.EntrySequence;
        }
    }
}
