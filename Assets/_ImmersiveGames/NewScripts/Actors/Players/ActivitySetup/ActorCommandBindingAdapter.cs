using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Projectile.Binding;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class ActorCommandBindingAdapter : IActorCommandBindingAdapter
    {
        private readonly ActorProjectileFireCommandBindingExecutor _projectileFireCommandBindingExecutor;

        public ActorCommandBindingAdapter(
            IPoolService poolService,
            IGlobalAudioService globalAudioService,
            IActorAttributeEventStream actorAttributeEventStream)
        {
            _projectileFireCommandBindingExecutor = new ActorProjectileFireCommandBindingExecutor(
                poolService ?? throw new ArgumentNullException(nameof(poolService)),
                globalAudioService ?? throw new ArgumentNullException(nameof(globalAudioService)),
                actorAttributeEventStream ?? throw new ArgumentNullException(nameof(actorAttributeEventStream)));
        }

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
                var requirement = command.Bindings[index];
                if (!requirement.IsValid)
                {
                    throw new InvalidOperationException($"ActorCommandBindingReference at index '{index}' is invalid.");
                }

                if (!requirement.ParticipantBinding.RequiresPlayerInput)
                {
                    continue;
                }

                if (!registry.TryGetActiveHandleByParticipant(requirement.ParticipantBinding.ParticipantId, out var actorHandle) || !actorHandle.IsValid)
                {
                    throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' actor handle not found.");
                }

                var capabilitySurface = actorHandle.CapabilitySurface ?? throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' missing ActorCapabilitySurface.");
                var commandHub = capabilitySurface.ActorCommandSourceHub;
                var projectileFireEndpoint = capabilitySurface.ActorProjectileFireEndpoint;
                var bindingContext = new ActorProjectileFireCommandBindingContext(
                    requirement.ParticipantBinding.ActorId,
                    actorHandle.ActorInstanceRuntimeId,
                    requirement.ParticipantBinding.ParticipantId.ToString(),
                    requirement.Required,
                    command.Source,
                    command.Reason);

                ActorProjectileFireCommandBindingResult projectileBinding = _projectileFireCommandBindingExecutor.Execute(
                    bindingContext,
                    commandHub,
                    projectileFireEndpoint);

                if (!projectileBinding.IsValid)
                {
                    throw new InvalidOperationException($"Actor command binding failed: actorId='{requirement.ParticipantBinding.ActorId}' participantId='{requirement.ParticipantBinding.ParticipantId}' projectile binding returned invalid result.");
                }

                ActorCommandBindingState bindingState = projectileBinding.IsExecutable
                    ? ActorCommandBindingState.Executable
                    : ActorCommandBindingState.SkippedOptional;

                records.Add(new ActorCommandBindingRecord(
                    requirement,
                    actorHandle.ActorIdentity,
                    bindingState,
                    observedEndpoint: projectileBinding.ObservedEndpoint));
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
