using System;
using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteConsumerReadinessResultKind
    {
        Unknown = 0,
        Ready = 1,
        NotRequired = 2,
        RejectedForeignOrStale = 3,
        Failed = 4,
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
            ConsumerIdentity = Normalize(consumerIdentity);
            ExpectedRouteOperationId = Normalize(expectedRouteOperationId);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
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

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
            ConsumerIdentity = Normalize(consumerIdentity);
            RouteOperationId = Normalize(routeOperationId);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
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

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IOperationalRouteConsumerReadinessPort
    {
        Task<OperationalRouteConsumerReadinessResult> AwaitReadinessAsync(
            OperationalRouteConsumerReadinessRequest request,
            CancellationToken cancellationToken);
    }
}
