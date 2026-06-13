using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ActivityObjectDefaultResetEndpoint : MonoBehaviour,
        IActivityObjectEntryInitializeResetEndpoint,
        IActivityObjectRuntimeLocalResetEndpoint,
        IActivityObjectRuntimeActivityResetEndpoint,
        IActivityObjectRuntimeActivityTransitionResetEndpoint,
        IActivityObjectRuntimeRouteTransitionResetEndpoint,
        IActivityObjectLifecycleContributionProvider
    {
        public void CollectActivityObjectLifecycleContributions(
            ActivityObjectLifecycleContributionContext context,
            IList<IActivityObjectLifecycleContribution> contributions)
        {
            if (contributions == null || !context.IsValid)
            {
                return;
            }

            contributions.Add(new ActivityObjectResetContribution(
                $"activity_object.reset:{context.TargetId}:{nameof(ActivityObjectDefaultResetEndpoint)}",
                100,
                this));
        }


        public ActivityObjectResetResult ApplyEntryInitializeReset(ActivityObjectResetCommand command)
        {
            return ApplyStateProfileReset(
                command,
                ActivityResetIntent.EntryInitialize,
                ActivityResetStateProfileKind.InitialState,
                nameof(IActivityObjectEntryInitializeResetEndpoint),
                "entry_initialize_object_state_profile");
        }

        public ActivityObjectResetResult ApplyRuntimeLocalReset(ActivityObjectResetCommand command)
        {
            return ApplyStateProfileReset(
                command,
                ActivityResetIntent.RuntimeLocalReset,
                ActivityResetStateProfileKind.RuntimeLocalState,
                nameof(IActivityObjectRuntimeLocalResetEndpoint),
                "runtime_local_object_state_profile");
        }

        public ActivityObjectResetResult ApplyRuntimeActivityReset(ActivityObjectResetCommand command)
        {
            return ApplyStateProfileReset(
                command,
                ActivityResetIntent.RuntimeActivityReset,
                ActivityResetStateProfileKind.RuntimeActivityState,
                nameof(IActivityObjectRuntimeActivityResetEndpoint),
                "runtime_activity_object_state_profile");
        }

        public ActivityObjectResetResult ApplyRuntimeActivityTransitionReset(ActivityObjectResetCommand command)
        {
            return ApplyStateProfileReset(
                command,
                ActivityResetIntent.RuntimeActivityTransitionReset,
                ActivityResetStateProfileKind.RuntimeActivityTransitionState,
                nameof(IActivityObjectRuntimeActivityTransitionResetEndpoint),
                "runtime_activity_transition_object_state_profile");
        }

        public ActivityObjectResetResult ApplyRuntimeRouteTransitionReset(ActivityObjectResetCommand command)
        {
            return ApplyStateProfileReset(
                command,
                ActivityResetIntent.RuntimeRouteTransitionReset,
                ActivityResetStateProfileKind.RuntimeRouteTransitionState,
                nameof(IActivityObjectRuntimeRouteTransitionResetEndpoint),
                "runtime_route_transition_object_state_profile");
        }

        private ActivityObjectResetResult ApplyStateProfileReset(
            ActivityObjectResetCommand command,
            ActivityResetIntent expectedIntent,
            ActivityResetStateProfileKind expectedStateProfile,
            string resetHandler,
            string objectProfileSource)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityObjectResetCommand is invalid.");
            }

            if (expectedIntent != ActivityResetIntent.Unknown && command.ResetIntent != expectedIntent)
            {
                throw new InvalidOperationException(
                    $"activity_object_reset_intent_mismatch: expected='{expectedIntent}' actual='{command.ResetIntent}' targetId='{command.TargetId}'.");
            }

            if (expectedStateProfile != ActivityResetStateProfileKind.Unknown && command.StateProfileKind != expectedStateProfile)
            {
                throw new InvalidOperationException(
                    $"activity_object_reset_state_profile_mismatch: expected='{expectedStateProfile}' actual='{command.StateProfileKind}' targetId='{command.TargetId}'.");
            }

            // Endpoint mínimo: a receita real de transform/estado ainda pertence aos próximos cortes de object reset.
            DebugUtility.Log(
                typeof(ActivityObjectDefaultResetEndpoint),
                $"[OBS][ActivityObjectReset] event='ActivityObjectStateProfileApplied' targetId='{command.TargetId}' roleId='{(string.IsNullOrWhiteSpace(command.RoleId) ? "<none>" : command.RoleId)}' contributorKind='{command.ContributorKind}' resetIntent='{command.ResetIntent}' resetStateProfile='{command.StateProfileKind}' objectProfileKind='{command.StateProfileKind}' objectProfileSource='{objectProfileSource}' resetHandler='{resetHandler}' resetDescriptor='{command.ResetDescriptorMetadata}' descriptorMode='endpoint_inventory' executionMode='intent_handler_per_report' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new ActivityObjectResetResult(
                ActivityObjectResetResultKind.Applied,
                command,
                command.Source,
                command.Reason,
                $"Activity object reset applied targetId='{command.TargetId}' resetIntent='{command.ResetIntent}' resetStateProfile='{command.StateProfileKind}' resetHandler='{resetHandler}' objectProfileKind='{command.StateProfileKind}' objectProfileSource='{objectProfileSource}' resetDescriptor='{command.ResetDescriptorMetadata}' executionMode='intent_handler_per_report' endpoint='{nameof(ActivityObjectDefaultResetEndpoint)}'.");
        }
    }
}
