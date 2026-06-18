using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Policies
{
    internal static class ActivityResetBoundaryPolicy
    {
        private const string Owner = "ActivityResetBoundaryPolicy";
        private const string BoundaryEligibilityPolicyId = "reset_intent_state_profile_policy.v1";

        internal static ActivityResetScopePlan ResolveForEntry(ActivityEntryCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCommand is invalid for reset boundary policy resolution.");
            }

            ActivityResetScopePlan plan = new(
                command.Identity,
                command.ResetBoundaryKind,
                ResolveTargetScope(command.ResetBoundaryKind),
                command.ResetIntent,
                command.StateProfileKind,
                BoundaryEligibilityPolicyId,
                command.Source,
                command.Reason);

            LogDecision(plan, "Resolved", ResolveOutcomeReason(command.ResetBoundaryKind));
            return plan;
        }

        internal static ActivityResetScopePlan ResolveForLocal(ActivityResetCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityResetCommand is invalid for local reset boundary policy resolution.");
            }

            ActivityResetScopePlan plan = new(
                command.Identity,
                ActivityResetBoundaryKind.Local,
                ActivityResetTargetScope.LocalTarget,
                ActivityResetIntent.RuntimeLocalReset,
                ActivityResetStateProfileKind.RuntimeLocalState,
                BoundaryEligibilityPolicyId,
                command.Source,
                command.Reason);

            LogDecision(plan, "Resolved", "local_boundary_eligibility_policy_resolved");
            return plan;
        }

        internal static ActivityResetTargetScope ResolveTargetScope(ActivityResetBoundaryKind boundaryKind)
        {
            return boundaryKind switch
            {
                ActivityResetBoundaryKind.Local => ActivityResetTargetScope.LocalTarget,
                ActivityResetBoundaryKind.Activity => ActivityResetTargetScope.CurrentActivity,
                ActivityResetBoundaryKind.ActivityTransition => ActivityResetTargetScope.CurrentActivity,
                ActivityResetBoundaryKind.RouteTransition => ActivityResetTargetScope.CurrentRoute,
                _ => ActivityResetTargetScope.Unknown,
            };
        }

        private static string ResolveOutcomeReason(ActivityResetBoundaryKind boundaryKind)
        {
            return boundaryKind switch
            {
                ActivityResetBoundaryKind.Local => "local_boundary_eligibility_policy_resolved",
                ActivityResetBoundaryKind.Activity => "activity_boundary_eligibility_policy_resolved",
                ActivityResetBoundaryKind.ActivityTransition => "activity_transition_boundary_eligibility_policy_resolved",
                ActivityResetBoundaryKind.RouteTransition => "route_transition_boundary_eligibility_policy_resolved",
                _ => "unknown_boundary_eligibility_policy_rejected",
            };
        }

        internal static bool AllowsReset(ActivityResetScopePlan resetScopePlan, ActivityResetBoundaryEligibility eligibility)
        {
            if (!resetScopePlan.IsValid)
            {
                return false;
            }

            if (resetScopePlan.ResetIntent == ActivityResetIntent.EntryInitialize)
            {
                return true;
            }

            ActivityResetBoundaryEligibility requiredEligibility = ResolveEligibility(resetScopePlan.BoundaryKind);
            return requiredEligibility != ActivityResetBoundaryEligibility.None &&
                   (eligibility & requiredEligibility) == requiredEligibility;
        }

        internal static ActivityResetBoundaryEligibility ResolveEligibility(ActivityResetBoundaryKind boundaryKind)
        {
            return boundaryKind switch
            {
                ActivityResetBoundaryKind.Local => ActivityResetBoundaryEligibility.Local,
                ActivityResetBoundaryKind.Activity => ActivityResetBoundaryEligibility.Activity,
                ActivityResetBoundaryKind.ActivityTransition => ActivityResetBoundaryEligibility.ActivityTransition,
                ActivityResetBoundaryKind.RouteTransition => ActivityResetBoundaryEligibility.RouteTransition,
                _ => ActivityResetBoundaryEligibility.None,
            };
        }

        internal static IReadOnlyList<ActorCapabilityResetEndpointReference> FilterActorResetReferencesByScopePlan(
            ActivityResetScopePlan resetScopePlan,
            IReadOnlyList<ActorCapabilityResetEndpointReference> references,
            string activityId,
            string contextId)
        {
            if (!resetScopePlan.IsValid)
            {
                throw new InvalidOperationException("ActivityResetScopePlan is invalid for actor reset reference boundary filtering.");
            }

            if (references == null || references.Count == 0)
            {
                return Array.Empty<ActorCapabilityResetEndpointReference>();
            }

            List<ActorCapabilityResetEndpointReference> filtered = new(references.Count);
            for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
            {
                ActorCapabilityResetEndpointReference reference = references[referenceIndex];
                if (reference == null || !reference.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Invalid actor reset reference for boundary filtering. activityId='{activityId.TrimToEmpty()}' contextId='{contextId.TrimToEmpty()}' referenceIndex='{referenceIndex}'.");
                }

                if (AllowsReset(resetScopePlan, reference.ResetBoundaryEligibility))
                {
                    filtered.Add(reference);
                }
            }

            filtered.Sort(static (left, right) => string.Compare(left.CapabilityId, right.CapabilityId, StringComparison.Ordinal));
            return filtered;
        }

        internal static string FormatResetBoundaryEligibility(ActivityResetBoundaryEligibility eligibility)
        {
            return ActivityResetBoundaryEligibilityFormatter.Format(eligibility);
        }

        private static void LogDecision(
            ActivityResetScopePlan plan,
            string outcome,
            string outcomeReason)
        {
            SessionActivityIdentity identity = plan.Identity;
            DebugUtility.LogVerbose(
                typeof(ActivityResetBoundaryPolicy),
                $"event='ActivityResetScopePlanResolved' owner='{Owner}' policyId='{plan.PolicyId}' resetIntent='{plan.ResetIntent}' resetStateProfile='{plan.StateProfileKind}' boundaryKind='{plan.BoundaryKind}' targetScope='{plan.TargetScope}' boundaryEligibilityRequired='{ResolveEligibility(plan.BoundaryKind)}' outcome='{outcome.TrimToEmpty()}' outcomeReason='{outcomeReason.TrimToEmpty()}' behaviorMode='ResetIntentStateProfilePolicy' pipelineId='{identity.PipelineId.TrimToEmpty()}' sessionStateId='{identity.SessionId.TrimToEmpty()}' activityId='{identity.ActivityId.TrimToEmpty()}' entrySequence='{identity.EntrySequence}' source='{plan.Source.TrimToEmpty()}' reason='{plan.Reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Info);
        }
}
}
