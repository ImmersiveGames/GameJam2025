using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class SessionActivityOperationalRouteHandoffExitAdapter : IOperationalRouteHandoffExitPort
    {
        private readonly Func<ISessionActivityRouteExitTeardownBoundary> _boundaryResolver;

        public SessionActivityOperationalRouteHandoffExitAdapter(Func<ISessionActivityRouteExitTeardownBoundary> boundaryResolver)
        {
            _boundaryResolver = boundaryResolver ?? throw new ArgumentNullException(nameof(boundaryResolver));
        }

        public OperationalRouteHandoffExitPreflightResult EvaluatePreflight(OperationalRouteHandoffExitPreflightRequest request)
        {
            if (!request.RequiresHandoffExit)
            {
                return new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.NotRequired,
                    "handoff_exit_not_required",
                    "No active handoff identity was present on the previous route.");
            }

            ResolveBoundaryOrFail();

            return new OperationalRouteHandoffExitPreflightResult(
                OperationalRouteHandoffExitPreflightKind.Accepted,
                "accepted",
                string.Empty);
        }

        public async Task<OperationalRouteHandoffExitResult> RequestExitAsync(
            OperationalRouteHandoffExitRequest request,
            CancellationToken cancellationToken)
        {
            if (!request.RequiresHandoffExit)
            {
                return new OperationalRouteHandoffExitResult(
                    OperationalRouteHandoffExitKind.NotRequired,
                    request.HandoffIdentity,
                    "handoff_exit_not_required",
                    "No active handoff identity was present on the previous route.");
            }

            var boundary = ResolveBoundaryOrFail();
            var result = await boundary.AwaitRouteExitTeardownAsync(
                request.HandoffIdentity,
                request.Source,
                request.Reason,
                cancellationToken);

            if (!result.IsValid)
            {
                return new OperationalRouteHandoffExitResult(
                    OperationalRouteHandoffExitKind.Failed,
                    request.HandoffIdentity,
                    "handoff_exit_invalid_result",
                    "consumer_exit_result_invalid");
            }

            if (result.Kind == SessionActivityRouteExitTeardownKind.NotRequired)
            {
                return new OperationalRouteHandoffExitResult(
                    OperationalRouteHandoffExitKind.NotRequired,
                    request.HandoffIdentity,
                    string.IsNullOrWhiteSpace(result.Reason) ? "handoff_exit_not_required" : result.Reason,
                    "consumer_exit_not_required");
            }

            if (result.Kind == SessionActivityRouteExitTeardownKind.Completed)
            {
                return new OperationalRouteHandoffExitResult(
                    OperationalRouteHandoffExitKind.Completed,
                    request.HandoffIdentity,
                    string.IsNullOrWhiteSpace(result.Reason) ? "handoff_exit_completed" : result.Reason,
                    "consumer_exit_completed");
            }

            if (result.Kind == SessionActivityRouteExitTeardownKind.Failed)
            {
                return new OperationalRouteHandoffExitResult(
                    OperationalRouteHandoffExitKind.Failed,
                    request.HandoffIdentity,
                    string.IsNullOrWhiteSpace(result.Reason) ? "handoff_exit_failed" : result.Reason,
                    "consumer_exit_failed");
            }

            return new OperationalRouteHandoffExitResult(
                OperationalRouteHandoffExitKind.RejectedByPolicy,
                request.HandoffIdentity,
                "handoff_exit_unexpected_in_progress_result",
                "consumer_exit_in_progress");
        }

        private ISessionActivityRouteExitTeardownBoundary ResolveBoundaryOrFail()
        {
            var boundary = _boundaryResolver();
            if (boundary == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] SessionActivity route-exit boundary ausente para adapter de handoff exit operacional.");
            }

            return boundary;
        }

    }
}
