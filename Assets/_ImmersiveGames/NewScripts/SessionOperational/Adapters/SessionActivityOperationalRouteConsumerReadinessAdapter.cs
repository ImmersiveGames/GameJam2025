using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class SessionActivityOperationalRouteConsumerReadinessAdapter : IOperationalRouteConsumerReadinessPort
    {
        private readonly Func<ISessionActivityVisualReadinessBoundary> _boundaryResolver;

        public SessionActivityOperationalRouteConsumerReadinessAdapter(Func<ISessionActivityVisualReadinessBoundary> boundaryResolver)
        {
            _boundaryResolver = boundaryResolver ?? throw new ArgumentNullException(nameof(boundaryResolver));
        }

        public async Task<OperationalRouteConsumerReadinessResult> AwaitReadinessAsync(
            OperationalRouteConsumerReadinessRequest request,
            CancellationToken cancellationToken)
        {
            if (!request.IsValid)
            {
                return new OperationalRouteConsumerReadinessResult(
                    OperationalRouteConsumerReadinessResultKind.Failed,
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    "operational_route_consumer_readiness_request_invalid",
                    "OperationalRouteConsumerReadinessRequest is invalid.");
            }

            var boundary = ResolveBoundaryOrFail();
            SessionActivityVisualReadinessRequest activityRequest = new(
                request.ConsumerIdentity,
                request.ExpectedRouteOperationId,
                request.RouteIdentity,
                request.RouteOperationId,
                request.TransitionId,
                request.RouteSequence,
                request.Source,
                request.Reason);

            var activityResult = await boundary.AwaitVisualReadinessAsync(
                activityRequest,
                cancellationToken);

            if (!activityResult.IsValid)
            {
                return new OperationalRouteConsumerReadinessResult(
                    OperationalRouteConsumerReadinessResultKind.Failed,
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    "session_activity_visual_readiness_result_invalid",
                    "Concrete consumer readiness result is invalid.");
            }

            if (activityResult.IsReady)
            {
                return new OperationalRouteConsumerReadinessResult(
                    OperationalRouteConsumerReadinessResultKind.Ready,
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    "consumer_readiness_ready",
                    "Concrete route consumer reported visual readiness.");
            }

            if (activityResult.IsNotRequired)
            {
                return new OperationalRouteConsumerReadinessResult(
                    OperationalRouteConsumerReadinessResultKind.NotRequired,
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    "consumer_readiness_not_required",
                    "Concrete route consumer reported visual readiness not required.");
            }

            if (activityResult.IsRejectedForeignOrStale)
            {
                return new OperationalRouteConsumerReadinessResult(
                    OperationalRouteConsumerReadinessResultKind.RejectedForeignOrStale,
                    request.ConsumerIdentity,
                    request.RouteOperationId,
                    "consumer_readiness_rejected_foreign_or_stale",
                    "Concrete route consumer rejected readiness request as foreign or stale.");
            }

            return new OperationalRouteConsumerReadinessResult(
                OperationalRouteConsumerReadinessResultKind.Failed,
                request.ConsumerIdentity,
                request.RouteOperationId,
                "consumer_readiness_failed",
                "Concrete route consumer failed visual readiness.");
        }

        private ISessionActivityVisualReadinessBoundary ResolveBoundaryOrFail()
        {
            var boundary = _boundaryResolver();
            if (boundary == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] ISessionActivityVisualReadinessBoundary obrigatorio ausente para consumer readiness adapter operacional.");
            }

            return boundary;
        }
    }
}
