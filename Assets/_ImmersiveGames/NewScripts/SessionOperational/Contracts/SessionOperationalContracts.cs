using System;
using System.Collections.Generic;
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

    public readonly struct SessionOperationalRouteKey : IEquatable<SessionOperationalRouteKey>
    {
        public SessionOperationalRouteKey(
            string pipelineId,
            string routeIdentity,
            string routeOperationId,
            string routeId,
            string routeProfileId,
            int routeSequence)
        {
            PipelineId = Normalize(pipelineId);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            RouteId = Normalize(routeId);
            RouteProfileId = Normalize(routeProfileId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
        }

        public string PipelineId { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string RouteId { get; }
        public string RouteProfileId { get; }
        public int RouteSequence { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(RouteId) &&
            !string.IsNullOrWhiteSpace(RouteProfileId) &&
            RouteSequence > 0;

        public bool Equals(SessionOperationalRouteKey other)
        {
            return string.Equals(PipelineId, other.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(RouteIdentity, other.RouteIdentity, StringComparison.Ordinal) &&
                   string.Equals(RouteOperationId, other.RouteOperationId, StringComparison.Ordinal) &&
                   string.Equals(RouteId, other.RouteId, StringComparison.Ordinal) &&
                   string.Equals(RouteProfileId, other.RouteProfileId, StringComparison.Ordinal) &&
                   RouteSequence == other.RouteSequence;
        }

        public override bool Equals(object obj)
        {
            return obj is SessionOperationalRouteKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(PipelineId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RouteIdentity ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RouteOperationId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RouteId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RouteProfileId ?? string.Empty);
                hashCode = (hashCode * 397) ^ RouteSequence;
                return hashCode;
            }
        }

        public static bool operator ==(SessionOperationalRouteKey left, SessionOperationalRouteKey right) => left.Equals(right);
        public static bool operator !=(SessionOperationalRouteKey left, SessionOperationalRouteKey right) => !left.Equals(right);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionOperationalTransitionKey : IEquatable<SessionOperationalTransitionKey>
    {
        public SessionOperationalTransitionKey(SessionOperationalRouteKey routeKey, string transitionId)
        {
            RouteKey = routeKey;
            TransitionId = Normalize(transitionId);
        }

        public SessionOperationalRouteKey RouteKey { get; }
        public string TransitionId { get; }
        public bool IsValid => RouteKey.IsValid && !string.IsNullOrWhiteSpace(TransitionId);

        public bool Equals(SessionOperationalTransitionKey other)
        {
            return RouteKey.Equals(other.RouteKey) &&
                   string.Equals(TransitionId, other.TransitionId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionOperationalTransitionKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (RouteKey.GetHashCode() * 397) ^ StringComparer.Ordinal.GetHashCode(TransitionId ?? string.Empty);
            }
        }

        public static bool operator ==(SessionOperationalTransitionKey left, SessionOperationalTransitionKey right) => left.Equals(right);
        public static bool operator !=(SessionOperationalTransitionKey left, SessionOperationalTransitionKey right) => !left.Equals(right);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionOperationalStageKey : IEquatable<SessionOperationalStageKey>
    {
        public SessionOperationalStageKey(SessionOperationalTransitionKey transitionKey, SessionOperationalStage stage)
        {
            TransitionKey = transitionKey;
            Stage = stage;
        }

        public SessionOperationalTransitionKey TransitionKey { get; }
        public SessionOperationalStage Stage { get; }
        public bool IsValid => TransitionKey.IsValid && Stage != SessionOperationalStage.Unknown;

        public bool Equals(SessionOperationalStageKey other)
        {
            return TransitionKey.Equals(other.TransitionKey) && Stage == other.Stage;
        }

        public override bool Equals(object obj)
        {
            return obj is SessionOperationalStageKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (TransitionKey.GetHashCode() * 397) ^ (int)Stage;
            }
        }

        public static bool operator ==(SessionOperationalStageKey left, SessionOperationalStageKey right) => left.Equals(right);
        public static bool operator !=(SessionOperationalStageKey left, SessionOperationalStageKey right) => !left.Equals(right);
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
            RouteKey = new SessionOperationalRouteKey(
                SessionOperationalPipelineId,
                RouteId,
                RouteOperationId,
                RouteId,
                RouteProfileId,
                TransitionSequence);
            TransitionKey = new SessionOperationalTransitionKey(RouteKey, TransitionId);
            StageKey = new SessionOperationalStageKey(TransitionKey, Stage);
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
        public SessionOperationalRouteKey RouteKey { get; }
        public SessionOperationalTransitionKey TransitionKey { get; }
        public SessionOperationalStageKey StageKey { get; }
        public string CycleSignature { get; }

        public bool IsValid =>
            RouteKey.IsValid &&
            TransitionKey.IsValid &&
            StageKey.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason) &&
            !string.IsNullOrWhiteSpace(CycleSignature);

        public bool Equals(SessionOperationalIdentity other)
        {
            return StageKey.Equals(other.StageKey);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionOperationalIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StageKey.GetHashCode();
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
        PauseOverlay = 3,
        InputLocked = 4,
    }

    public enum SessionOperationalInputPolicy
    {
        Unknown = 0,
        MenuNavigation = 1,
        ActivityGameplay = 2,
        OverlayNavigation = 3,
        InputLocked = 4,
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

