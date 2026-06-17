using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class PlayerActorParticipationAdapter : IPlayerActorParticipationAdapter
    {
        private readonly IActivityCapabilityPermissionRuntime _permissionRuntime;

        public PlayerActorParticipationAdapter(IActivityCapabilityPermissionRuntime permissionRuntime)
        {
            _permissionRuntime = permissionRuntime ?? throw new InvalidOperationException("PlayerActorParticipationAdapter requires non-null permission runtime.");
        }

        public IReadOnlyList<PlayerActorParticipationExitRecord> Execute(
            PlayerActorParticipationExitCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("PlayerActorParticipationExitCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for player actor participation exit.");
            }

            if (!IsSameActivityCycle(command.PipelineIdentity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_player_actor_participation_exit_command: command identity does not match active identity.");
            }

            if (registry == null)
            {
                throw new InvalidOperationException("PlayerActorParticipationAdapter requires a non-null registry.");
            }

            _permissionRuntime.SetActiveIdentity(
                activeIdentity.PipelineId,
                activeIdentity.SessionId,
                activeIdentity.ActivityId,
                activeIdentity.EntrySequence);

            List<PlayerActorParticipationExitRecord> records = new(command.Actors.Count);
            for (int index = 0; index < command.Actors.Count; index++)
            {
                var actorIdentity = command.Actors[index];
                if (!actorIdentity.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActorIdentityRecord at index '{index}' is invalid.");
                }

                if (!TryResolveHandle(registry, actorIdentity.ParticipantId, out var handle) || !handle.IsValid)
                {
                    throw new InvalidOperationException(
                        $"player_participation_exit_actor_not_found: playerActorId='{actorIdentity.PlayerActorId}' playerSlotId='{actorIdentity.PlayerSlotId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
                }

                var actorInstanceRuntimeId = handle.ActorInstanceRuntimeId;
                if (!actorInstanceRuntimeId.IsValid)
                {
                    throw new InvalidOperationException($"player_participation_exit_actor_missing_actor_instance_runtime_id: playerActorId='{actorIdentity.PlayerActorId}' playerSlotId='{actorIdentity.PlayerSlotId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
                }

                var instance = handle.Instance;
                var boundIdentity = instance.GetComponent<PlayerActorIdentity>();
                if (boundIdentity == null || !boundIdentity.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActor identity component is missing or invalid. playerActorId='{actorIdentity.PlayerActorId}'.");
                }

                EnsureIdentityMatches(instance, boundIdentity, activeIdentity, actorIdentity);

                var participation = instance.GetComponent<PlayerActorParticipationState>();
                if (participation == null)
                {
                    participation = instance.AddComponent<PlayerActorParticipationState>();
                }

                participation.MarkExitedActivity();

                ActivityCapabilityPermissionCommand permissionCommand = new(
                    ActivityCapabilityPermissionId.ActivityGameplayControl,
                    ActivityCapabilityPermissionScope.Actor,
                    ActivityCapabilityPermissionState.Unbound,
                    activeIdentity.PipelineId,
                    activeIdentity.SessionId,
                    activeIdentity.ActivityId,
                    activeIdentity.EntrySequence,
                    actorIdentity.ActorId,
                    actorInstanceRuntimeId,
                    actorIdentity.PlayerActorId,
                    actorIdentity.PlayerSlotId,
                    command.Source,
                    command.Reason);

                var fact = _permissionRuntime.Publish(permissionCommand);
                if (IsRejected(fact))
                {
                    throw new InvalidOperationException($"Player participation exit permission publish rejected outcomeKind='{fact.OutcomeKind}' outcome='{fact.OutcomeCode}' playerActorId='{actorIdentity.PlayerActorId}'.");
                }

                records.Add(new PlayerActorParticipationExitRecord(actorIdentity, exited: true));
            }

            return records;
        }

        public IReadOnlyList<PlayerActorParticipationEnterRecord> Execute(
            PlayerActorParticipationEnterCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("PlayerActorParticipationEnterCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for player actor participation enter.");
            }

            if (!IsSameActivityCycle(command.PipelineIdentity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_player_actor_participation_enter_command: command identity does not match active identity.");
            }

            if (registry == null)
            {
                throw new InvalidOperationException("PlayerActorParticipationAdapter requires a non-null registry.");
            }

            List<PlayerActorParticipationEnterRecord> records = new(command.Actors.Count);
            for (int index = 0; index < command.Actors.Count; index++)
            {
                var actorIdentity = command.Actors[index];
                if (!actorIdentity.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActorIdentityRecord at index '{index}' is invalid.");
                }

                if (!TryResolveHandle(registry, actorIdentity.ParticipantId, out var handle) || !handle.IsValid)
                {
                    throw new InvalidOperationException(
                        $"player_participation_enter_actor_not_found: playerActorId='{actorIdentity.PlayerActorId}' playerSlotId='{actorIdentity.PlayerSlotId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
                }

                var instance = handle.Instance;
                var boundIdentity = instance.GetComponent<PlayerActorIdentity>();
                if (boundIdentity == null || !boundIdentity.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActor identity component is missing or invalid. playerActorId='{actorIdentity.PlayerActorId}'.");
                }

                EnsureIdentityMatches(instance, boundIdentity, activeIdentity, actorIdentity);

                var participation = instance.GetComponent<PlayerActorParticipationState>();
                if (participation == null)
                {
                    participation = instance.AddComponent<PlayerActorParticipationState>();
                }

                participation.MarkActiveInActivity(activeIdentity);
                records.Add(new PlayerActorParticipationEnterRecord(actorIdentity, entered: true));
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

        private static bool TryResolveHandle(ActivityPlayerActorRegistry registry, SessionParticipantId participantId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            return registry.TryGetActiveHandleByParticipant(participantId, out handle) && handle.IsValid ||
                registry.TryGetRouteScopedHandleByParticipant(participantId, out handle) && handle.IsValid;
        }

        private static void EnsureIdentityMatches(
            GameObject actorInstance,
            PlayerActorIdentity identity,
            SessionActivityIdentity activeIdentity,
            PlayerActorIdentityRecord expected)
        {
            if (actorInstance == null)
            {
                throw new InvalidOperationException(
                    $"stale_or_foreign_player_actor_identity_binding: playerActorId='{expected.PlayerActorId}' reason='actor_instance_null'.");
            }

            var runtimeActor = actorInstance.GetComponent<Actor>();
            bool isRetainedAcrossActivity = runtimeActor != null && ActorLifetimePolicyRuntime.IsRetainedAcrossActivity(runtimeActor.ActorScopeMetadata);

            if (!string.Equals(identity.PipelineId, activeIdentity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(identity.SessionId, activeIdentity.SessionId, StringComparison.Ordinal) ||
                identity.PlayerSlotId != expected.PlayerSlotId ||
                identity.PlayerActorId != expected.PlayerActorId)
            {
                throw new InvalidOperationException(
                    $"stale_or_foreign_player_actor_identity_binding: playerActorId='{expected.PlayerActorId}' does not match current pipeline identity.");
            }

            if (!isRetainedAcrossActivity &&
                (!string.Equals(identity.ActivityId, activeIdentity.ActivityId, StringComparison.Ordinal) ||
                 identity.ActivityOrdinal != activeIdentity.ActivityOrdinal ||
                 identity.EntrySequence != activeIdentity.EntrySequence))
            {
                throw new InvalidOperationException(
                    $"stale_or_foreign_player_actor_activity_context: playerActorId='{expected.PlayerActorId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}' does not match bound identity context.");
            }
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
