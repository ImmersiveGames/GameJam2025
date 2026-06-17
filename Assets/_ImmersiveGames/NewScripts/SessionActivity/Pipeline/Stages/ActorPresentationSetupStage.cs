using System;
using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActorPresentationSetupStage
    {
        public static ActorPresentationPlanResolutionResult ResolvePlan(
            ActorPresentationPlanResolver resolver,
            ActorPresentationProfileAsset profile,
            ActorPresentationEndpoint endpoint,
            string activityId,
            string actorId,
            string actorKind,
            string source,
            string reason)
        {
            return resolver.Resolve(profile, endpoint, activityId, actorId, actorKind, source, reason);
        }

        public static ActorPresentationResult Materialize(
            IActorPresentationMaterializationAdapter adapter,
            ActorPresentationResolvedPlan resolvedPlan,
            string source,
            string reason)
        {
            ActorPresentationMaterializationCommand command = new(resolvedPlan, source, reason);
            return adapter.Materialize(command);
        }

        public static bool IsRetentionPolicyAllowed(ActorPresentationReleasePolicy policy, out string reasonCode)
        {
            switch (policy)
            {
                case ActorPresentationReleasePolicy.KeepBound:
                case ActorPresentationReleasePolicy.ReleaseOnRouteExit:
                    reasonCode = "retention_allowed";
                    return true;
                case ActorPresentationReleasePolicy.ReleaseOnActivityExit:
                    reasonCode = "policy_requires_activity_exit_release";
                    return false;
                default:
                    reasonCode = "policy_unknown";
                    return false;
            }
        }

        public static bool IsRetentionCompatible(
            string expectedActorId,
            ActorPresentationRuntimeHandle activeHandle,
            ActorPresentationResolvedPlan resolvedPlan,
            out string reasonCode)
        {
            if (string.IsNullOrWhiteSpace(expectedActorId))
            {
                reasonCode = "player_actor_id_missing";
                return false;
            }

            if (!activeHandle.IsValid)
            {
                reasonCode = "active_handle_invalid";
                return false;
            }

            if (!string.Equals(activeHandle.ResolvedPlan.ActorId, expectedActorId, StringComparison.Ordinal))
            {
                reasonCode = "actor_id_mismatch";
                return false;
            }

            if (!resolvedPlan.IsValid)
            {
                reasonCode = "resolved_plan_invalid";
                return false;
            }

            if (!string.Equals(activeHandle.ResolvedPlan.ProfileId, resolvedPlan.ProfileId, StringComparison.Ordinal))
            {
                reasonCode = "profile_id_mismatch";
                return false;
            }

            if (activeHandle.ResolvedPlan.PrimarySlotKind != resolvedPlan.PrimarySlotKind)
            {
                reasonCode = "primary_slot_kind_mismatch";
                return false;
            }

            if (!string.Equals(activeHandle.ResolvedPlan.PrimarySlotId, resolvedPlan.PrimarySlotId, StringComparison.Ordinal))
            {
                reasonCode = "primary_slot_id_mismatch";
                return false;
            }

            if (activeHandle.PresentationInstance == null)
            {
                reasonCode = "presentation_instance_missing";
                return false;
            }

            reasonCode = "retention_compatible";
            return true;
        }
    }
}
