using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityActorParticipationExitDecisionRecordFactory
    {
        public static ActivityActorParticipationExitDecisionRecord Create(
            ActivityExitActorTeardownCommand command,
            SessionActivityIdentity identity,
            ActorInstanceRecord instance,
            ActorLifetimeTrigger trigger,
            ActorLifetimeDecision decision)
        {
            return new ActivityActorParticipationExitDecisionRecord(
                identity,
                command.Identity.ActivityId,
                command.EntrySequence,
                instance.ActorId,
                instance.ActorInstanceRuntimeId,
                instance.Scope,
                trigger,
                decision,
                command.Source,
                command.Reason);
        }

        public static ActivityActorParticipationExitDecisionRecord Create(
            ActivityExitActorTeardownCommand command,
            SessionActivityIdentity identity,
            SessionActorRuntimeEntry entry,
            ActorLifetimeTrigger trigger,
            ActorLifetimeDecision decision)
        {
            return new ActivityActorParticipationExitDecisionRecord(
                identity,
                command.Identity.ActivityId,
                command.EntrySequence,
                entry.ActorId.Value,
                entry.ActorInstanceRuntimeId,
                entry.ActorScope,
                trigger,
                decision,
                command.Source,
                command.Reason);
        }
    }
}
