using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.ActivitySetup
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
                PlayerActorIdentityRecord actorIdentity = command.Actors[index];
                if (!actorIdentity.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActorIdentityRecord at index '{index}' is invalid.");
                }

                if (!IsSameActivityCycle(actorIdentity.Identity, activeIdentity))
                {
                    throw new InvalidOperationException("stale_or_foreign_player_actor_identity: actor identity does not match active identity.");
                }

                GameObject instance = registry.ResolveActiveInstanceOrFail(activeIdentity, actorIdentity.PlayerActorId);
                PlayerActorIdentity boundIdentity = instance.GetComponent<PlayerActorIdentity>();
                if (boundIdentity == null || !boundIdentity.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActor identity component is missing or invalid. playerActorId='{actorIdentity.PlayerActorId}'.");
                }

                EnsureIdentityMatches(boundIdentity, activeIdentity, actorIdentity);

                PlayerActorParticipationState participation = instance.GetComponent<PlayerActorParticipationState>();
                if (participation == null)
                {
                    participation = instance.AddComponent<PlayerActorParticipationState>();
                }

                participation.MarkExitedActivityRetainedForRoute();

                ActivityCapabilityPermissionCommand permissionCommand = new(
                    ActivityCapabilityPermissionId.ActivityGameplayControl,
                    ActivityCapabilityPermissionScope.Actor,
                    ActivityCapabilityPermissionState.Unbound,
                    activeIdentity.PipelineId,
                    activeIdentity.SessionId,
                    activeIdentity.ActivityId,
                    activeIdentity.EntrySequence,
                    actorIdentity.PlayerActorId,
                    command.Source,
                    command.Reason);

                ActivityCapabilityPermissionFact fact = _permissionRuntime.Publish(permissionCommand);
                if (IsRejected(fact))
                {
                    throw new InvalidOperationException($"Player participation exit permission publish rejected outcome='{fact.Outcome}' playerActorId='{actorIdentity.PlayerActorId}'.");
                }

                records.Add(new PlayerActorParticipationExitRecord(actorIdentity, exited: true, retainedForRoute: true));
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
                PlayerActorIdentityRecord actorIdentity = command.Actors[index];
                if (!actorIdentity.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActorIdentityRecord at index '{index}' is invalid.");
                }

                if (!IsSameActivityCycle(actorIdentity.Identity, activeIdentity))
                {
                    throw new InvalidOperationException("stale_or_foreign_player_actor_identity: actor identity does not match active identity.");
                }

                GameObject instance = registry.ResolveActiveInstanceOrFail(activeIdentity, actorIdentity.PlayerActorId);
                PlayerActorIdentity boundIdentity = instance.GetComponent<PlayerActorIdentity>();
                if (boundIdentity == null || !boundIdentity.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActor identity component is missing or invalid. playerActorId='{actorIdentity.PlayerActorId}'.");
                }

                EnsureIdentityMatches(boundIdentity, activeIdentity, actorIdentity);

                PlayerActorParticipationState participation = instance.GetComponent<PlayerActorParticipationState>();
                if (participation == null)
                {
                    participation = instance.AddComponent<PlayerActorParticipationState>();
                }

                participation.MarkActiveInActivity(activeIdentity.ActivityId, activeIdentity.EntrySequence);
                records.Add(new PlayerActorParticipationEnterRecord(actorIdentity, entered: true, retainedForRoute: true));
            }

            return records;
        }

        private static bool IsRejected(ActivityCapabilityPermissionFact fact)
        {
            return fact.IsValid &&
                !string.IsNullOrWhiteSpace(fact.Outcome) &&
                fact.Outcome.StartsWith("rejected", StringComparison.Ordinal);
        }

        private static void EnsureIdentityMatches(
            PlayerActorIdentity identity,
            SessionActivityIdentity activeIdentity,
            PlayerActorIdentityRecord expected)
        {
            if (!string.Equals(identity.PipelineId, activeIdentity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(identity.SessionId, activeIdentity.SessionId, StringComparison.Ordinal) ||
                !string.Equals(identity.ActivityId, activeIdentity.ActivityId, StringComparison.Ordinal) ||
                identity.ActivityOrdinal != activeIdentity.ActivityOrdinal ||
                identity.EntrySequence != activeIdentity.EntrySequence ||
                !string.Equals(identity.PlayerSlotId, expected.PlayerSlotId, StringComparison.Ordinal) ||
                !string.Equals(identity.PlayerActorId, expected.PlayerActorId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"stale_or_foreign_player_actor_identity_binding: playerActorId='{expected.PlayerActorId}' does not match current pipeline identity.");
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
