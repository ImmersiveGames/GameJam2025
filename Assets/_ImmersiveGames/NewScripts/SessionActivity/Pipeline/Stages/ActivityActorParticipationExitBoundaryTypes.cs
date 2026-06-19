using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityActorParticipationExitBoundaryCommand
    {
        public ActivityActorParticipationExitBoundaryCommand(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            SessionActivityIdentity startedIdentity,
            ActorLifetimeTrigger lifetimeTrigger)
        {
            Command = command;
            Definition = definition;
            StartedIdentity = startedIdentity;
            LifetimeTrigger = lifetimeTrigger;
        }

        public ActivityExitActorTeardownCommand Command { get; }
        public SessionActivityDefinition Definition { get; }
        public SessionActivityIdentity StartedIdentity { get; }
        public ActorLifetimeTrigger LifetimeTrigger { get; }

        public bool IsValid =>
            Command.IsValid &&
            Definition.IsValid &&
            StartedIdentity.IsValid &&
            LifetimeTrigger != ActorLifetimeTrigger.Unknown;
    }

    internal readonly struct ActivityActorParticipationExitBoundaryResult
    {
        public ActivityActorParticipationExitBoundaryResult(
            SessionActivityIdentity completedIdentity,
            int total,
            int exited,
            int skipped,
            int failed,
            IReadOnlyList<PlayerActorIdentityRecord> exitedPlayerActors,
            HashSet<ActorInstanceRuntimeId> actorLifetimeDecisionRuntimeIds)
        {
            CompletedIdentity = completedIdentity;
            Total = total < 0 ? 0 : total;
            Exited = exited < 0 ? 0 : exited;
            Skipped = skipped < 0 ? 0 : skipped;
            Failed = failed < 0 ? 0 : failed;
            ExitedPlayerActors = exitedPlayerActors ?? Array.Empty<PlayerActorIdentityRecord>();
            ActorLifetimeDecisionRuntimeIds = actorLifetimeDecisionRuntimeIds ?? new HashSet<ActorInstanceRuntimeId>();
        }

        public SessionActivityIdentity CompletedIdentity { get; }
        public int Total { get; }
        public int Exited { get; }
        public int Skipped { get; }
        public int Failed { get; }
        public IReadOnlyList<PlayerActorIdentityRecord> ExitedPlayerActors { get; }
        public HashSet<ActorInstanceRuntimeId> ActorLifetimeDecisionRuntimeIds { get; }
        public bool IsValid => CompletedIdentity.IsValid && Total >= 0 && Exited >= 0 && Skipped >= 0 && Failed >= 0;
    }

    internal readonly struct ActivityActorParticipationExitDecisionRecord
    {
        public ActivityActorParticipationExitDecisionRecord(
            SessionActivityIdentity identity,
            string activityId,
            int entrySequence,
            string actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorScope actorScope,
            ActorLifetimeTrigger trigger,
            ActorLifetimeDecision decision,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityId = activityId.TrimToEmpty();
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            ActorId = actorId.TrimToEmpty();
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorScope = actorScope;
            Trigger = trigger;
            Decision = decision;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public string ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorScope ActorScope { get; }
        public ActorLifetimeTrigger Trigger { get; }
        public ActorLifetimeDecision Decision { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(ActorId) &&
            ActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
    }

}
