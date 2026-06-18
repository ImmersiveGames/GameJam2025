using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum SessionActivityVisualReadinessResultKind
    {
        Unknown = 0,
        Ready = 1,
        NotRequired = 2,
        RejectedForeignOrStale = 3,
        Failed = 4,
    }

    public readonly struct SessionActivityVisualReadinessRequest
    {
        public SessionActivityVisualReadinessRequest(
            string sessionStateId,
            string expectedRouteOperationId,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            SessionStateId = sessionStateId.TrimToEmpty();
            ExpectedRouteOperationId = expectedRouteOperationId.TrimToEmpty();
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string SessionStateId { get; }
        public string ExpectedRouteOperationId { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ExpectedRouteOperationId) &&
            !string.IsNullOrWhiteSpace(RouteOperationId);

        public override string ToString()
        {
            return $"sessionStateId='{SessionStateId}', expectedRouteOperationId='{ExpectedRouteOperationId}', routeIdentity='{RouteIdentity}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', source='{Source}', reason='{Reason}'";
        }
}

    public readonly struct SessionActivityVisualReadinessResult
    {
        public SessionActivityVisualReadinessResult(
            SessionActivityVisualReadinessResultKind kind,
            string sessionStateId,
            string routeOperationId,
            string activityId,
            int entrySequence,
            string reason,
            string detail)
        {
            Kind = kind;
            SessionStateId = sessionStateId.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            ActivityId = activityId.TrimToEmpty();
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public SessionActivityVisualReadinessResultKind Kind { get; }
        public string SessionStateId { get; }
        public string RouteOperationId { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsValid =>
            Kind != SessionActivityVisualReadinessResultKind.Unknown &&
            !string.IsNullOrWhiteSpace(Reason);

        public bool IsReady => Kind == SessionActivityVisualReadinessResultKind.Ready;
        public bool IsNotRequired => Kind == SessionActivityVisualReadinessResultKind.NotRequired;
        public bool IsRejectedForeignOrStale => Kind == SessionActivityVisualReadinessResultKind.RejectedForeignOrStale;
        public bool IsFailed => Kind == SessionActivityVisualReadinessResultKind.Failed;

        public override string ToString()
        {
            return $"kind='{Kind}', sessionStateId='{SessionStateId}', routeOperationId='{RouteOperationId}', activityId='{ActivityId}', entrySequence='{EntrySequence}', reason='{Reason}', detail='{Detail}'";
        }
}

    public interface ISessionActivityVisualReadinessBoundary
    {
        Task<SessionActivityVisualReadinessResult> AwaitVisualReadinessAsync(
            SessionActivityVisualReadinessRequest request,
            CancellationToken cancellationToken);
    }
}
