using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityCatalogAdvanceAtEndMode
    {
        StopAtEnd = 0,
        LoopToFirst = 1,
    }

    public enum SessionActivityStage
    {
        Unknown = 0,
        ActivationExecuting = 1,
        ActivationSkippedNoContent = 2,
        ActivityRunning = 3,
        Deactivation = 4,
        ActivityResultPresentationExecuting = 5,
        ActivityResultPresentationSkippedNoContent = 6,
        Completed = 7,
    }

    public enum ActivityExecutionState
    {
        Unknown = 0,
        Stopped = 1,
        Running = 2,
        Paused = 3,
    }

    public readonly struct SessionActivityIdentity : IEquatable<SessionActivityIdentity>
    {
        public SessionActivityIdentity(
            string pipelineId,
            string sessionStateId,
            string activityId,
            int activityOrdinal,
            int entrySequence,
            SessionActivityStage stage,
            string source)
        {
            PipelineId = Normalize(pipelineId);
            SessionId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            Stage = stage;
            Source = Normalize(source);
            CycleSignature = BuildCycleSignature(PipelineId, SessionId, ActivityId, ActivityOrdinal, EntrySequence, Stage);
        }

        public string PipelineId { get; }
        public string SessionId { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public SessionActivityStage Stage { get; }
        public string Source { get; }
        public string CycleSignature { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            EntrySequence > 0 &&
            Stage != SessionActivityStage.Unknown &&
            !string.IsNullOrWhiteSpace(CycleSignature);

        public static SessionActivityIdentity Empty => default;

        public bool Equals(SessionActivityIdentity other)
        {
            return string.Equals(CycleSignature, other.CycleSignature, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionActivityIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(CycleSignature ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid
                ? $"pipelineId='{PipelineId}', sessionStateId='{SessionId}', activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', entrySequence='{EntrySequence}', stage='{Stage}'"
                : "<none>";
        }

        public static bool operator ==(SessionActivityIdentity left, SessionActivityIdentity right) => left.Equals(right);
        public static bool operator !=(SessionActivityIdentity left, SessionActivityIdentity right) => !left.Equals(right);

        private static string BuildCycleSignature(string pipelineId, string sessionStateId, string activityId, int activityOrdinal, int entrySequence, SessionActivityStage stage)
        {
            return $"{pipelineId}|{sessionStateId}|{activityId}|{activityOrdinal}|{entrySequence}|{stage}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivityDefinition
    {
        public SessionActivityDefinition(
            string activityId,
            string displayName,
            int activityOrdinal,
            bool hasActivation,
            bool hasGameplayContent,
            bool hasActivityResult,
            string nextActivityId,
            string source)
        {
            ActivityId = Normalize(activityId);
            DisplayName = Normalize(displayName);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            HasActivation = hasActivation;
            HasGameplayContent = hasGameplayContent;
            HasActivityResult = hasActivityResult;
            NextActivityId = Normalize(nextActivityId);
            Source = Normalize(source);
        }

        public string ActivityId { get; }
        public string DisplayName { get; }
        public int ActivityOrdinal { get; }
        public bool HasActivation { get; }
        public bool HasGameplayContent { get; }
        public bool HasActivityResult { get; }
        public string NextActivityId { get; }
        public string Source { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ActivityId) &&
            !string.IsNullOrWhiteSpace(DisplayName) &&
            ActivityOrdinal > 0 &&
            !string.IsNullOrWhiteSpace(Source);

        public bool HasNextActivity => !string.IsNullOrWhiteSpace(NextActivityId);

        public override string ToString()
        {
            return $"activityId='{ActivityId}', displayName='{DisplayName}', ordinal='{ActivityOrdinal}', activation='{HasActivation}', gameplay='{HasGameplayContent}', hasActivityResult='{HasActivityResult}', nextActivityId='{(HasNextActivity ? NextActivityId : "<none>")}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionActivityCommandKind
    {
        Unknown = 0,
        StartActivity = 1,
        CompleteCurrentActivity = 2,
        ContinueToNextActivity = 3,
        GoToNextActivity = 4,
        GoToPreviousActivity = 5,
        RestartCurrentActivity = 6,
        GoToActivity = 7,
        PauseRequested = 8,
        ResumeRequested = 9,
        PauseSimulation = 10,
        ResumeSimulation = 11,
    }

    public readonly struct SessionActivityCommand
    {
        public SessionActivityCommand(
            SessionActivityCommandKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string targetActivityId = null)
        {
            Kind = kind;
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
            TargetActivityId = Normalize(targetActivityId);
        }

        public SessionActivityCommandKind Kind { get; }
        public SessionActivityIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }
        public string TargetActivityId { get; }

        public bool IsValid =>
            Kind != SessionActivityCommandKind.Unknown &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            (Kind != SessionActivityCommandKind.GoToActivity || !string.IsNullOrWhiteSpace(TargetActivityId));

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', source='{Source}', reason='{Reason}', targetActivityId='{(string.IsNullOrWhiteSpace(TargetActivityId) ? "<none>" : TargetActivityId)}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionActivityFactKind
    {
        Unknown = 0,
        PipelineStarted = 1,
        ActivationEntered = 2,
        ActivationSkippedNoContent = 3,
        ActivityRunningEntered = 4,
        GameplayContentSkippedNoContent = 5,
        ActivityDeactivated = 6,
        ActivityResultPresentationEntered = 7,
        ActivityResultPresentationSkippedNoContent = 8,
        ContinueAccepted = 9,
        ActivityHandoffPrepared = 10,
        PipelineCompleted = 11,
        CommandRejected = 12,
        SimulationPaused = 13,
        SimulationResumed = 14,
        PauseResolved = 15,
        ResumeResolved = 16,
        PauseRejected = 17,
        ResumeRejected = 18,
        ActivityCatalogLooped = 19,
    }

    public readonly struct SessionActivityFact
    {
        public SessionActivityFact(
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string message,
            SessionActivityHandoff handoff)
        {
            Kind = kind;
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
            Handoff = handoff;
        }

        public SessionActivityFactKind Kind { get; }
        public SessionActivityIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }
        public SessionActivityHandoff Handoff { get; }

        public bool IsValid =>
            Kind != SessionActivityFactKind.Unknown &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivitySnapshot
    {
        public SessionActivitySnapshot(
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            SessionActivityHandoff handoff,
            string source,
            string reason,
            string message)
        {
            Identity = identity;
            Definition = definition;
            Handoff = handoff;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityDefinition Definition { get; }
        public SessionActivityHandoff Handoff { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Definition.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"identity='{Identity}', definition='{Definition}', handoff='{Handoff}', source='{Source}', reason='{Reason}', message='{Message}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionActivityCommandResultKind
    {
        Unknown = 0,
        Accepted = 1,
        Rejected = 2,
        Completed = 3,
        SkipNoContent = 4,
    }

    public readonly struct SessionActivityCommandResult
    {
        public SessionActivityCommandResult(
            SessionActivityCommandResultKind kind,
            SessionActivityCommand command,
            IReadOnlyList<SessionActivityFact> facts,
            string reason)
        {
            Kind = kind;
            Command = command;
            Facts = facts ?? Array.Empty<SessionActivityFact>();
            Reason = Normalize(reason);
        }

        public SessionActivityCommandResultKind Kind { get; }
        public SessionActivityCommand Command { get; }
        public IReadOnlyList<SessionActivityFact> Facts { get; }
        public string Reason { get; }

        public bool IsValid =>
            Kind != SessionActivityCommandResultKind.Unknown &&
            Command.IsValid;

        public bool IsRejected => Kind == SessionActivityCommandResultKind.Rejected;
        public bool IsCompleted => Kind == SessionActivityCommandResultKind.Completed;
        public bool IsSkipNoContent => Kind == SessionActivityCommandResultKind.SkipNoContent;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivityHandoff
    {
        public SessionActivityHandoff(
            SessionActivityIdentity fromIdentity,
            SessionActivityIdentity toIdentity,
            string nextActivityId,
            string source,
            string reason)
        {
            FromIdentity = fromIdentity;
            ToIdentity = toIdentity;
            NextActivityId = Normalize(nextActivityId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity FromIdentity { get; }
        public SessionActivityIdentity ToIdentity { get; }
        public string NextActivityId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            FromIdentity.IsValid &&
            ToIdentity.IsValid &&
            !string.IsNullOrWhiteSpace(NextActivityId) &&
            !string.IsNullOrWhiteSpace(Source);

        public override string ToString()
        {
            return $"from='{FromIdentity}', to='{ToIdentity}', nextActivityId='{NextActivityId}', source='{Source}', reason='{Reason}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionActivityPauseOverlayAdapter
    {
        void Show(SessionActivityIdentity identity, string source, string reason);
        void Hide(SessionActivityIdentity identity, string source, string reason);
    }

    public enum SessionActivityInputModeKind
    {
        Unknown = 0,
        ActivityGameplay = 1,
        PauseOverlay = 2,
        Disabled = 3,
    }

    public readonly struct SessionActivityInputModeCommand
    {
        public SessionActivityInputModeCommand(
            SessionActivityInputModeKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            Kind = kind;
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityInputModeKind Kind { get; }
        public SessionActivityIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Kind != SessionActivityInputModeKind.Unknown &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivityInputModeObservation
    {
        public SessionActivityInputModeObservation(
            SessionActivityInputModeCommand command,
            string fact,
            string snapshot,
            string outcome)
        {
            Command = command;
            Fact = Normalize(fact);
            Snapshot = Normalize(snapshot);
            Outcome = Normalize(outcome);
        }

        public SessionActivityInputModeCommand Command { get; }
        public string Fact { get; }
        public string Snapshot { get; }
        public string Outcome { get; }

        public bool IsValid =>
            Command.IsValid &&
            !string.IsNullOrWhiteSpace(Fact) &&
            !string.IsNullOrWhiteSpace(Snapshot) &&
            !string.IsNullOrWhiteSpace(Outcome);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionActivityInputModeAdapter
    {
        SessionActivityInputModeObservation Apply(SessionActivityInputModeCommand command);
    }
}

