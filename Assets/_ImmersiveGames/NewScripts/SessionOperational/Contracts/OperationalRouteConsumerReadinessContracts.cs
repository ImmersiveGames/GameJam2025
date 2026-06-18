using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum OperationalRouteConsumerReadinessResultKind
    {
        Unknown = 0,
        Ready = 1,
        NotRequired = 2,
        RejectedForeignOrStale = 3,
        Failed = 4
    }

    public readonly struct OperationalRouteConsumerReadinessRequest
    {
        public OperationalRouteConsumerReadinessRequest(
            string consumerIdentity,
            string expectedRouteOperationId,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            ConsumerIdentity = consumerIdentity.TrimToEmpty();
            ExpectedRouteOperationId = expectedRouteOperationId.TrimToEmpty();
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string ConsumerIdentity { get; }
        public string ExpectedRouteOperationId { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ConsumerIdentity) &&
            !string.IsNullOrWhiteSpace(ExpectedRouteOperationId) &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct OperationalRouteConsumerReadinessResult
    {
        public OperationalRouteConsumerReadinessResult(
            OperationalRouteConsumerReadinessResultKind kind,
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            Kind = kind;
            ConsumerIdentity = consumerIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalRouteConsumerReadinessResultKind Kind { get; }
        public string ConsumerIdentity { get; }
        public string RouteOperationId { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsValid => Kind != OperationalRouteConsumerReadinessResultKind.Unknown && !string.IsNullOrWhiteSpace(Reason);
        public bool IsReady => Kind == OperationalRouteConsumerReadinessResultKind.Ready;
        public bool IsNotRequired => Kind == OperationalRouteConsumerReadinessResultKind.NotRequired;
        public bool IsRejectedForeignOrStale => Kind == OperationalRouteConsumerReadinessResultKind.RejectedForeignOrStale;
        public bool IsFailed => Kind == OperationalRouteConsumerReadinessResultKind.Failed;

        public override string ToString()
        {
            return $"kind='{Kind}', consumerIdentity='{ConsumerIdentity}', routeOperationId='{RouteOperationId}', reason='{Reason}', detail='{Detail}'";
        }
    }

    public interface IOperationalRouteConsumerReadinessPort
    {
        Task<OperationalRouteConsumerReadinessResult> AwaitReadinessAsync(
            OperationalRouteConsumerReadinessRequest request,
            CancellationToken cancellationToken);
    }
}
