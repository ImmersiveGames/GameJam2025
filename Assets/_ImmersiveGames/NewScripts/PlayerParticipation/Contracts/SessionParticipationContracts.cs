using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.PlayerParticipation.Contracts
{
    public readonly struct PlayerSlotReservation
    {
        public PlayerSlotReservation(
            PlayerSlotId slotId,
            PlayerSlotKind slotKind,
            PlayerSlotReservationSourceKind sourceKind,
            bool required,
            bool defaultReservation,
            string source,
            string reason)
        {
            SlotId = slotId;
            SlotKind = slotKind;
            SourceKind = sourceKind;
            Required = required;
            DefaultReservation = defaultReservation;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public PlayerSlotId SlotId { get; }
        public PlayerSlotKind SlotKind { get; }
        public PlayerSlotReservationSourceKind SourceKind { get; }
        public bool Required { get; }
        public bool DefaultReservation { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => SlotId.IsValid && SlotKind != PlayerSlotKind.Unknown && SourceKind != PlayerSlotReservationSourceKind.Unknown;
}

    public readonly struct PlayerSelection
    {
        public PlayerSelection(
            PlayerSlotId slotId,
            PlayerSelectionId selectionId,
            ActorDefinitionId actorDefinitionId,
            PlayerSelectionSourceKind sourceKind,
            string source,
            string reason)
        {
            SlotId = slotId;
            SelectionId = selectionId;
            ActorDefinitionId = actorDefinitionId;
            SourceKind = sourceKind;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public PlayerSlotId SlotId { get; }
        public PlayerSelectionId SelectionId { get; }
        public ActorDefinitionId ActorDefinitionId { get; }
        public PlayerSelectionSourceKind SourceKind { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            SlotId.IsValid &&
            SelectionId.IsValid &&
            ActorDefinitionId.IsValid &&
            SourceKind != PlayerSelectionSourceKind.Unknown;
}

    public readonly struct SessionParticipantBinding
    {
        public SessionParticipantBinding(
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
            ParticipantId.IsValid &&
            Role != SessionParticipantRole.Unknown &&
            ActorDefinitionId.IsValid &&
            ActorId.IsValid &&
            ActorScope != ActorScope.Unknown &&
            MaterializationPolicy != ActorMaterializationPolicyKind.Unknown;
}

    public sealed class SessionParticipationContext
    {
        public SessionParticipationContext(
            string routeIdentity,
            string routeOperationId,
            RouteParticipationRequirementKind requirementKind,
            IReadOnlyList<PlayerSlotReservation> slotReservations,
            IReadOnlyList<PlayerSelection> selections,
            IReadOnlyList<SessionParticipantBinding> participants,
            RuntimePlayerJoinPolicyKind runtimeJoinPolicy,
            string source,
            string reason)
            : this(
                string.Empty,
                0,
                routeIdentity,
                routeOperationId,
                requirementKind,
                slotReservations,
                selections,
                participants,
                runtimeJoinPolicy,
                source,
                reason)
        {
        }

        public SessionParticipationContext(
            string sessionId,
            int revision,
            string routeIdentity,
            string routeOperationId,
            RouteParticipationRequirementKind requirementKind,
            IReadOnlyList<PlayerSlotReservation> slotReservations,
            IReadOnlyList<PlayerSelection> selections,
            IReadOnlyList<SessionParticipantBinding> participants,
            RuntimePlayerJoinPolicyKind runtimeJoinPolicy,
            string source,
            string reason)
        {
            SessionId = sessionId.TrimToEmpty();
            Revision = revision;
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            RequirementKind = requirementKind;
            SlotReservations = slotReservations ?? Array.Empty<PlayerSlotReservation>();
            Selections = selections ?? Array.Empty<PlayerSelection>();
            Participants = participants ?? Array.Empty<SessionParticipantBinding>();
            RuntimeJoinPolicy = runtimeJoinPolicy;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string SessionId { get; }
        public int Revision { get; }
        public bool HasSessionId => !string.IsNullOrWhiteSpace(SessionId);
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public RouteParticipationRequirementKind RequirementKind { get; }
        public IReadOnlyList<PlayerSlotReservation> SlotReservations { get; }
        public IReadOnlyList<PlayerSelection> Selections { get; }
        public IReadOnlyList<SessionParticipantBinding> Participants { get; }
        public RuntimePlayerJoinPolicyKind RuntimeJoinPolicy { get; }
        public string Source { get; }
        public string Reason { get; }

        public int SlotReservationCount => SlotReservations?.Count ?? 0;
        public int SelectionCount => Selections?.Count ?? 0;
        public int ParticipantCount => Participants?.Count ?? 0;
        public bool RequiresParticipants => RequirementKind == RouteParticipationRequirementKind.RequiredDefaultable || RequirementKind == RouteParticipationRequirementKind.RequiredExplicit;
        public bool HasParticipants => ParticipantCount > 0;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            Revision >= 0 &&
            RequirementKind != RouteParticipationRequirementKind.Unknown &&
            RuntimeJoinPolicy != RuntimePlayerJoinPolicyKind.Unknown &&
            (!RequiresParticipants || HasParticipants) &&
            AreSlotReservationsValid(SlotReservations) &&
            AreSelectionsValid(Selections) &&
            AreParticipantsValid(Participants);

        public static SessionParticipationContext Empty(
            string routeIdentity,
            string routeOperationId,
            string source,
            string reason)
        {
            return new SessionParticipationContext(
                routeIdentity,
                routeOperationId,
                RouteParticipationRequirementKind.None,
                Array.Empty<PlayerSlotReservation>(),
                Array.Empty<PlayerSelection>(),
                Array.Empty<SessionParticipantBinding>(),
                RuntimePlayerJoinPolicyKind.Unsupported,
                source,
                reason);
        }

        private static bool AreSlotReservationsValid(IReadOnlyList<PlayerSlotReservation> slotReservations)
        {
            if (slotReservations == null)
            {
                return false;
            }

            for (int i = 0; i < slotReservations.Count; i++)
            {
                if (!slotReservations[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreSelectionsValid(IReadOnlyList<PlayerSelection> selections)
        {
            if (selections == null)
            {
                return false;
            }

            for (int i = 0; i < selections.Count; i++)
            {
                if (!selections[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreParticipantsValid(IReadOnlyList<SessionParticipantBinding> participants)
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
}
