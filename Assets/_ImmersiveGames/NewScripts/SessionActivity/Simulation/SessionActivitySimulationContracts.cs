using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Simulation
{
    public enum ActivityExecutionBlockingCommandKind
    {
        Unknown = 0,
        BlockActivityExecution = 1,
        ReleaseActivityExecution = 2,
        BlockSessionSimulation = 3,
        ReleaseSessionSimulation = 4,
    }

    public enum ActivityExecutionBlockingFactKind
    {
        Unknown = 0,
        ActivityExecutionBlocked = 1,
        ActivityExecutionReleased = 2,
        SessionExecutionBlocked = 3,
        SessionExecutionReleased = 4,
        ActivityExecutionBlockingCommandRejected = 5,
    }

    [Serializable]
    public readonly struct ActivityExecutionBlockingIdentity : IEquatable<ActivityExecutionBlockingIdentity>
    {
        private readonly string _pipelineId;
        private readonly string _sessionStateId;
        private readonly string _activityId;
        private readonly int _activityOrdinal;
        private readonly int _entrySequence;
        private readonly SessionActivityStage _stage;
        private readonly string _source;
        private readonly string _reason;

        public ActivityExecutionBlockingIdentity(
            string pipelineId,
            string sessionStateId,
            string activityId,
            int activityOrdinal,
            int entrySequence,
            SessionActivityStage stage,
            string source,
            string reason)
        {
            _pipelineId = Normalize(pipelineId);
            _sessionStateId = Normalize(sessionStateId);
            _activityId = Normalize(activityId);
            _activityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            _entrySequence = entrySequence < 0 ? 0 : entrySequence;
            _stage = stage;
            _source = Normalize(source);
            _reason = Normalize(reason);
        }

        public string PipelineId => _pipelineId ?? string.Empty;
        public string SessionStateId => _sessionStateId ?? string.Empty;
        public string ActivityId => _activityId ?? string.Empty;
        public int ActivityOrdinal => _activityOrdinal;
        public int EntrySequence => _entrySequence;
        public SessionActivityStage Stage => _stage;
        public string Source => _source ?? string.Empty;
        public string Reason => _reason ?? string.Empty;

        public bool HasSessionScope =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        public bool HasActivityScope =>
            HasSessionScope &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            Stage != SessionActivityStage.Unknown;

        public bool MatchesSessionScope(ActivityExecutionBlockingIdentity other)
        {
            return string.Equals(PipelineId, other.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(SessionStateId, other.SessionStateId, StringComparison.Ordinal);
        }

        public bool MatchesActivityScope(ActivityExecutionBlockingIdentity other)
        {
            return MatchesSessionScope(other) &&
                   string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal) &&
                   ActivityOrdinal == other.ActivityOrdinal &&
                   EntrySequence == other.EntrySequence &&
                   Stage == other.Stage;
        }

        public bool Equals(ActivityExecutionBlockingIdentity other)
        {
            return string.Equals(PipelineId, other.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(SessionStateId, other.SessionStateId, StringComparison.Ordinal) &&
                   string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal) &&
                   ActivityOrdinal == other.ActivityOrdinal &&
                   EntrySequence == other.EntrySequence &&
                   Stage == other.Stage &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is ActivityExecutionBlockingIdentity other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(
                PipelineId ?? string.Empty,
                SessionStateId ?? string.Empty,
                ActivityId ?? string.Empty,
                ActivityOrdinal,
                EntrySequence,
                Stage,
                Source ?? string.Empty,
                Reason ?? string.Empty);
        }

        public override string ToString()
        {
            return $"pipelineId='{PipelineId}', sessionStateId='{SessionStateId}', activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', entrySequence='{EntrySequence}', stage='{Stage}', source='{Source}', reason='{Reason}'";
        }

        public static bool operator ==(ActivityExecutionBlockingIdentity left, ActivityExecutionBlockingIdentity right) => left.Equals(right);
        public static bool operator !=(ActivityExecutionBlockingIdentity left, ActivityExecutionBlockingIdentity right) => !left.Equals(right);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public readonly struct ActivityExecutionBlockingCommand
    {
        public ActivityExecutionBlockingCommand(
            ActivityExecutionBlockingCommandKind kind,
            ActivityExecutionBlockingIdentity identity,
            string source,
            string reason)
        {
            Kind = kind;
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActivityExecutionBlockingCommandKind Kind { get; }
        public ActivityExecutionBlockingIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => Kind != ActivityExecutionBlockingCommandKind.Unknown;

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public readonly struct SimulationGateFact
    {
        public SimulationGateFact(
            ActivityExecutionBlockingFactKind kind,
            ActivityExecutionBlockingIdentity identity,
            string source,
            string reason,
            string message)
        {
            Kind = kind;
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActivityExecutionBlockingFactKind Kind { get; }
        public ActivityExecutionBlockingIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid => Kind != ActivityExecutionBlockingFactKind.Unknown;

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public readonly struct SimulationGateSnapshot
    {
        public SimulationGateSnapshot(
            ActivityExecutionBlockingCommandKind commandKind,
            ActivityExecutionBlockingIdentity commandIdentity,
            bool sessionBlocked,
            ActivityExecutionBlockingIdentity sessionIdentity,
            bool activityBlocked,
            ActivityExecutionBlockingIdentity activityIdentity,
            SimulationGateFact lastFact,
            string source,
            string reason,
            string message)
        {
            CommandKind = commandKind;
            CommandIdentity = commandIdentity;
            SessionBlocked = sessionBlocked;
            SessionIdentity = sessionIdentity;
            ActivityBlocked = activityBlocked;
            ActivityIdentity = activityIdentity;
            LastFact = lastFact;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActivityExecutionBlockingCommandKind CommandKind { get; }
        public ActivityExecutionBlockingIdentity CommandIdentity { get; }
        public bool SessionBlocked { get; }
        public ActivityExecutionBlockingIdentity SessionIdentity { get; }
        public bool ActivityBlocked { get; }
        public ActivityExecutionBlockingIdentity ActivityIdentity { get; }
        public SimulationGateFact LastFact { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid => true;

        public override string ToString()
        {
            return $"commandKind='{CommandKind}', commandIdentity='{CommandIdentity}', sessionBlocked='{SessionBlocked}', sessionIdentity='{SessionIdentity}', activityBlocked='{ActivityBlocked}', activityIdentity='{ActivityIdentity}', lastFact='{LastFact}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public sealed class ActivityExecutionBlockingState
    {
        public bool SessionBlocked { get; internal set; }
        public ActivityExecutionBlockingIdentity SessionIdentity { get; internal set; }
        public bool ActivityBlocked { get; internal set; }
        public ActivityExecutionBlockingIdentity ActivityIdentity { get; internal set; }
        public SimulationGateFact LastFact { get; internal set; }
        public SimulationGateSnapshot LastSnapshot { get; internal set; }

        public void Reset()
        {
            SessionBlocked = false;
            SessionIdentity = default;
            ActivityBlocked = false;
            ActivityIdentity = default;
            LastFact = default;
            LastSnapshot = default;
        }

        public override string ToString()
        {
            return $"sessionBlocked='{SessionBlocked}', sessionIdentity='{SessionIdentity}', activityBlocked='{ActivityBlocked}', activityIdentity='{ActivityIdentity}', lastFact='{LastFact}', lastSnapshot='{LastSnapshot}'";
        }
    }

    [Serializable]
    public readonly struct ActivityExecutionBlockingResult
    {
        public ActivityExecutionBlockingResult(
            ActivityExecutionBlockingCommand command,
            IReadOnlyList<SimulationGateFact> facts,
            SimulationGateSnapshot snapshot,
            string reason)
        {
            Command = command;
            Facts = facts ?? Array.Empty<SimulationGateFact>();
            Snapshot = snapshot;
            Reason = Normalize(reason);
        }

        public ActivityExecutionBlockingCommand Command { get; }
        public IReadOnlyList<SimulationGateFact> Facts { get; }
        public SimulationGateSnapshot Snapshot { get; }
        public string Reason { get; }

        public bool IsValid => Facts.Count > 0;

        public bool IsRejected => Facts.Count > 0 && Facts[Facts.Count - 1].Kind == ActivityExecutionBlockingFactKind.ActivityExecutionBlockingCommandRejected;
        public bool IsAccepted => !IsRejected;

        public override string ToString()
        {
            return $"command='{Command}', factsCount='{Facts.Count}', reason='{Reason}', snapshot='{Snapshot}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

