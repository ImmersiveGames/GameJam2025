using System;
using System.Text;

namespace ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime
{
    /// <summary>
    /// Stable semantic identity for a participant.
    /// This is intentionally separate from any actor runtime identifier.
    /// </summary>
    public readonly struct ParticipantId : IEquatable<ParticipantId>
    {
        public ParticipantId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static ParticipantId None => default;

        public bool Equals(ParticipantId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ParticipantId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid ? Value : "<none>";
        }

        public static bool operator ==(ParticipantId left, ParticipantId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ParticipantId left, ParticipantId right)
        {
            return !left.Equals(right);
        }
    }

    public enum ParticipantKind
    {
        Unknown = 0,
        Player = 1,
        Actor = 2,
        Spectator = 3,
        System = 4
    }

    public enum OwnershipKind
    {
        Unknown = 0,
        Authoring = 1,
        Local = 2,
        Remote = 3,
        Shared = 4
    }

    public enum BindingHintKind
    {
        Unknown = 0,
        None = 1,
        LocalPrimary = 2,
        LocalSecondary = 3,
        Remote = 4,
        Shared = 5,
        Custom = 6
    }

    /// <summary>
    /// Semantic binding hint for adjacent consumers such as InputModes.
    /// This does not resolve concrete input devices or PlayerInput.
    /// </summary>
    public readonly struct BindingHint : IEquatable<BindingHint>
    {
        public BindingHint(BindingHintKind kind, string value = "")
        {
            Kind = kind;
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public BindingHintKind Kind { get; }
        public string Value { get; }

        public bool IsValid => Kind != BindingHintKind.Unknown;
        public bool HasValue => !string.IsNullOrWhiteSpace(Value);
        public bool IsLocalBindingCandidate => Kind == BindingHintKind.LocalPrimary || Kind == BindingHintKind.LocalSecondary;
        public bool IsConcreteBindingHint => Kind != BindingHintKind.Unknown && Kind != BindingHintKind.None;

        public static BindingHint None => new(BindingHintKind.None);

        public bool Equals(BindingHint other)
        {
            return Kind == other.Kind && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is BindingHint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
            }
        }

        public override string ToString()
        {
            return HasValue ? $"{Kind}:{Value}" : Kind.ToString();
        }

        public static bool operator ==(BindingHint left, BindingHint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BindingHint left, BindingHint right)
        {
            return !left.Equals(right);
        }
    }

    public enum ParticipationReadinessState
    {
        Unknown = 0,
        NotReady = 1,
        Ready = 2,
        NoContent = 3
    }

    /// <summary>
    /// Block-level readiness snapshot for participation.
    /// </summary>
    public readonly struct ParticipationReadinessSnapshot
    {
        public ParticipationReadinessSnapshot(
            ParticipationReadinessState state,
            string reason,
            int participantCount,
            ParticipantId primaryParticipantId)
        {
            State = state;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            ParticipantCount = participantCount < 0 ? 0 : participantCount;
            PrimaryParticipantId = primaryParticipantId;
        }

        public ParticipationReadinessState State { get; }
        public string Reason { get; }
        public int ParticipantCount { get; }
        public ParticipantId PrimaryParticipantId { get; }

        public bool IsReady => State == ParticipationReadinessState.Ready;
        public bool IsNoContent => State == ParticipationReadinessState.NoContent;
        public bool CanEnterGameplay => IsReady || IsNoContent;
        public bool HasParticipants => ParticipantCount > 0;
        public bool HasPrimaryParticipant => PrimaryParticipantId.IsValid;
        public bool IsValid => State != ParticipationReadinessState.Unknown;

        public static ParticipationReadinessSnapshot Empty => new(
            ParticipationReadinessState.Unknown,
            string.Empty,
            0,
            ParticipantId.None);

        public override string ToString()
        {
            return $"state='{State}', participantCount='{ParticipantCount}', primaryParticipantId='{PrimaryParticipantId}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}'";
        }
    }

    public enum ParticipantLifecycleState
    {
        Unknown = 0,
        Declared = 1,
        Expected = 2,
        Materialized = 3,
        Bound = 4,
        Active = 5,
        Suspended = 6,
        Disconnected = 7,
        Ended = 8
    }

    public enum ParticipantLifecycleTransitionKind
    {
        Unknown = 0,
        DeclaredToExpected = 1,
        ExpectedToMaterialized = 2,
        MaterializedToBound = 3,
        BoundToActive = 4,
        ActiveToSuspended = 5,
        SuspendedToActive = 6,
        ActiveToEnded = 7,
        SuspendedToEnded = 8,
        BoundToEnded = 9,
        MaterializedToEnded = 10,
        ExpectedToEnded = 11,
        DeclaredToEnded = 12,
        DisconnectedToEnded = 13
    }

