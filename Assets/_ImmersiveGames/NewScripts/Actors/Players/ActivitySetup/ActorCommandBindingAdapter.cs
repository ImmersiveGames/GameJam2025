using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class ActorCommandBindingAdapter : IActorCommandBindingAdapter
    {
        public IReadOnlyList<ActorCommandBindingRecord> Execute(
            ActorCommandBindingCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActorCommandBindingCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for actor command binding.");
            }

            if (!IsSameActivityCycle(command.PipelineIdentity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_actor_command_binding_command: command identity does not match active identity.");
            }

            if (registry == null)
            {
                throw new InvalidOperationException("ActorCommandBindingAdapter requires non-null registry.");
            }

            List<ActorCommandBindingRecord> records = new(command.Bindings.Count);
            for (int index = 0; index < command.Bindings.Count; index++)
            {
                ActorCommandBindingReference requirement = command.Bindings[index];
                if (!requirement.IsValid)
                {
                    throw new InvalidOperationException($"ActorCommandBindingReference at index '{index}' is invalid.");
                }

                if (!requirement.ParticipantBinding.RequiresPlayerInput)
                {
                    continue;
                }

                if (!registry.TryGetActiveHandleByParticipant(requirement.ParticipantBinding.ParticipantId, out PlayerActorRuntimeHandle actorHandle) || !actorHandle.IsValid)
                {
                    throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' actor handle not found.");
                }
                ActorCapabilitySurface capabilitySurface = actorHandle.CapabilitySurface ?? throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' missing ActorCapabilitySurface.");
                IActorCommandSourceHub commandHub = capabilitySurface.ActorCommandSourceHub ?? throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' missing Actor command input hub.");
                IActorObjectEmitterEndpoint objectEmitterEndpoint = capabilitySurface.ActorObjectEmitterEndpoint;
                if (objectEmitterEndpoint == null)
                {
                    if (requirement.Required)
                    {
                        throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' missing IActorObjectEmitterEndpoint.");
                    }

                    DebugUtility.Log(
                        typeof(ActorCommandBindingAdapter),
                        $"[OBS][ActorCommandHub] event='ActorCommandSinkBindingSkipped' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' commandId='FirePrimary' sink='IActorObjectEmitterEndpoint' source='{nameof(ActorCommandBindingAdapter)}' reason='optional_endpoint_missing'.",
                        DebugUtility.Colors.Info);

                    records.Add(new ActorCommandBindingRecord(
                        requirement,
                        actorHandle.ActorIdentity,
                        bound: false,
                        skipped: true,
                        observedEndpoint: "optional_endpoint_missing"));
                    continue;
                }

                if (!commandHub.IsPrepared)
                {
                    throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' actor command hub is not prepared.");
                }

                if (!commandHub.HasBinding(
                    ActorCommandSourceKind.PlayerInput,
                    ActorCommandValueKind.FirePrimary,
                    ActorCommandTriggerKind.Pressed))
                {
                    throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' missing active FirePrimary binding on ActorCommandSourceHub.");
                }

                DebugUtility.Log(
                    typeof(ActorCommandBindingAdapter),
                    $"[OBS][ActorObjectEmission] event='ActorObjectEmitterEndpointResolved' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' participantId='{requirement.ParticipantBinding.ParticipantId}' endpointType='{objectEmitterEndpoint.GetType().Name}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                commandHub.BindCommandSink(ActorCommandValueKind.FirePrimary, objectEmitterEndpoint);

                DebugUtility.Log(
                    typeof(ActorCommandBindingAdapter),
                    $"[OBS][ActorCommandHub] event='ActorCommandSinkBound' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' commandId='FirePrimary' sink='{objectEmitterEndpoint.GetType().Name}' source='{nameof(ActorCommandBindingAdapter)}' reason='command_sink_bound'.",
                    DebugUtility.Colors.Info);

                records.Add(new ActorCommandBindingRecord(
                    requirement,
                    actorHandle.ActorIdentity,
                    bound: true,
                    skipped: false,
                    observedEndpoint: $"{objectEmitterEndpoint.GetType().Name}|hub={commandHub.GetType().Name}|hubPrepared={commandHub.IsPrepared}|hubBound=true"));
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
    }
}
