using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
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
                var requirement = command.Requirements[index];
                if (!requirement.IsValid)
                {
                    throw new InvalidOperationException($"MovementBindingRequirement at index '{index}' is invalid.");
                }

                if (!IsSameActivityCycle(requirement.Identity, activeIdentity))
                {
                    throw new InvalidOperationException("stale_or_foreign_movement_binding_requirement: requirement identity does not match active identity.");
                }

                if (!TryResolveHandle(registry, requirement.ParticipantId, out var actorHandle) || !actorHandle.IsValid)
                {
                    throw new InvalidOperationException($"Movement binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' actor handle not found.");
                }
                var playerActorId = actorHandle.PlayerActorId;
                var actorInstanceRuntimeId = actorHandle.ActorInstanceRuntimeId;
                if (!actorInstanceRuntimeId.IsValid)
                {
                    throw new InvalidOperationException($"Movement binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' missing ActorInstanceRuntimeId.");
                }

                if (actorHandle.PlayerSlotId != requirement.PlayerSlotId || actorHandle.ActorId != requirement.ActorId)
                {
                    throw new InvalidOperationException($"stale_or_foreign_movement_binding_requirement: handle mismatch participantId='{requirement.ParticipantId}' actorId='{requirement.ActorId}' playerSlotId='{requirement.PlayerSlotId}'.");
                }

                var capabilitySurface = actorHandle.CapabilitySurface ?? throw new InvalidOperationException($"Movement binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' missing ActorCapabilitySurface.");
                var commandHub = capabilitySurface.ActorCommandSourceHub ?? throw new InvalidOperationException($"Movement binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' missing Actor command input hub.");
                var movementEndpoint = capabilitySurface.ActorMovementEndpoint ?? throw new InvalidOperationException($"Movement binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' missing IActorMovementEndpoint.");
                if (movementEndpoint is not IActorCommandSink commandSink)
                {
                    throw new InvalidOperationException($"Movement binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' movement endpoint does not implement IActorCommandSink.");
                }

                if (!commandHub.IsPrepared)
                {
                    throw new InvalidOperationException($"Movement binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' actor command hub is not prepared.");
                }

                if (!commandHub.HasBinding(
                        ActorCommandId.Move,
                        ActorCommandTriggerKind.Continuous) &&
                    !commandHub.HasBinding(
                        ActorCommandId.Move,
                        ActorCommandTriggerKind.ValueChanged))
                {
                    throw new InvalidOperationException($"Movement binding failed: actorId='{requirement.ActorId}' participantId='{requirement.ParticipantId}' missing active Move binding on ActorCommandSourceHub.");
                }

                commandHub.BindCommandSink(ActorCommandId.Move, commandSink);

                var actorInstance = actorHandle.Instance;
                var bindingState = actorInstance.GetComponent<PlayerActorMovementBindingState>();
                if (bindingState == null)
                {
                    bindingState = actorInstance.AddComponent<PlayerActorMovementBindingState>();
                }

                bindingState.Bind(
                    activeIdentity,
                    requirement.PlayerSlotId,
                    playerActorId,
                    movementEndpoint.GetType().Name);

                records.Add(new MovementBindingRecord(
                    requirement,
                    actorHandle.ActorIdentity,
                    true,
                    $"{movementEndpoint.GetType().Name}|hub={commandHub.GetType().Name}|hubPrepared={commandHub.IsPrepared}|hubBound=true|controlEnabled=false"));
            }

            return records;
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

        private static bool TryResolveHandle(ActivityPlayerActorRegistry registry, SessionParticipantId participantId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            return registry.TryGetActiveHandleByParticipant(participantId, out handle) && handle.IsValid ||
                registry.TryGetRouteScopedHandleByParticipant(participantId, out handle) && handle.IsValid;
        }
    }
}
