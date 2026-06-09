using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Projectile.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

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
                if (!commandHub.IsPrepared)
                {
                    if (requirement.Required)
                    {
                        throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' actor command hub is not prepared.");
                    }

                    DebugUtility.Log(
                        typeof(ActorCommandBindingAdapter),
                        $"[OBS][ActorCommandHub] event='ActorCommandBindingSkipped' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' commandId='FirePrimary' state='SkippedOptional' sink='none' source='{nameof(ActorCommandBindingAdapter)}' reason='optional_hub_not_prepared'.",
                        DebugUtility.Colors.Info);

                    records.Add(new ActorCommandBindingRecord(
                        requirement,
                        actorHandle.ActorIdentity,
                        ActorCommandBindingState.SkippedOptional,
                        observedEndpoint: "optional_hub_not_prepared"));
                    continue;
                }

                if (!commandHub.HasBinding(
                    ActorCommandId.FirePrimary,
                    ActorCommandTriggerKind.Pressed))
                {
                    if (requirement.Required)
                    {
                        throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' missing active FirePrimary binding on ActorCommandSourceHub.");
                    }

                    DebugUtility.Log(
                        typeof(ActorCommandBindingAdapter),
                        $"[OBS][ActorCommandHub] event='ActorCommandBindingSkipped' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' commandId='FirePrimary' state='SkippedOptional' sink='none' source='{nameof(ActorCommandBindingAdapter)}' reason='optional_fireprimary_binding_missing'.",
                        DebugUtility.Colors.Info);

                    records.Add(new ActorCommandBindingRecord(
                        requirement,
                        actorHandle.ActorIdentity,
                        ActorCommandBindingState.SkippedOptional,
                        observedEndpoint: "optional_fireprimary_binding_missing"));
                    continue;
                }

                DebugUtility.Log(
                    typeof(ActorCommandBindingAdapter),
                    $"[OBS][ActorCommandHub] event='ActorCommandBindingReadinessObserved' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' participantId='{requirement.ParticipantBinding.ParticipantId}' commandId='FirePrimary' state='Prepared' hubType='{commandHub.GetType().Name}' hubPrepared='{commandHub.IsPrepared}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                IActorProjectileFireEndpoint projectileFireEndpoint = capabilitySurface.ActorProjectileFireEndpoint;
                if (projectileFireEndpoint == null)
                {
                    if (requirement.Required)
                    {
                        throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' missing ActorProjectileFireEndpoint.");
                    }

                    DebugUtility.Log(
                        typeof(ActorCommandBindingAdapter),
                        $"[OBS][ActorProjectileFire] event='ActorProjectileFireEndpointSkipped' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' participantId='{requirement.ParticipantBinding.ParticipantId}' commandId='FirePrimary' state='SkippedOptional' reason='optional_projectile_fire_endpoint_missing' source='{nameof(ActorCommandBindingAdapter)}'.",
                        DebugUtility.Colors.Info);

                    records.Add(new ActorCommandBindingRecord(
                        requirement,
                        actorHandle.ActorIdentity,
                        ActorCommandBindingState.SkippedOptional,
                        observedEndpoint: "optional_projectile_fire_endpoint_missing"));
                    continue;
                }

                if (!projectileFireEndpoint.TryGetReadiness(ActorCommandId.FirePrimary, out ActorProjectileFireEndpointReadiness readiness) || !readiness.IsPrepared)
                {
                    if (requirement.Required || projectileFireEndpoint.IsRequired)
                    {
                        throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' projectile fire endpoint is not ready. state='{readiness.Kind}' reason='{readiness.Reason}'.");
                    }

                    DebugUtility.Log(
                        typeof(ActorCommandBindingAdapter),
                        $"[OBS][ActorProjectileFire] event='ActorProjectileFireEndpointReadinessObserved' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' participantId='{requirement.ParticipantBinding.ParticipantId}' commandId='FirePrimary' endpointId='{projectileFireEndpoint.EndpointId}' profileId='{projectileFireEndpoint.ProfileId}' fireModeId='{projectileFireEndpoint.DefaultFireModeId}' state='{readiness.Kind}' readinessAccepted='False' source='{command.Source}' reason='{readiness.Reason}'.",
                        DebugUtility.Colors.Info);

                    records.Add(new ActorCommandBindingRecord(
                        requirement,
                        actorHandle.ActorIdentity,
                        ActorCommandBindingState.SkippedOptional,
                        observedEndpoint: $"{projectileFireEndpoint.GetType().Name}|state={readiness.Kind}|reason={readiness.Reason}"));
                    continue;
                }

                DebugUtility.Log(
                    typeof(ActorCommandBindingAdapter),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileFireEndpointReadinessObserved' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' participantId='{requirement.ParticipantBinding.ParticipantId}' commandId='FirePrimary' endpointId='{projectileFireEndpoint.EndpointId}' profileId='{projectileFireEndpoint.ProfileId}' fireModeId='{projectileFireEndpoint.DefaultFireModeId}' state='{readiness.Kind}' readinessAccepted='True' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);

                IActorProjectileSpawnAdapter spawnAdapter = new PooledActorProjectileSpawnAdapter(
                    "actor.projectile.spawn.adapter.pooled.player.primary");
                projectileFireEndpoint.ConfigureSpawnAdapter(
                    spawnAdapter,
                    nameof(ActorCommandBindingAdapter),
                    "projectile_spawn_adapter_configured_by_actor_command_binding");

                commandHub.BindCommandSink(ActorCommandId.FirePrimary, projectileFireEndpoint);

                DebugUtility.Log(
                    typeof(ActorCommandBindingAdapter),
                    $"[OBS][ActorCommandHub] event='ActorCommandSinkBound' actorId='{requirement.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' participantId='{requirement.ParticipantBinding.ParticipantId}' commandId='FirePrimary' state='Executable' sink='{projectileFireEndpoint.GetType().Name}' endpointId='{projectileFireEndpoint.EndpointId}' profileId='{projectileFireEndpoint.ProfileId}' fireModeId='{projectileFireEndpoint.DefaultFireModeId}' adapter='{projectileFireEndpoint.SpawnAdapterName}' source='{nameof(ActorCommandBindingAdapter)}' reason='projectile_fire_spawn_adapter_sink_bound'.",
                    DebugUtility.Colors.Success);

                records.Add(new ActorCommandBindingRecord(
                    requirement,
                    actorHandle.ActorIdentity,
                    ActorCommandBindingState.Executable,
                    observedEndpoint: $"{projectileFireEndpoint.GetType().Name}|endpointId={projectileFireEndpoint.EndpointId}|profileId={projectileFireEndpoint.ProfileId}|fireModeId={projectileFireEndpoint.DefaultFireModeId}|spawnAdapter={projectileFireEndpoint.SpawnAdapterName}"));
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
