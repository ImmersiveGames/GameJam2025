using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityRouteExitActorLifetimeDecisionStage
    {
        public static void Execute(
            ActivityExitActorTeardownCommand command,
            IActivityEntryRuntimeBridge endpoint,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            List<SessionActivityFact> facts,
            SessionActivityIdentity identity,
            HashSet<ActorInstanceRuntimeId> alreadyResolvedRuntimeIds)
        {
            if (command.ReleaseRail != SessionActivityPipeline.ActorPresentationReleaseRail.RouteExit ||
                sessionActorRuntimeStore == null ||
                !identity.IsValid)
            {
                return;
            }

            IReadOnlyList<SessionActorRuntimeEntry> sessionActors = sessionActorRuntimeStore.GetEntriesForSession(identity);
            if (sessionActors == null || sessionActors.Count == 0)
            {
                return;
            }

            HashSet<ActorInstanceRuntimeId> resolvedRuntimeIds = alreadyResolvedRuntimeIds ?? new HashSet<ActorInstanceRuntimeId>();
            for (int index = 0; index < sessionActors.Count; index++)
            {
                var entry = sessionActors[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                var runtimeId = entry.ActorInstanceRuntimeId;
                if (!runtimeId.IsValid || resolvedRuntimeIds.Contains(runtimeId))
                {
                    continue;
                }

                var decision = ActorLifetimePolicyRuntime.ResolveDecision(entry.ActorScope, ActorLifetimeTrigger.RouteExit);
                var lifetimeDecision = ActivityActorParticipationExitDecisionRecordFactory.Create(
                    command,
                    identity,
                    entry,
                    ActorLifetimeTrigger.RouteExit,
                    decision);

                ActivityActorParticipationExitFactWriter.EmitLifetimeDecision(
                    endpoint,
                    facts,
                    lifetimeDecision,
                    typeof(ActivityRouteExitActorLifetimeDecisionStage),
                    nameof(ActivityRouteExitActorLifetimeDecisionStage));

                resolvedRuntimeIds.Add(runtimeId);
            }
        }
    }
}
