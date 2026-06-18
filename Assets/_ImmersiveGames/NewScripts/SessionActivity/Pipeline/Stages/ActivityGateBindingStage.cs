using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityGateBindingStage
    {
        public static ActivityEntryPermissionTargetPreparationResult Execute(
            ActivityEntryPermissionTargetPreparationCommand command,
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            IActivityEntryPermissionTargetRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPermissionTargetPreparationCommand is invalid.");
            }

            identityBridge = identityBridge ?? throw new ArgumentNullException(nameof(identityBridge));
            factBridge = factBridge ?? throw new ArgumentNullException(nameof(factBridge));
            bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            var startedIdentity = BuildIdentity(command, SessionActivityStage.ActivitySetupStarted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.PermissionTargetPreparationStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' gate binding started requiredReceivers='{command.RequireReceivers}' registerReceivers='{command.RegisterReceivers}' contributionCount='{command.PermissionReceiverContributions.Count}'.");
            factBridge.EmitSnapshot(
                snapshots,
                "gate_binding_started",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' gate binding started requiredReceivers='{command.RequireReceivers}' registerReceivers='{command.RegisterReceivers}' contributionCount='{command.PermissionReceiverContributions.Count}'.");

            bridge.BeginPermissionScope(startedIdentity);

            if (command.PermissionReceiverContributions.Count == 0)
            {
                bridge.ReplacePermissionReceivers(Array.Empty<ActivityCapabilityPermissionReceiverReference>());
                if (command.RequireReceivers)
                {
                    throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][GateBinding] Missing permission receiver contributions activityId='{command.ActivityId}' entrySequence='{startedIdentity.EntrySequence}'.");
                }

                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.PermissionTargetPreparationSkippedNoReceivers,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' gate binding skipped because no permission receiver contributions were discovered.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "gate_binding_skipped_no_receivers",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' gate binding skipped because no permission receiver contributions were discovered.");
                return new ActivityEntryPermissionTargetPreparationResult(
                    true,
                    startedIdentity,
                    0,
                    true,
                    "no_permission_receiver_contributions");
            }

            List<ActivityCapabilityPermissionReceiverReference> receivers = new(command.PermissionReceiverContributions.Count);
            for (int index = 0; index < command.PermissionReceiverContributions.Count; index++)
            {
                var contribution = command.PermissionReceiverContributions[index];
                if (!contribution.IsValid)
                {
                    throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][GateBinding] Invalid permission receiver contribution index='{index}' activityId='{command.ActivityId}' entrySequence='{startedIdentity.EntrySequence}'.");
                }

                if (!IsSamePermissionScope(startedIdentity, contribution.ReceiverIdentity))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][ActivityEntryPipeline][GateBinding] permission_receiver_contribution_identity_mismatch activityId='{command.ActivityId}' entrySequence='{startedIdentity.EntrySequence}' receiverId='{contribution.ReceiverId}' contributionIdentity='{contribution.Identity}' receiverIdentity='{contribution.ReceiverIdentity}'.");
                }

                if (!contribution.Provider.TryCreateReceiver(out var receiver) || receiver == null)
                {
                    if (command.RequireReceivers)
                    {
                        throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][GateBinding] Missing permission receiver implementation for required contribution capabilityId='{contribution.CapabilityId}' activityId='{command.ActivityId}' entrySequence='{startedIdentity.EntrySequence}'.");
                    }

                    continue;
                }

                receivers.Add(new ActivityCapabilityPermissionReceiverReference(
                    contribution.CapabilityId,
                    contribution.OwnerId,
                    contribution.ActorId,
                    contribution.ActorInstanceRuntimeId,
                    contribution.PlayerActorId,
                    contribution.PlayerSlotId,
                    contribution.ComponentPath,
                    contribution.PermissionId,
                    contribution.ReceiverIdentity,
                    receiver));

                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.PermissionTargetReceiverResolved,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' permission receiver resolved receiverId='{contribution.ReceiverId}' actorId='{contribution.ActorId}' actorInstanceRuntimeId='{contribution.ActorInstanceRuntimeId}' playerActorId='{contribution.PlayerActorId}' playerSlotId='{contribution.PlayerSlotId}'.");
            }

            if (command.RegisterReceivers)
            {
                bridge.ReplacePermissionReceivers(receivers);
            }

            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.PermissionTargetPreparationCompleted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' gate binding completed receivers='{receivers.Count}' registered='{command.RegisterReceivers}'.");
            factBridge.EmitSnapshot(
                snapshots,
                "gate_binding_completed",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' gate binding completed receivers='{receivers.Count}' registered='{command.RegisterReceivers}'.");

            return new ActivityEntryPermissionTargetPreparationResult(
                true,
                startedIdentity,
                receivers.Count,
                false,
                command.RegisterReceivers ? "receivers_registered" : "receivers_resolved");
        }

        private static SessionActivityIdentity BuildIdentity(
            ActivityEntryPermissionTargetPreparationCommand command,
            SessionActivityStage stage)
        {
            return new SessionActivityIdentity(
                command.Identity.PipelineId,
                command.Identity.SessionId,
                command.ActivityId,
                command.ActivityOrdinal,
                command.Identity.EntrySequence,
                stage,
                command.Source);
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
