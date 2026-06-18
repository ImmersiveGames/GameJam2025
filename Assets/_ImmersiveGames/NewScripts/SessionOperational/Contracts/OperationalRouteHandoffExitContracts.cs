using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum OperationalRouteHandoffExitPreflightKind
    {
        Unknown = 0,
        Accepted = 1,
        NotRequired = 2,
        RejectedByPolicy = 3,
        Failed = 4,
    }

    public readonly struct OperationalRouteHandoffExitPreflightRequest
    {
        public OperationalRouteHandoffExitPreflightRequest(
            string routeIdentity,
            string previousRouteIdentity,
            string handoffIdentity,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity.TrimToEmpty();
            PreviousRouteIdentity = previousRouteIdentity.TrimToEmpty();
            HandoffIdentity = handoffIdentity.TrimToEmpty();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string RouteIdentity { get; }
        public string PreviousRouteIdentity { get; }
        public string HandoffIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool RequiresHandoffExit => !string.IsNullOrWhiteSpace(HandoffIdentity);
}

    public readonly struct OperationalRouteHandoffExitRequest
    {
        public OperationalRouteHandoffExitRequest(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string previousRouteIdentity,
            string handoffIdentity,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence;
            PreviousRouteIdentity = previousRouteIdentity.TrimToEmpty();
            HandoffIdentity = handoffIdentity.TrimToEmpty();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string PreviousRouteIdentity { get; }
        public string HandoffIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool RequiresHandoffExit => !string.IsNullOrWhiteSpace(HandoffIdentity);
}

    public readonly struct OperationalRouteHandoffExitPreflightResult
    {
        public OperationalRouteHandoffExitPreflightResult(
            OperationalRouteHandoffExitPreflightKind kind,
            string reason,
            string detail)
        {
            Kind = kind;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalRouteHandoffExitPreflightKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsAccepted => Kind == OperationalRouteHandoffExitPreflightKind.Accepted || Kind == OperationalRouteHandoffExitPreflightKind.NotRequired;
        public bool IsRejected => Kind == OperationalRouteHandoffExitPreflightKind.RejectedByPolicy;
        public bool IsFailed => Kind == OperationalRouteHandoffExitPreflightKind.Failed;
        public bool IsValid => Kind != OperationalRouteHandoffExitPreflightKind.Unknown && !string.IsNullOrWhiteSpace(Reason);

        public override string ToString()
        {
            return $"kind='{Kind}', reason='{Reason}', detail='{Detail}'";
        }
}

    public enum OperationalRouteHandoffExitKind
    {
        Unknown = 0,
        NotRequired = 1,
        Completed = 2,
        RejectedByPolicy = 3,
        Failed = 4,
    }

    public readonly struct OperationalRouteHandoffExitResult
    {
        public OperationalRouteHandoffExitResult(
            OperationalRouteHandoffExitKind kind,
            string handoffIdentity,
            string reason,
            string detail)
        {
            Kind = kind;
            HandoffIdentity = handoffIdentity.TrimToEmpty();
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalRouteHandoffExitKind Kind { get; }
        public string HandoffIdentity { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsValid => Kind != OperationalRouteHandoffExitKind.Unknown && !string.IsNullOrWhiteSpace(Reason);
        public bool IsCompleted => Kind == OperationalRouteHandoffExitKind.Completed || Kind == OperationalRouteHandoffExitKind.NotRequired;
        public bool IsRejected => Kind == OperationalRouteHandoffExitKind.RejectedByPolicy;
        public bool IsFailed => Kind == OperationalRouteHandoffExitKind.Failed;

        public override string ToString()
        {
            return $"kind='{Kind}', handoffIdentity='{HandoffIdentity}', reason='{Reason}', detail='{Detail}'";
        }
}

    public interface IOperationalRouteHandoffExitPort
    {
        OperationalRouteHandoffExitPreflightResult EvaluatePreflight(OperationalRouteHandoffExitPreflightRequest request);

        Task<OperationalRouteHandoffExitResult> RequestExitAsync(
            OperationalRouteHandoffExitRequest request,
            CancellationToken cancellationToken);
    }
}
