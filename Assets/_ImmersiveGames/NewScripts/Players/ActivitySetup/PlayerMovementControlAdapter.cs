using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.ActivitySetup
{
    public sealed class PlayerMovementControlAdapter : IPlayerMovementControlAdapter
    {
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

                if (!command.Enable)
                {
                    controller.ClearMovementState();
                }

                controller.SetMovementEnabled(command.Enable);

                records.Add(new MovementControlRecord(actor, command.Enable, $"{controller.GetType().Name}|reader={reader.GetType().Name}"));
            }

            return records;
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
}
