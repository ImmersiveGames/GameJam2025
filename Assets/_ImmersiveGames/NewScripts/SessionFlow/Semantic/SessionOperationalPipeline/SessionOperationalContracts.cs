using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    public enum SessionOperationalStage
    {
        Unknown = 0,
        EnvelopeStarted = 1,
        CurtainClosed = 2,
        SessionOperationalSetupExecuting = 3,
        InitialActivitySelected = 4,
        SessionActivityEntryHandoffPrepared = 5,
        ReadyToOpenCurtain = 6,
        Completed = 7,
    }

    public readonly struct SessionOperationalIdentity : IEquatable<SessionOperationalIdentity>
    {
        public SessionOperationalIdentity(
            string sessionPipelineId,
            string sessionStateId,
            string routeId,
            string routeProfileId,
            int transitionSequence,
            SessionOperationalStage stage,
            string source)
        {
            SessionPipelineId = Normalize(sessionPipelineId);
            SessionStateId = Normalize(sessionStateId);
            RouteId = Normalize(routeId);
            RouteProfileId = Normalize(routeProfileId);
            TransitionSequence = transitionSequence < 0 ? 0 : transitionSequence;
            Stage = stage;
            Source = Normalize(source);
            CycleSignature = BuildCycleSignature(
                SessionPipelineId,
                SessionStateId,
                RouteId,
                RouteProfileId,
                TransitionSequence,
                Stage);
        }

        public string SessionPipelineId { get; }
        public string SessionStateId { get; }
        public string RouteId { get; }
        public string RouteProfileId { get; }
        public int TransitionSequence { get; }
        public SessionOperationalStage Stage { get; }
        public string Source { get; }
        public string CycleSignature { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SessionPipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(RouteId) &&
            !string.IsNullOrWhiteSpace(RouteProfileId) &&
            TransitionSequence > 0 &&
            Stage != SessionOperationalStage.Unknown &&
            !string.IsNullOrWhiteSpace(CycleSignature);

        public static SessionOperationalIdentity Empty => default;

        public bool Equals(SessionOperationalIdentity other)
        {
            return string.Equals(CycleSignature, other.CycleSignature, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionOperationalIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(CycleSignature ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid
                ? $"sessionPipelineId='{SessionPipelineId}', sessionStateId='{SessionStateId}', routeId='{RouteId}', routeProfileId='{RouteProfileId}', transitionSequence='{TransitionSequence}', stage='{Stage}'"
                : "<none>";
        }

        public static bool operator ==(SessionOperationalIdentity left, SessionOperationalIdentity right) => left.Equals(right);
        public static bool operator !=(SessionOperationalIdentity left, SessionOperationalIdentity right) => !left.Equals(right);

        private static string BuildCycleSignature(
            string sessionPipelineId,
            string sessionStateId,
            string routeId,
            string routeProfileId,
            int transitionSequence,
            SessionOperationalStage stage)
        {
            return $"{sessionPipelineId}|{sessionStateId}|{routeId}|{routeProfileId}|{transitionSequence}|{stage}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivityEntryHandoff : IEquatable<SessionActivityEntryHandoff>
    {
        public SessionActivityEntryHandoff(
            string activityId,
            int activityOrdinal,
            int entrySequence,
            string sessionStateId,
            string source,
            string reason)
        {
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            SessionStateId = Normalize(sessionStateId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public string SessionStateId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(Source);

        public bool Equals(SessionActivityEntryHandoff other)
        {
            return string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal) &&
                   ActivityOrdinal == other.ActivityOrdinal &&
                   EntrySequence == other.EntrySequence &&
                   string.Equals(SessionStateId, other.SessionStateId, StringComparison.Ordinal) &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionActivityEntryHandoff other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ActivityId ?? string.Empty);
                hashCode = (hashCode * 397) ^ ActivityOrdinal;
                hashCode = (hashCode * 397) ^ EntrySequence;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SessionStateId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return IsValid
                ? $"activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', entrySequence='{EntrySequence}', sessionStateId='{SessionStateId}'"
                : "<none>";
        }

        public static bool operator ==(SessionActivityEntryHandoff left, SessionActivityEntryHandoff right) => left.Equals(right);
        public static bool operator !=(SessionActivityEntryHandoff left, SessionActivityEntryHandoff right) => !left.Equals(right);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionOperationalFactKind
    {
        Unknown = 0,
        EnvelopeStarted = 1,
        CurtainClosed = 2,
        SessionOperationalSetupExecuting = 3,
        InitialActivitySelected = 4,
        SessionActivityEntryHandoffPrepared = 5,
        ReadyToOpenCurtain = 6,
        Completed = 7,
        CommandRejected = 8,
    }

    public readonly struct SessionOperationalFact
    {
        public SessionOperationalFact(
            SessionOperationalFactKind kind,
            SessionOperationalIdentity identity,
            string source,
            string reason,
            string message,
            SessionActivityEntryHandoff activityEntryHandoff)
        {
            Kind = kind;
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
            ActivityEntryHandoff = activityEntryHandoff;
        }

        public SessionOperationalFactKind Kind { get; }
        public SessionOperationalIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }
        public SessionActivityEntryHandoff ActivityEntryHandoff { get; }

        public bool IsValid =>
            Kind != SessionOperationalFactKind.Unknown &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', source='{Source}', reason='{Reason}', message='{Message}', handoff='{ActivityEntryHandoff}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionOperationalSnapshot
    {
        public SessionOperationalSnapshot(
            SessionOperationalIdentity identity,
            SessionActivityDefinition initialActivity,
            SessionActivityEntryHandoff activityEntryHandoff,
            string source,
            string reason,
            string message)
        {
            Identity = identity;
            InitialActivity = initialActivity;
            ActivityEntryHandoff = activityEntryHandoff;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public SessionOperationalIdentity Identity { get; }
        public SessionActivityDefinition InitialActivity { get; }
        public SessionActivityEntryHandoff ActivityEntryHandoff { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid =>
            Identity.IsValid &&
            InitialActivity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', initialActivity='{InitialActivity}', handoff='{ActivityEntryHandoff}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionOperationalResultKind
    {
        Unknown = 0,
        Completed = 1,
        Rejected = 2,
    }

    public readonly struct SessionOperationalResult
    {
        public SessionOperationalResult(
            SessionOperationalResultKind kind,
            SessionOperationalIdentity identity,
            SessionActivityEntryHandoff activityEntryHandoff,
            SessionActivityCommandResult activityResult,
            IReadOnlyList<SessionOperationalFact> facts,
            IReadOnlyList<SessionOperationalSnapshot> snapshots,
            string reason)
        {
            Kind = kind;
            Identity = identity;
            ActivityEntryHandoff = activityEntryHandoff;
            ActivityResult = activityResult;
            Facts = facts ?? Array.Empty<SessionOperationalFact>();
            Snapshots = snapshots ?? Array.Empty<SessionOperationalSnapshot>();
            Reason = Normalize(reason);
        }

        public SessionOperationalResultKind Kind { get; }
        public SessionOperationalIdentity Identity { get; }
        public SessionActivityEntryHandoff ActivityEntryHandoff { get; }
        public SessionActivityCommandResult ActivityResult { get; }
        public IReadOnlyList<SessionOperationalFact> Facts { get; }
        public IReadOnlyList<SessionOperationalSnapshot> Snapshots { get; }
        public string Reason { get; }

        public bool IsValid =>
            Kind != SessionOperationalResultKind.Unknown &&
            Identity.IsValid;

        public bool IsCompleted => Kind == SessionOperationalResultKind.Completed;
        public bool IsRejected => Kind == SessionOperationalResultKind.Rejected;

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', handoff='{ActivityEntryHandoff}', activityResult='{ActivityResult}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
