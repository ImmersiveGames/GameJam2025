using System;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Players.Runtime
{
    public enum PlayerActorParticipationStateKind
    {
        Unknown = 0,
        ActiveInActivity = 1,
        ExitedActivity = 2,
    }

    [DisallowMultipleComponent]
    public sealed class PlayerActorParticipationState : MonoBehaviour, IActorResetEndpoint, IActorResetContributionProvider
    {
        private static readonly ActorResetGroup[] ActivityParticipationResetGroups =
        {
            ActorResetGroup.ActivityParticipation,
        };

        [SerializeField, HideInInspector] private PlayerActorParticipationStateKind participationState = PlayerActorParticipationStateKind.ActiveInActivity;
        [SerializeField, HideInInspector] private string currentActivityId;
        [SerializeField, HideInInspector] private int currentEntrySequence;

        public PlayerActorParticipationStateKind ParticipationState => participationState;
        public string CurrentActivityId => string.IsNullOrWhiteSpace(currentActivityId) ? string.Empty : currentActivityId.Trim();
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

            contribution = new PlayerActorActivityParticipationResetContribution(context);
            return true;
        }

        public bool Supports(ActorResetGroup group)
        {
            return group == ActorResetGroup.ActivityParticipation;
        }

        public void ApplyReset(ActorResetContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("PlayerActorParticipationState received invalid reset context.");
            }

            if (context.Group != ActorResetGroup.ActivityParticipation)
            {
                throw new InvalidOperationException(
                    $"PlayerActorParticipationState received unsupported reset group='{context.Group}' for actorId='{context.Actor.ActorId}'.");
            }

            MarkActiveInActivity(context.PipelineIdentity);
        }

        private readonly struct PlayerActorActivityParticipationResetContribution : IActorResetContribution
        {
            public PlayerActorActivityParticipationResetContribution(ActorCapabilityContributionContext context)
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
            }

            public ActorCapabilityContributionDescriptor Descriptor { get; }
            public ActorResetGroup[] SupportedGroups => ActivityParticipationResetGroups;
            public bool IsValid => Descriptor.IsValid && SupportedGroups is { Length: > 0 };
        }
    }
}
