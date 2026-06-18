using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class PlayerMovementControlAdapter : IPlayerMovementControlAdapter
    {
        private readonly IActivityCapabilityPermissionRuntime _permissionRuntime;

        public PlayerMovementControlAdapter(IActivityCapabilityPermissionRuntime permissionRuntime)
        {
            _permissionRuntime = permissionRuntime ?? throw new InvalidOperationException("PlayerMovementControlAdapter requires non-null permission runtime.");
        }

        public IReadOnlyList<MovementControlRecord> Execute(
            MovementControlCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("MovementControlCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for movement control.");
            }

            if (!IsSameActivityCycle(command.PipelineIdentity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_movement_control_command: command identity does not match active identity.");
            }

            if (registry == null)
            {
                throw new InvalidOperationException("PlayerMovementControlAdapter requires non-null registry.");
            }

            _permissionRuntime.SetActiveIdentity(
                activeIdentity.PipelineId,
                activeIdentity.SessionId,
                activeIdentity.ActivityId,
                activeIdentity.EntrySequence);

            List<MovementControlRecord> records = new(command.Actors.Count);
            for (int index = 0; index < command.Actors.Count; index++)
            {
                var actor = command.Actors[index];
                if (!actor.IsValid)
                {
                    throw new InvalidOperationException($"Movement control actor identity invalid at index '{index}'.");
                }

                if ((!registry.TryGetActiveHandleByParticipant(actor.ParticipantId, out var handle) ||
                        !handle.IsValid) &&
                    (!registry.TryGetRouteScopedHandleByParticipant(actor.ParticipantId, out handle) ||
                        !handle.IsValid))
                {
                    throw new InvalidOperationException($"Movement control failed: actor not found for playerActorId='{actor.PlayerActorId}'.");
                }

                if (handle.PlayerSlotId != actor.PlayerSlotId || handle.ParticipantId != actor.ParticipantId)
                {
                    throw new InvalidOperationException($"Movement control failed: handle mismatch participantId='{actor.ParticipantId}' playerActorId='{actor.PlayerActorId}' playerSlotId='{actor.PlayerSlotId}'.");
                }

                var actorInstanceRuntimeId = handle.ActorInstanceRuntimeId;
                if (!actorInstanceRuntimeId.IsValid)
                {
                    throw new InvalidOperationException($"Movement control failed: actorId='{actor.ActorId}' participantId='{actor.ParticipantId}' missing ActorInstanceRuntimeId.");
                }

                var capabilitySurface = handle.CapabilitySurface ?? throw new InvalidOperationException($"Movement control failed: actor not found capability surface for playerActorId='{actor.PlayerActorId}'.");
                var movementEndpoint = capabilitySurface.ActorMovementEndpoint ?? throw new InvalidOperationException($"Movement control failed: actor not found movement endpoint for playerActorId='{actor.PlayerActorId}'.");

                var permissionState = command.Enable
                    ? ActivityCapabilityPermissionState.Allowed
                    : ActivityCapabilityPermissionState.Blocked;

                ActivityCapabilityPermissionCommand permissionCommand = new(
                    ActivityCapabilityPermissionId.ActivityGameplayControl,
                    ActivityCapabilityPermissionScope.Actor,
                    permissionState,
                    activeIdentity.PipelineId,
                    activeIdentity.SessionId,
                    activeIdentity.ActivityId,
                    activeIdentity.EntrySequence,
                    actor.ActorId,
                    actorInstanceRuntimeId,
                    actor.PlayerActorId,
                    actor.PlayerSlotId,
                    command.Source,
                    command.Reason);

                var fact = _permissionRuntime.Publish(permissionCommand);
                if (IsRejected(fact))
                {
                    throw new InvalidOperationException($"Movement control permission publish rejected outcomeKind='{fact.OutcomeKind}' outcome='{fact.OutcomeCode}' playerActorId='{actor.PlayerActorId}'.");
                }

                records.Add(new MovementControlRecord(actor, command.Enable, movementEndpoint.GetType().Name));
            }

            return records;
        }

        private static bool IsRejected(ActivityCapabilityPermissionFact fact)
        {
            return fact.IsValid &&
                IsRejected(fact.OutcomeKind);
        }

        private static bool IsRejected(PermissionOutcomeKind outcomeKind)
        {
            return outcomeKind == PermissionOutcomeKind.RejectedInvalidCommand ||
                outcomeKind == PermissionOutcomeKind.RejectedForeignIdentity ||
                outcomeKind == PermissionOutcomeKind.RejectedStaleIdentity ||
                outcomeKind == PermissionOutcomeKind.RejectedMissingRequiredReceiver ||
                outcomeKind == PermissionOutcomeKind.Failed;
        }

        private static bool IsSameActivityCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) &&
                string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal) &&
                string.Equals(left.ActivityId, right.ActivityId, StringComparison.Ordinal) &&
                left.ActivityOrdinal == right.ActivityOrdinal &&
                left.EntrySequence == right.EntrySequence;
        }
    }

    public sealed class PlayerMovementPermissionReceiver : IActorPermissionReceiver
    {
        private readonly IActorMovementEndpoint _movementEndpoint;
        private readonly ActorId _actorId;
        private readonly ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private readonly PlayerActorId _playerActorId;
        private readonly PlayerSlotId _playerSlotId;
        private readonly ActivityCapabilityPermissionReceiverId _receiverId;

        public PlayerMovementPermissionReceiver(
            IActorMovementEndpoint movementEndpoint,
            ActivityCapabilityPermissionReceiverId receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            _movementEndpoint = movementEndpoint ?? throw new InvalidOperationException("PlayerMovementPermissionReceiver requires non-null IActorMovementEndpoint.");
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _playerActorId = playerActorId;
            _playerSlotId = playerSlotId;

            if (!_actorId.IsValid)
            {
                throw new InvalidOperationException("PlayerMovementPermissionReceiver requires valid ActorId.");
            }

            if (!_actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("PlayerMovementPermissionReceiver requires valid ActorInstanceRuntimeId.");
            }

            if (!_playerActorId.IsValid)
            {
                throw new InvalidOperationException("PlayerMovementPermissionReceiver requires valid PlayerActorId.");
            }

            _receiverId = receiverId;
            if (!_receiverId.IsValid)
            {
                throw new InvalidOperationException("PlayerMovementPermissionReceiver requires non-empty receiverId.");
            }
        }

        public ActivityCapabilityPermissionReceiverId ReceiverId => _receiverId;

        public static ActivityCapabilityPermissionReceiverId CreateReceiverId(
            ActivityCapabilityPermissionReceiverIdentity identity)
        {
            string normalizedPipelineId = identity.PipelineId.TrimToEmpty();
            string normalizedSessionStateId = identity.SessionStateId.TrimToEmpty();
            string normalizedActivityId = identity.ActivityId.TrimToEmpty();
            string normalizedActorInstanceRuntimeId = identity.ActorInstanceRuntimeId.IsValid ? identity.ActorInstanceRuntimeId.Value : string.Empty;
            string actorInstanceToken = string.IsNullOrWhiteSpace(normalizedActorInstanceRuntimeId) ? "actor.instance.unbound" : normalizedActorInstanceRuntimeId;
            return ActivityCapabilityPermissionReceiverId.FromString($"movement.receiver|pipeline={normalizedPipelineId}|session={normalizedSessionStateId}|activity={normalizedActivityId}|entry={identity.EntrySequence}|actorInstance={actorInstanceToken}");
        }

        public void OnPermissionChanged(ActivityCapabilityPermissionFact fact)
        {
            if (!fact.IsValid || !fact.Command.IsValid)
            {
                return;
            }

            if (!IsGameplayControlPermission(fact.Command.PermissionId))
            {
                return;
            }

            if (!TargetsCurrentActor(fact.Command.ActorInstanceRuntimeId))
            {
                return;
            }

            DebugUtility.LogVerbose(
                typeof(PlayerMovementPermissionReceiver),
                $"event='ActivityCapabilityPermissionReceiverNotified' permissionId='{fact.Command.PermissionId}' state='{fact.Command.State}' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                DebugUtility.Colors.Info);

            switch (fact.Command.State)
            {
                case ActivityCapabilityPermissionState.Allowed:
                    _movementEndpoint.SetMovementEnabled(true);
                    DebugUtility.Log(
                        typeof(PlayerMovementPermissionReceiver),
                        $"event='PlayerMovementPermissionApplied' permissionId='{fact.Command.PermissionId}' state='Allowed' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Success);
                    break;

                case ActivityCapabilityPermissionState.Blocked:
                    _movementEndpoint.SetMovementEnabled(false);
                    _movementEndpoint.ClearMovementState();
                    DebugUtility.Log(
                        typeof(PlayerMovementPermissionReceiver),
                        $"event='PlayerMovementPermissionApplied' permissionId='{fact.Command.PermissionId}' state='Blocked' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Warning);
                    break;

                case ActivityCapabilityPermissionState.Unbound:
                    _movementEndpoint.SetMovementEnabled(false);
                    _movementEndpoint.ClearMovementState();
                    DebugUtility.Log(
                        typeof(PlayerMovementPermissionReceiver),
                        $"event='PlayerMovementPermissionApplied' permissionId='{fact.Command.PermissionId}' state='Unbound' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Warning);
                    break;
            }
        }

        private bool TargetsCurrentActor(ActorInstanceRuntimeId actorInstanceRuntimeId)
        {
            return actorInstanceRuntimeId.IsValid && actorInstanceRuntimeId == _actorInstanceRuntimeId;
        }

        private static bool IsGameplayControlPermission(ActivityCapabilityPermissionId permissionId)
        {
            string token = ActivityCapabilityPermissionIds.ToToken(permissionId);
            return string.Equals(token, ActivityCapabilityPermissionIds.ActivityGameplayControl, StringComparison.Ordinal);
        }
    }
}
