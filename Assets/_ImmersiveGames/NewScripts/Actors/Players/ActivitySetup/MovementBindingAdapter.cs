using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class MovementBindingAdapter : IMovementBindingAdapter
    {
        private readonly IActivityCapabilityPermissionRuntime _permissionRuntime;

        public MovementBindingAdapter(IActivityCapabilityPermissionRuntime permissionRuntime)
        {
            _permissionRuntime = permissionRuntime ?? throw new InvalidOperationException("MovementBindingAdapter requires non-null permission runtime.");
        }

        public IReadOnlyList<MovementBindingRecord> Execute(
            MovementBindingCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("MovementBindingCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for movement binding.");
            }

            if (!IsSameActivityCycle(command.PipelineIdentity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_movement_binding_command: command identity does not match active identity.");
            }

            if (registry == null)
            {
                throw new InvalidOperationException("MovementBindingAdapter requires non-null registry.");
            }

            _permissionRuntime.SetActiveIdentity(
                activeIdentity.PipelineId,
                activeIdentity.SessionId,
                activeIdentity.ActivityId,
                activeIdentity.EntrySequence);

            List<MovementBindingRecord> records = new(command.Requirements.Count);
            for (int index = 0; index < command.Requirements.Count; index++)
            {
                MovementBindingRequirement requirement = command.Requirements[index];
                if (!requirement.IsValid)
                {
                    throw new InvalidOperationException($"MovementBindingRequirement at index '{index}' is invalid.");
                }

                if (!IsSameActivityCycle(requirement.Identity, activeIdentity))
                {
                    throw new InvalidOperationException("stale_or_foreign_movement_binding_requirement: requirement identity does not match active identity.");
                }

                PlayerActorRuntimeHandle actorHandle = registry.ResolveActiveHandleOrFail(activeIdentity, requirement.ParticipantId);
                GameObject actorInstance = actorHandle.Instance;
                if (actorHandle.PlayerActorId != requirement.PlayerActorId || actorHandle.PlayerSlotId != requirement.PlayerSlotId)
                {
                    throw new InvalidOperationException($"stale_or_foreign_movement_binding_requirement: handle mismatch participantId='{requirement.ParticipantId}' playerActorId='{requirement.PlayerActorId}' playerSlotId='{requirement.PlayerSlotId}'.");
                }

                PlayerInput input = ResolveBoundPlayerInputOrFail(actorInstance, requirement);
                PlayerMoveInputReader reader = ResolveSingleComponentOrFail<PlayerMoveInputReader>(actorInstance, requirement, "PlayerMoveInputReader");
                PlayerMovementController controller = ResolveSingleComponentOrFail<PlayerMovementController>(actorInstance, requirement, "PlayerMovementController");

                reader.Bind(input);
                reader.SetInputEnabled(false);
                reader.ClearInput();
                controller.BindReaderOrFail(reader);

                PlayerActorMovementBindingState bindingState = actorInstance.GetComponent<PlayerActorMovementBindingState>();
                if (bindingState == null)
                {
                    bindingState = actorInstance.AddComponent<PlayerActorMovementBindingState>();
                }

                bindingState.Bind(
                    activeIdentity,
                    requirement.PlayerSlotId,
                    requirement.PlayerActorId,
                    bindEndpointType: nameof(PlayerMovementController));

                ActivityCapabilityPermissionCommand permissionCommand = new(
                    ActivityCapabilityPermissionId.ActivityGameplayControl,
                    ActivityCapabilityPermissionScope.Actor,
                    ActivityCapabilityPermissionState.Blocked,
                    activeIdentity.PipelineId,
                    activeIdentity.SessionId,
                    activeIdentity.ActivityId,
                    activeIdentity.EntrySequence,
                    requirement.PlayerActorId,
                    requirement.PlayerSlotId,
                    command.Source,
                    command.Reason);

                ActivityCapabilityPermissionFact fact = _permissionRuntime.Publish(permissionCommand);
                if (IsRejected(fact))
                {
                    throw new InvalidOperationException($"Movement binding permission publish rejected outcomeKind='{fact.OutcomeKind}' outcome='{fact.OutcomeCode}' playerActorId='{requirement.PlayerActorId}'.");
                }

                records.Add(new MovementBindingRecord(requirement, bound: true,
                    observedEndpoint: $"{controller.GetType().Name}|reader={reader.GetType().Name}|playerInput={input.name}|controlEnabled=false"));
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

        private static PlayerInput ResolveBoundPlayerInputOrFail(GameObject actorInstance, MovementBindingRequirement requirement)
        {
            PlayerInput[] inputs = actorInstance.GetComponentsInChildren<PlayerInput>(includeInactive: true);
            if (inputs == null || inputs.Length == 0)
            {
                throw new InvalidOperationException($"Movement binding failed: playerActorId='{requirement.PlayerActorId}' slotId='{requirement.PlayerSlotId}' sem PlayerInput.");
            }

            if (inputs.Length > 1)
            {
                throw new InvalidOperationException($"Movement binding failed: playerActorId='{requirement.PlayerActorId}' slotId='{requirement.PlayerSlotId}' possui multiplos PlayerInput sem endpoint explicito.");
            }

            return inputs[0] ?? throw new InvalidOperationException($"Movement binding failed: playerActorId='{requirement.PlayerActorId}' slotId='{requirement.PlayerSlotId}' PlayerInput invalido.");
        }

        private static T ResolveSingleComponentOrFail<T>(GameObject actorInstance, MovementBindingRequirement requirement, string componentLabel)
            where T : Component
        {
            T[] found = actorInstance.GetComponentsInChildren<T>(includeInactive: true);
            if (found == null || found.Length == 0)
            {
                throw new InvalidOperationException($"Movement binding failed: playerActorId='{requirement.PlayerActorId}' slotId='{requirement.PlayerSlotId}' sem {componentLabel}.");
            }

            if (found.Length > 1)
            {
                throw new InvalidOperationException($"Movement binding failed: playerActorId='{requirement.PlayerActorId}' slotId='{requirement.PlayerSlotId}' possui multiplos {componentLabel} sem endpoint explicito.");
            }

            return found[0] ?? throw new InvalidOperationException($"Movement binding failed: playerActorId='{requirement.PlayerActorId}' slotId='{requirement.PlayerSlotId}' {componentLabel} invalido.");
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
}
