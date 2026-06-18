using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.PlayerParticipation.Contracts
{
    public readonly struct ActivityParticipantBinding
    {
        public ActivityParticipantBinding(
            ActivityParticipantRequirementId requirementId,
            SessionParticipantId participantId,
            SessionParticipantRole role,
            PlayerSlotId playerSlotId,
            PlayerSelectionId playerSelectionId,
            ActorDefinitionId actorDefinitionId,
            ActorId actorId,
            ActorScope actorScope,
            ActorMaterializationPolicyKind materializationPolicy,
            bool required,
            bool requiresPlayerActor,
            bool requiresPlayerInput,
            string source,
            string reason)
        {
            RequirementId = requirementId;
            ParticipantId = participantId;
            Role = role;
            PlayerSlotId = playerSlotId;
            PlayerSelectionId = playerSelectionId;
            ActorDefinitionId = actorDefinitionId;
            ActorId = actorId;
            ActorScope = actorScope;
            MaterializationPolicy = materializationPolicy;
            Required = required;
            RequiresPlayerActor = requiresPlayerActor;
            RequiresPlayerInput = requiresPlayerInput;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActivityParticipantRequirementId RequirementId { get; }
        public SessionParticipantId ParticipantId { get; }
        public SessionParticipantRole Role { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerSelectionId PlayerSelectionId { get; }
        public ActorDefinitionId ActorDefinitionId { get; }
        public ActorId ActorId { get; }
        public ActorScope ActorScope { get; }
        public ActorMaterializationPolicyKind MaterializationPolicy { get; }
        public bool Required { get; }
        public bool RequiresPlayerActor { get; }
        public bool RequiresPlayerInput { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasPlayerSlot => PlayerSlotId.IsValid;
        public bool HasPlayerSelection => PlayerSelectionId.IsValid;
        public bool IsValid =>
            RequirementId.IsValid &&
            ParticipantId.IsValid &&
            Role != SessionParticipantRole.Unknown &&
            ActorDefinitionId.IsValid &&
            ActorId.IsValid &&
            ActorScope != ActorScope.Unknown &&
            MaterializationPolicy != ActorMaterializationPolicyKind.Unknown;
    }

    public sealed class ActivityParticipationContext
    {
        public ActivityParticipationContext(
            SessionActivityIdentity sessionActivityIdentity,
            IReadOnlyList<ActivityParticipantBinding> participants,
            string source,
            string reason)
        {
            SessionActivityIdentity = sessionActivityIdentity;
            Participants = participants ?? Array.Empty<ActivityParticipantBinding>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity SessionActivityIdentity { get; }
        public IReadOnlyList<ActivityParticipantBinding> Participants { get; }
        public string Source { get; }
        public string Reason { get; }

        public int ParticipantCount => Participants?.Count ?? 0;
        public bool HasParticipants => ParticipantCount > 0;
        public bool IsValid => SessionActivityIdentity.IsValid && AreParticipantsValid(Participants);

        private static bool AreParticipantsValid(IReadOnlyList<ActivityParticipantBinding> participants)
        {
            if (participants == null)
            {
                return false;
            }

            for (int i = 0; i < participants.Count; i++)
            {
                if (!participants[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public readonly struct ActorMaterializationRequest
    {
        public ActorMaterializationRequest(
            SessionActivityIdentity identity,
            ActivityParticipantBinding participant,
            string source,
            string reason)
        {
            Identity = identity;
            Participant = participant;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public ActivityParticipantBinding Participant { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && Participant.IsValid;
    }

    public readonly struct ActorMaterializationResult
    {
        public ActorMaterializationResult(
            SessionParticipantId participantId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorCapabilitySurface capabilitySurface,
            bool materialized,
            bool retained,
            string source,
            string reason)
        {
            ParticipantId = participantId;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            CapabilitySurface = capabilitySurface;
            Materialized = materialized;
            Retained = retained;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionParticipantId ParticipantId { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorCapabilitySurface CapabilitySurface { get; }
        public bool Materialized { get; }
        public bool Retained { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasCapabilitySurface => CapabilitySurface != null;
        public bool IsValid =>
            ParticipantId.IsValid &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            (Materialized || Retained);
    }
}
