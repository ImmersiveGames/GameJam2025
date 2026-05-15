using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum SessionOperationalStage
    {
        Unknown = 0,
        RouteOperationStarted = 1,
        NavigationIntentObserved = 2,
        RouteResolved = 3,
        TransitionRequested = 4,
        TransitionStarted = 5,
        CurtainClosed = 6,
        PreviousRouteTeardownSkipped = 7,
        RoutePhysicalApplyObserved = 8,
        ScenesReadyObserved = 9,
        SessionOperationalSetupNoOp = 10,
        PlayerPreparationObserved = 11,
        InputCapabilityPrepared = 12,
        InitialInputModePrepared = 13,
        PauseCapabilityPrepared = 14,
        ReadyToOpenCurtain = 15,
        TransitionCompletedObserved = 16,
        Completed = 17,
    }

    public readonly struct SessionOperationalIdentity : IEquatable<SessionOperationalIdentity>
    {
        public SessionOperationalIdentity(
            string sessionOperationalPipelineId,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason,
            SessionOperationalStage stage)
        {
            SessionOperationalPipelineId = Normalize(sessionOperationalPipelineId);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            TransitionSequence = transitionSequence < 0 ? 0 : transitionSequence;
            RouteId = Normalize(routeId);
            RouteProfileId = Normalize(routeProfileId);
            Source = Normalize(source);
            Reason = Normalize(reason);
            Stage = stage;
            CycleSignature = BuildCycleSignature(
                SessionOperationalPipelineId,
                RouteOperationId,
                TransitionId,
                TransitionSequence,
                RouteId,
                RouteProfileId,
                Source,
                Reason,
                Stage);
        }

        public string SessionOperationalPipelineId { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int TransitionSequence { get; }
        public string RouteId { get; }
        public string RouteProfileId { get; }
        public string Source { get; }
        public string Reason { get; }
        public SessionOperationalStage Stage { get; }
        public string CycleSignature { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SessionOperationalPipelineId) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            TransitionSequence > 0 &&
            !string.IsNullOrWhiteSpace(RouteId) &&
            !string.IsNullOrWhiteSpace(RouteProfileId) &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason) &&
            Stage != SessionOperationalStage.Unknown &&
            !string.IsNullOrWhiteSpace(CycleSignature);

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
                ? $"sessionOperationalPipelineId='{SessionOperationalPipelineId}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', transitionSequence='{TransitionSequence}', routeId='{RouteId}', routeProfileId='{RouteProfileId}', source='{Source}', reason='{Reason}', stage='{Stage}'"
                : "<none>";
        }

        public static bool operator ==(SessionOperationalIdentity left, SessionOperationalIdentity right) => left.Equals(right);
        public static bool operator !=(SessionOperationalIdentity left, SessionOperationalIdentity right) => !left.Equals(right);

        private static string BuildCycleSignature(
            string sessionOperationalPipelineId,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason,
            SessionOperationalStage stage)
        {
            return $"{sessionOperationalPipelineId}|{routeOperationId}|{transitionId}|{transitionSequence}|{routeId}|{routeProfileId}|{source}|{reason}|{stage}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionOperationalFactKind
    {
        Unknown = 0,
        RouteOperationStarted = 1,
        NavigationIntentObserved = 2,
        RouteResolved = 3,
        TransitionRequested = 4,
        TransitionStarted = 5,
        CurtainClosed = 6,
        PreviousRouteTeardownSkipped = 7,
        RoutePhysicalApplyObserved = 8,
        ScenesReadyObserved = 9,
        SessionOperationalSetupNoOp = 10,
        PlayerPreparationObserved = 11,
        InputCapabilityPrepared = 12,
        InitialInputModePrepared = 13,
        PauseCapabilityPrepared = 14,
        ReadyToOpenCurtain = 15,
        TransitionCompletedObserved = 16,
        Completed = 17,
        IgnoredForeignOrStale = 18,
    }

    public enum SessionOperationalInputModeKind
    {
        Unknown = 0,
        FrontendMenu = 1,
        ActivityDefault = 2,
    }

    public readonly struct SessionOperationalInputModeCommand : IEvent
    {
        public SessionOperationalInputModeCommand(
            SessionOperationalIdentity identity,
            SessionOperationalInputModeKind initialInputMode,
            string routeClass)
        {
            Identity = identity;
            InitialInputMode = initialInputMode;
            RouteClass = string.IsNullOrWhiteSpace(routeClass) ? string.Empty : routeClass.Trim();
        }

        public SessionOperationalIdentity Identity { get; }
        public SessionOperationalInputModeKind InitialInputMode { get; }
        public string RouteClass { get; }
        public string Source => Identity.Source;
        public string Reason => Identity.Reason;
        public string ContextSignature => Identity.CycleSignature;

        public bool IsValid =>
            Identity.IsValid &&
            Identity.Stage == SessionOperationalStage.InitialInputModePrepared &&
            InitialInputMode != SessionOperationalInputModeKind.Unknown;
    }

    public readonly struct SessionOperationalFact
    {
        public SessionOperationalFact(
            SessionOperationalFactKind kind,
            SessionOperationalIdentity identity,
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

        public SessionOperationalFactKind Kind { get; }
        public SessionOperationalIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool IsValid =>
            Kind != SessionOperationalFactKind.Unknown &&
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

    public enum SessionOperationalResultKind
    {
        Unknown = 0,
        Accepted = 1,
        Rejected = 2,
        Completed = 3,
    }

    public readonly struct SessionOperationalResult
    {
        public SessionOperationalResult(
            SessionOperationalResultKind kind,
            SessionOperationalIdentity identity,
            IReadOnlyList<SessionOperationalFact> facts,
            string reason)
        {
            Kind = kind;
            Identity = identity;
            Facts = facts ?? Array.Empty<SessionOperationalFact>();
            Reason = Normalize(reason);
        }

        public SessionOperationalResultKind Kind { get; }
        public SessionOperationalIdentity Identity { get; }
        public IReadOnlyList<SessionOperationalFact> Facts { get; }
        public string Reason { get; }

        public bool IsValid =>
            Kind != SessionOperationalResultKind.Unknown &&
            Identity.IsValid;

        public bool IsAccepted => Kind == SessionOperationalResultKind.Accepted || Kind == SessionOperationalResultKind.Completed;
        public bool IsRejected => Kind == SessionOperationalResultKind.Rejected;
        public bool IsCompleted => Kind == SessionOperationalResultKind.Completed;

        public override string ToString()
        {
            return $"kind='{Kind}', identity='{Identity}', reason='{Reason}', factsCount='{Facts.Count}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

