using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Preparation
{
    public static class ActorPreparationStage
    {
        public static ActorPreparationResult Execute(ActorPreparationPlan plan)
        {
            if (!plan.IsValid)
            {
                throw new System.InvalidOperationException("ActorPreparationPlan is invalid.");
            }

            ActorPreparationSnapshot snapshot = new(
                plan.Identity,
                ActorPreparationOutcome.ObservedNoOp,
                0,
                "No actor materialization. Stage observed as canonical no-op.");

            ActorPreparationResult result = new(plan, snapshot);
            if (!result.IsValid)
            {
                throw new System.InvalidOperationException("ActorPreparationResult is invalid.");
            }

            DebugUtility.Log(typeof(ActorPreparationStage),
                $"[OBS][ActorPreparationStage] pipelineId='{plan.Identity.PipelineId}' sessionId='{plan.Identity.SessionId}' routeIdentity='{plan.Identity.RouteIdentity}' routeSequence='{plan.Identity.RouteSequence}' transitionId='{plan.Identity.TransitionId}' stage='ActorPreparationStage' outcome='observed_noop' source='{plan.Source}' reason='{plan.Reason}' plannedActors='{snapshot.PlannedActorsCount}' message='{snapshot.Message}'.",
                DebugUtility.Colors.Info);

            return result;
        }
    }
}
