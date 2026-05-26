using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.ActivitySetup
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
                PlayerActorIdentityRecord actor = command.Actors[index];
                if (!actor.IsValid)
                {
                    throw new InvalidOperationException($"Movement control actor identity invalid at index '{index}'.");
                }

                if (!registry.TryResolveInstanceForControl(activeIdentity, actor.PlayerActorId, out GameObject actorInstance, out PlayerActorIdentityRecord resolvedIdentity) ||
                    actorInstance == null ||
                    !resolvedIdentity.IsValid)
                {
                    throw new InvalidOperationException($"Movement control failed: actor not found for playerActorId='{actor.PlayerActorId}'.");
                }

                if (!string.Equals(resolvedIdentity.PlayerSlotId, actor.PlayerSlotId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Movement control failed: slot mismatch playerActorId='{actor.PlayerActorId}' expectedSlotId='{actor.PlayerSlotId}' observedSlotId='{resolvedIdentity.PlayerSlotId}'.");
                }

                PlayerMoveInputReader reader = ResolveSingleComponentOrFail<PlayerMoveInputReader>(actorInstance, actor, "PlayerMoveInputReader");
                PlayerMovementController controller = ResolveSingleComponentOrFail<PlayerMovementController>(actorInstance, actor, "PlayerMovementController");

                ActivityCapabilityPermissionState permissionState = command.Enable
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
                    actor.PlayerActorId,
                    command.Source,
                    command.Reason);

                ActivityCapabilityPermissionFact fact = _permissionRuntime.Publish(permissionCommand);
                if (IsRejected(fact))
                {
                    throw new InvalidOperationException($"Movement control permission publish rejected outcome='{fact.Outcome}' playerActorId='{actor.PlayerActorId}'.");
                }

                records.Add(new MovementControlRecord(actor, command.Enable, $"{controller.GetType().Name}|reader={reader.GetType().Name}"));
            }

            return records;
        }

        private static bool IsRejected(ActivityCapabilityPermissionFact fact)
        {
            return fact.IsValid &&
                !string.IsNullOrWhiteSpace(fact.Outcome) &&
                fact.Outcome.StartsWith("rejected", StringComparison.Ordinal);
        }

        private static T ResolveSingleComponentOrFail<T>(GameObject actorInstance, PlayerActorIdentityRecord actor, string componentLabel)
            where T : Component
        {
            T[] found = actorInstance.GetComponentsInChildren<T>(includeInactive: true);
            if (found == null || found.Length == 0)
            {
                throw new InvalidOperationException($"Movement control failed: playerActorId='{actor.PlayerActorId}' slotId='{actor.PlayerSlotId}' sem {componentLabel}.");
            }

            if (found.Length > 1)
            {
                throw new InvalidOperationException($"Movement control failed: playerActorId='{actor.PlayerActorId}' slotId='{actor.PlayerSlotId}' possui multiplos {componentLabel} sem endpoint explicito.");
            }

            return found[0] ?? throw new InvalidOperationException($"Movement control failed: playerActorId='{actor.PlayerActorId}' slotId='{actor.PlayerSlotId}' {componentLabel} invalido.");
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

    public sealed class PlayerMovementPermissionReceiver : IActivityCapabilityPermissionReceiver
    {
        private readonly PlayerMovementController _controller;
        private readonly string _pipelineId;
        private readonly string _sessionStateId;
        private readonly string _activityId;
        private readonly int _entrySequence;
        private readonly string _playerActorId;
        private readonly string _playerSlotId;
        private readonly string _receiverId;

        public PlayerMovementPermissionReceiver(
            PlayerMovementController controller,
            string receiverId,
            string pipelineId,
            string sessionStateId,
            string activityId,
            int entrySequence,
            string playerActorId,
            string playerSlotId)
        {
            _controller = controller ?? throw new InvalidOperationException("PlayerMovementPermissionReceiver requires non-null PlayerMovementController.");
            _pipelineId = Normalize(pipelineId);
            _sessionStateId = Normalize(sessionStateId);
            _activityId = Normalize(activityId);
            _entrySequence = entrySequence < 0 ? 0 : entrySequence;
            _playerActorId = Normalize(playerActorId);
            _playerSlotId = Normalize(playerSlotId);

            if (string.IsNullOrWhiteSpace(_pipelineId) ||
                string.IsNullOrWhiteSpace(_sessionStateId) ||
                string.IsNullOrWhiteSpace(_activityId) ||
                _entrySequence <= 0 ||
                string.IsNullOrWhiteSpace(_playerActorId))
            {
                throw new InvalidOperationException("PlayerMovementPermissionReceiver requires valid identity and playerActorId.");
            }

            _receiverId = Normalize(receiverId);
            if (string.IsNullOrWhiteSpace(_receiverId))
            {
                throw new InvalidOperationException("PlayerMovementPermissionReceiver requires non-empty receiverId.");
            }
        }

        public string ReceiverId => _receiverId;

        public static string CreateReceiverId(
            string pipelineId,
            string sessionStateId,
            string activityId,
            int entrySequence,
            string playerActorId,
            string playerSlotId)
        {
            string normalizedPipelineId = Normalize(pipelineId);
            string normalizedSessionStateId = Normalize(sessionStateId);
            string normalizedActivityId = Normalize(activityId);
            string normalizedPlayerActorId = Normalize(playerActorId);
            string normalizedPlayerSlotId = Normalize(playerSlotId);
            string slotToken = string.IsNullOrWhiteSpace(normalizedPlayerSlotId) ? "slot.unbound" : normalizedPlayerSlotId;
            return $"movement.receiver|pipeline={normalizedPipelineId}|session={normalizedSessionStateId}|activity={normalizedActivityId}|entry={entrySequence}|actor={normalizedPlayerActorId}|slot={slotToken}";
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

            if (!IsSameIdentity(fact.Command))
            {
                return;
            }

            if (!TargetsCurrentActor(fact.Command.TargetId))
            {
                return;
            }

            DebugUtility.Log(
                typeof(PlayerMovementPermissionReceiver),
                $"[OBS][ActivityCapabilityPermission] event='ActivityCapabilityPermissionReceiverNotified' permissionId='{fact.Command.PermissionId}' state='{fact.Command.State}' outcome='{fact.Outcome}' receiverId='{_receiverId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' pipelineId='{_pipelineId}' sessionStateId='{_sessionStateId}' activityId='{_activityId}' entrySequence='{_entrySequence}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                DebugUtility.Colors.Info);

            switch (fact.Command.State)
            {
                case ActivityCapabilityPermissionState.Allowed:
                    _controller.SetMovementEnabled(true);
                    DebugUtility.Log(
                        typeof(PlayerMovementPermissionReceiver),
                        $"[OBS][ActivityCapabilityPermission] event='PlayerMovementPermissionApplied' permissionId='{fact.Command.PermissionId}' state='Allowed' outcome='{fact.Outcome}' receiverId='{_receiverId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' pipelineId='{_pipelineId}' sessionStateId='{_sessionStateId}' activityId='{_activityId}' entrySequence='{_entrySequence}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Success);
                    break;

                case ActivityCapabilityPermissionState.Blocked:
                    _controller.SetMovementEnabled(false);
                    _controller.ClearMovementState();
                    DebugUtility.Log(
                        typeof(PlayerMovementPermissionReceiver),
                        $"[OBS][ActivityCapabilityPermission] event='PlayerMovementPermissionApplied' permissionId='{fact.Command.PermissionId}' state='Blocked' outcome='{fact.Outcome}' receiverId='{_receiverId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' pipelineId='{_pipelineId}' sessionStateId='{_sessionStateId}' activityId='{_activityId}' entrySequence='{_entrySequence}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Warning);
                    break;

                case ActivityCapabilityPermissionState.Unbound:
                    _controller.SetMovementEnabled(false);
                    _controller.ClearMovementState();
                    DebugUtility.Log(
                        typeof(PlayerMovementPermissionReceiver),
                        $"[OBS][ActivityCapabilityPermission] event='PlayerMovementPermissionApplied' permissionId='{fact.Command.PermissionId}' state='Unbound' outcome='{fact.Outcome}' receiverId='{_receiverId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' pipelineId='{_pipelineId}' sessionStateId='{_sessionStateId}' activityId='{_activityId}' entrySequence='{_entrySequence}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Warning);
                    break;
            }
        }

        private bool IsSameIdentity(ActivityCapabilityPermissionCommand command)
        {
            return string.Equals(command.PipelineId, _pipelineId, StringComparison.Ordinal) &&
                   string.Equals(command.SessionStateId, _sessionStateId, StringComparison.Ordinal) &&
                   string.Equals(command.ActivityId, _activityId, StringComparison.Ordinal) &&
                   command.EntrySequence == _entrySequence;
        }

        private bool TargetsCurrentActor(string targetId)
        {
            string normalized = Normalize(targetId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return true;
            }

            return string.Equals(normalized, _playerActorId, StringComparison.Ordinal) ||
                   (!string.IsNullOrWhiteSpace(_playerSlotId) &&
                    string.Equals(normalized, _playerSlotId, StringComparison.Ordinal)) ||
                   string.Equals(normalized, _receiverId, StringComparison.Ordinal);
        }

        private static bool IsGameplayControlPermission(ActivityCapabilityPermissionId permissionId)
        {
            string token = ActivityCapabilityPermissionIds.ToToken(permissionId);
            return string.Equals(token, ActivityCapabilityPermissionIds.ActivityGameplayControl, StringComparison.Ordinal);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