    /// <summary>
    /// Lifecycle transition contract for participant state changes.
    /// The block can publish snapshots and lifecycle transition events.
    /// </summary>
    public readonly struct ParticipantLifecycleTransition : IEquatable<ParticipantLifecycleTransition>
    {
        public ParticipantLifecycleTransition(
            ParticipantId participantId,
            ParticipantLifecycleState fromState,
            ParticipantLifecycleState toState,
            ParticipantLifecycleTransitionKind kind,
            string source = "",
            string reason = "")
        {
            ParticipantId = participantId;
            FromState = fromState;
            ToState = toState;
            Kind = kind;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public ParticipantId ParticipantId { get; }
        public ParticipantLifecycleState FromState { get; }
        public ParticipantLifecycleState ToState { get; }
        public ParticipantLifecycleTransitionKind Kind { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ParticipantId.IsValid &&
            FromState != ParticipantLifecycleState.Unknown &&
            ToState != ParticipantLifecycleState.Unknown &&
            FromState != ToState &&
            Kind != ParticipantLifecycleTransitionKind.Unknown;

        public bool Equals(ParticipantLifecycleTransition other)
        {
            return ParticipantId == other.ParticipantId &&
                   FromState == other.FromState &&
                   ToState == other.ToState &&
                   Kind == other.Kind &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ParticipantLifecycleTransition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ParticipantId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)FromState;
                hashCode = (hashCode * 397) ^ (int)ToState;
                hashCode = (hashCode * 397) ^ (int)Kind;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"participantId='{ParticipantId}', from='{FromState}', to='{ToState}', kind='{Kind}', source='{(string.IsNullOrWhiteSpace(Source) ? "<none>" : Source)}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}'";
        }
    }

    /// <summary>
    /// Explicit event-shaped contract for lifecycle transitions.
    /// No event bus wiring is implied by this type.
    /// </summary>
    public readonly struct ParticipantLifecycleTransitionEvent : IEquatable<ParticipantLifecycleTransitionEvent>
    {
        public ParticipantLifecycleTransitionEvent(
            ParticipantLifecycleTransition transition,
            ParticipationSignature participationSignature)
        {
            Transition = transition;
            ParticipationSignature = participationSignature;
        }

        public ParticipantLifecycleTransition Transition { get; }
        public ParticipationSignature ParticipationSignature { get; }

        public bool IsValid => Transition.IsValid && ParticipationSignature.IsValid;

        public bool Equals(ParticipantLifecycleTransitionEvent other)
        {
            return Transition.Equals(other.Transition) && ParticipationSignature.Equals(other.ParticipationSignature);
        }

        public override bool Equals(object obj)
        {
            return obj is ParticipantLifecycleTransitionEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Transition.GetHashCode() * 397) ^ ParticipationSignature.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"transition='{Transition}', participationSignature='{ParticipationSignature}'";
        }
    }

    public enum ParticipationPublicationMode
    {
        SnapshotOnly = 0,
        SnapshotAndLifecycleTransitions = 1
    }

