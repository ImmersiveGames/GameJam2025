using System;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    public enum PlayerActorParticipationStateKind
    {
        Unknown = 0,
        ActiveInActivity = 1,
        ExitedActivity = 2
    }

    [DisallowMultipleComponent]
    public sealed class PlayerActorParticipationState : MonoBehaviour, IActorEntryInitializeResetEndpoint, IActorRuntimeLocalResetEndpoint, IActorRuntimeActivityResetEndpoint, IActorRuntimeActivityTransitionResetEndpoint, IActorRuntimeRouteTransitionResetEndpoint, IActorResetContributionProvider
    {
        [SerializeField] [HideInInspector] private PlayerActorParticipationStateKind participationState = PlayerActorParticipationStateKind.ActiveInActivity;
        [SerializeField] [HideInInspector] private string currentActivityId;
        [SerializeField] [HideInInspector] private int currentEntrySequence;
        [Header("Reset")]
        [SerializeField] private ActivityResetBoundaryEligibility resetBoundaryEligibility = ActivityResetBoundaryEligibility.RuntimeAll;

        public PlayerActorParticipationStateKind ParticipationState => participationState;
        public string CurrentActivityId => currentActivityId.TrimToEmpty();
        public int CurrentEntrySequence => currentEntrySequence;

        public void MarkActiveInActivity(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                Clear();
                return;
            }

            currentActivityId = identity.ActivityId;
            currentEntrySequence = identity.EntrySequence;
            participationState = PlayerActorParticipationStateKind.ActiveInActivity;
        }

        public void MarkExitedActivity()
        {
            participationState = PlayerActorParticipationStateKind.ExitedActivity;
        }

        public void Clear()
        {
            currentActivityId = string.Empty;
            currentEntrySequence = 0;
            participationState = PlayerActorParticipationStateKind.Unknown;
        }

        public bool TryCreateResetContribution(
            ActorCapabilityContributionContext context,
            out IActorResetContribution contribution)
        {
            if (!context.IsValid)
            {
                contribution = null;
                return false;
            }

            contribution = new PlayerActorActivityParticipationResetContribution(context, resetBoundaryEligibility);
            return true;
        }

        public void ApplyEntryInitializeReset(ActorResetContext context)
        {
            ApplyParticipationStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.InitialState),
                "entry_initialize_activity_participation_active");
        }

        public void ApplyRuntimeLocalReset(ActorResetContext context)
        {
            ApplyParticipationStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeLocalState),
                "runtime_local_activity_participation_reaffirm_active");
        }

        public void ApplyRuntimeActivityReset(ActorResetContext context)
        {
            ApplyParticipationStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeActivityState),
                "runtime_activity_activity_participation_reenter_active");
        }

        public void ApplyRuntimeActivityTransitionReset(ActorResetContext context)
        {
            ApplyParticipationStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeActivityTransitionState),
                "runtime_activity_transition_activity_participation_active_in_next_activity");
        }

        public void ApplyRuntimeRouteTransitionReset(ActorResetContext context)
        {
            ApplyParticipationStateProfile(
                context,
                nameof(ActivityResetStateProfileKind.RuntimeRouteTransitionState),
                "runtime_route_transition_activity_participation_clear");
        }

        private void ApplyParticipationStateProfile(
            ActorResetContext context,
            string participationProfileKind,
            string participationProfileSource)
        {
            EnsureParticipationResetContext(context, participationProfileKind);

            var stateBefore = participationState;
            string activityBefore = CurrentActivityId;
            int entrySequenceBefore = currentEntrySequence;

            if (context.ResetIntent == ActivityResetIntent.RuntimeRouteTransitionReset)
            {
                Clear();
            }
            else
            {
                MarkActiveInActivity(context.PipelineIdentity);
            }

            DebugUtility.LogVerbose(typeof(PlayerActorParticipationState),
                $"event='ParticipationStateProfileApplied' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}' participationProfileKind='{participationProfileKind}' participationProfileSource='{participationProfileSource}' participationStateBefore='{stateBefore}' participationStateAfter='{participationState}' activityIdBefore='{activityBefore}' activityIdAfter='{CurrentActivityId}' entrySequenceBefore='{entrySequenceBefore}' entrySequenceAfter='{currentEntrySequence}' source='{context.Source}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info, this);
        }

        private static void EnsureParticipationResetContext(ActorResetContext context, string operation)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException($"PlayerActorParticipationState received invalid reset context for operation='{operation}'.");
            }

        }

        private readonly struct PlayerActorActivityParticipationResetContribution : IActorResetContribution
        {
            public PlayerActorActivityParticipationResetContribution(ActorCapabilityContributionContext context, ActivityResetBoundaryEligibility resetBoundaryEligibility)
            {
                Descriptor = new ActorCapabilityContributionDescriptor(
                    new ActorCapabilityId("actor.capability.player.activity_participation"),
                    ActorCapabilityContributionPhase.Reset,
                    ActorCapabilityContributionRequirement.Optional,
                    context.ActorId,
                    context.ActorInstanceRuntimeId,
                    context.ActorKind,
                    context.ActorRole,
                    context.ActorScope,
                    context.ComponentPath,
                    nameof(PlayerActorParticipationState),
                    "player_actor_activity_participation_reset_contribution");
                ResetBoundaryEligibility = resetBoundaryEligibility;
            }

            public ActorCapabilityContributionDescriptor Descriptor { get; }
            public ActivityResetBoundaryEligibility ResetBoundaryEligibility { get; }
            public bool IsValid => Descriptor.IsValid;
        }
    }
}