    /// <summary>
    /// Semantic participant snapshot.
    /// Actor runtime linkage is intentionally not part of the primary identity axis.
    /// </summary>
    public readonly struct ParticipantSnapshot : IEquatable<ParticipantSnapshot>
    {
        public ParticipantSnapshot(
            ParticipantId participantId,
            ParticipantKind kind,
            OwnershipKind ownershipKind,
            BindingHint bindingHint,
            ParticipantLifecycleState lifecycleState,
            bool isPrimary,
            bool isLocal,
            string authoringRef = "")
        {
            ParticipantId = participantId;
            Kind = kind;
            OwnershipKind = ownershipKind;
            BindingHint = bindingHint;
            LifecycleState = lifecycleState;
            IsPrimary = isPrimary;
            IsLocal = isLocal;
            AuthoringRef = string.IsNullOrWhiteSpace(authoringRef) ? string.Empty : authoringRef.Trim();
        }

        public ParticipantId ParticipantId { get; }
        public ParticipantKind Kind { get; }
        public OwnershipKind OwnershipKind { get; }
        public BindingHint BindingHint { get; }
        public ParticipantLifecycleState LifecycleState { get; }
        public bool IsPrimary { get; }
        public bool IsLocal { get; }
        public string AuthoringRef { get; }

        public bool HasAuthoringRef => !string.IsNullOrWhiteSpace(AuthoringRef);
        public bool IsValid =>
            ParticipantId.IsValid &&
            Kind != ParticipantKind.Unknown &&
            OwnershipKind != OwnershipKind.Unknown &&
            LifecycleState != ParticipantLifecycleState.Unknown &&
            BindingHint.IsValid;

        internal string GetSignatureToken()
        {
            return $"{ParticipantId}|{Kind}|{OwnershipKind}|{LifecycleState}|primary:{IsPrimary}|local:{IsLocal}|binding:{BindingHint}|authoring:{(HasAuthoringRef ? AuthoringRef : "<none>")}";
        }

        public bool Equals(ParticipantSnapshot other)
        {
            return ParticipantId == other.ParticipantId &&
                   Kind == other.Kind &&
                   OwnershipKind == other.OwnershipKind &&
                   BindingHint.Equals(other.BindingHint) &&
                   LifecycleState == other.LifecycleState &&
                   IsPrimary == other.IsPrimary &&
                   IsLocal == other.IsLocal &&
                   string.Equals(AuthoringRef, other.AuthoringRef, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ParticipantSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ParticipantId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Kind;
                hashCode = (hashCode * 397) ^ (int)OwnershipKind;
                hashCode = (hashCode * 397) ^ BindingHint.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)LifecycleState;
                hashCode = (hashCode * 397) ^ IsPrimary.GetHashCode();
                hashCode = (hashCode * 397) ^ IsLocal.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(AuthoringRef ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"participantId='{ParticipantId}', kind='{Kind}', ownershipKind='{OwnershipKind}', lifecycleState='{LifecycleState}', primary='{IsPrimary}', local='{IsLocal}', bindingHint='{BindingHint}', authoringRef='{(string.IsNullOrWhiteSpace(AuthoringRef) ? "<none>" : AuthoringRef)}'";
        }
    }

    /// <summary>
    /// Canonical roster snapshot for the participation block.
    /// The snapshot is the primary state contract; lifecycle transitions are an explicit secondary seam.
    /// </summary>
    public readonly struct ParticipationSnapshot : IEquatable<ParticipationSnapshot>
    {
        public ParticipationSnapshot(
            string sessionSignature,
            string phaseSignature,
            ParticipantSnapshot[] participants,
            ParticipationReadinessSnapshot readiness,
            ParticipationPublicationMode publicationMode = ParticipationPublicationMode.SnapshotAndLifecycleTransitions)
        {
            SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? string.Empty : sessionSignature.Trim();
            PhaseSignature = string.IsNullOrWhiteSpace(phaseSignature) ? string.Empty : phaseSignature.Trim();
            Participants = participants == null ? Array.Empty<ParticipantSnapshot>() : (ParticipantSnapshot[])participants.Clone();
            Readiness = readiness;
            PublicationMode = publicationMode;
            PrimaryParticipantId = ResolveParticipantId(Participants, participant => participant.IsPrimary);
            LocalParticipantId = ResolveParticipantId(Participants, participant => participant.IsLocal);
            ParticipantCount = Participants.Length;
            Signature = ParticipationSignature.FromSnapshot(
                SessionSignature,
                PhaseSignature,
                Participants,
                Readiness,
                PublicationMode,
                PrimaryParticipantId,
                LocalParticipantId);
        }

        public string SessionSignature { get; }
        public string PhaseSignature { get; }
        public ParticipantSnapshot[] Participants { get; }
        public ParticipationReadinessSnapshot Readiness { get; }
        public ParticipationPublicationMode PublicationMode { get; }
        public ParticipantId PrimaryParticipantId { get; }
        public ParticipantId LocalParticipantId { get; }
        public int ParticipantCount { get; }
        public ParticipationSignature Signature { get; }

        public bool HasParticipants => ParticipantCount > 0;
        public bool HasPrimaryParticipant => PrimaryParticipantId.IsValid;
        public bool HasLocalParticipant => LocalParticipantId.IsValid;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SessionSignature) &&
            !string.IsNullOrWhiteSpace(PhaseSignature) &&
            Signature.IsValid;

        public static ParticipationSnapshot Empty => new(
            string.Empty,
            string.Empty,
            Array.Empty<ParticipantSnapshot>(),
            ParticipationReadinessSnapshot.Empty,
            ParticipationPublicationMode.SnapshotAndLifecycleTransitions);

        public bool Equals(ParticipationSnapshot other)
        {
            return string.Equals(SessionSignature, other.SessionSignature, StringComparison.Ordinal) &&
                   string.Equals(PhaseSignature, other.PhaseSignature, StringComparison.Ordinal) &&
                   PublicationMode == other.PublicationMode &&
                   PrimaryParticipantId == other.PrimaryParticipantId &&
                   LocalParticipantId == other.LocalParticipantId &&
                   Readiness.Equals(other.Readiness) &&
                   Signature.Equals(other.Signature) &&
                   ParticipantCount == other.ParticipantCount;
        }

        public override bool Equals(object obj)
        {
            return obj is ParticipationSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(SessionSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PhaseSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)PublicationMode;
                hashCode = (hashCode * 397) ^ PrimaryParticipantId.GetHashCode();
                hashCode = (hashCode * 397) ^ LocalParticipantId.GetHashCode();
                hashCode = (hashCode * 397) ^ Readiness.GetHashCode();
                hashCode = (hashCode * 397) ^ Signature.GetHashCode();
                hashCode = (hashCode * 397) ^ ParticipantCount;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"sessionSignature='{(string.IsNullOrWhiteSpace(SessionSignature) ? "<none>" : SessionSignature)}', phaseSignature='{(string.IsNullOrWhiteSpace(PhaseSignature) ? "<none>" : PhaseSignature)}', participantCount='{ParticipantCount}', primaryParticipantId='{PrimaryParticipantId}', localParticipantId='{LocalParticipantId}', readiness='{Readiness}', publicationMode='{PublicationMode}', signature='{Signature}'";
        }

        public bool TryGetLocalBindingCandidate(out ParticipantSnapshot participant)
        {
            participant = default;

            if (Participants == null)
            {
                return false;
            }

            for (int index = 0; index < Participants.Length; index += 1)
            {
                ParticipantSnapshot current = Participants[index];
                if (!current.IsValid)
                {
                    continue;
                }

                if (current.IsLocal && current.BindingHint.IsLocalBindingCandidate)
                {
                    participant = current;
                    return true;
                }
            }

            return false;
        }

        private static ParticipantId ResolveParticipantId(ParticipantSnapshot[] participants, Func<ParticipantSnapshot, bool> predicate)
        {
            if (participants == null || predicate == null)
            {
                return ParticipantId.None;
            }

            for (int index = 0; index < participants.Length; index += 1)
            {
                ParticipantSnapshot participant = participants[index];
                if (participant.IsValid && predicate(participant))
                {
                    return participant.ParticipantId;
                }
            }

            return ParticipantId.None;
        }
    }

    /// <summary>
    /// Event contract for participation snapshot publication.
    /// Operational seams may consume this as a read-only bridge, but they do not own the roster.
    /// </summary>
    public readonly struct ParticipationSnapshotChangedEvent : ImmersiveGames.GameJam2025.Core.Events.IEvent
    {
        public ParticipationSnapshotChangedEvent(
            ParticipationSnapshot snapshot,
            string source,
            string reason,
            bool isCleared = false)
        {
            Snapshot = snapshot;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            IsCleared = isCleared;
        }

        public ParticipationSnapshot Snapshot { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsCleared { get; }

        public bool IsValid => Snapshot.IsValid || IsCleared;

        public override string ToString()
        {
            return $"snapshot='{Snapshot}', source='{(string.IsNullOrWhiteSpace(Source) ? "<none>" : Source)}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', isCleared='{IsCleared}'";
        }
    }

    /// <summary>
    /// Stable signature for the participation block.
    /// </summary>
    public readonly struct ParticipationSignature : IEquatable<ParticipationSignature>
    {
        public ParticipationSignature(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static ParticipationSignature FromSnapshot(
            string sessionSignature,
            string phaseSignature,
            ParticipantSnapshot[] participants,
            ParticipationReadinessSnapshot readiness,
            ParticipationPublicationMode publicationMode,
            ParticipantId primaryParticipantId,
            ParticipantId localParticipantId)
        {
            var builder = new StringBuilder();
            builder.Append(string.IsNullOrWhiteSpace(sessionSignature) ? "<no-session>" : sessionSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(phaseSignature) ? "<no-phase>" : phaseSignature.Trim());
            builder.Append('|');
            builder.Append("participants:");
            builder.Append(participants == null ? 0 : participants.Length);
            builder.Append('|');
            builder.Append("primary:");
            builder.Append(primaryParticipantId);
            builder.Append('|');
            builder.Append("local:");
            builder.Append(localParticipantId);
            builder.Append('|');
            builder.Append("readiness:");
            builder.Append(readiness.State);
            builder.Append('|');
            builder.Append("publication:");
            builder.Append(publicationMode);

            if (participants != null)
            {
                for (int index = 0; index < participants.Length; index += 1)
                {
                    builder.Append('|');
                    builder.Append("p:");
                    builder.Append(participants[index].GetSignatureToken());
                }
            }

            return new ParticipationSignature(builder.ToString());
        }

        public bool Equals(ParticipationSignature other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ParticipationSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid ? Value : "<none>";
        }

        public static bool operator ==(ParticipationSignature left, ParticipationSignature right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ParticipationSignature left, ParticipationSignature right)
        {
            return !left.Equals(right);
        }
    }
}

