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

            var boundary = ResolveBoundaryOrFail();
            var preflight = boundary.EvaluateRouteExitTeardownPreflight(
                request.HandoffIdentity,
                request.Source,
                request.Reason);

            if (!preflight.IsValid)
            {
                return new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.Failed,
                    "handoff_exit_preflight_invalid_result",
                    "session_activity_route_exit_teardown_preflight_invalid_result");
            }

            return preflight.Kind switch
            {
                SessionActivityRouteExitTeardownPreflightKind.NotRequired => new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.NotRequired,
                    string.IsNullOrWhiteSpace(preflight.Reason) ? "handoff_exit_not_required" : preflight.Reason,
                    string.IsNullOrWhiteSpace(preflight.Detail) ? "session_activity_route_exit_teardown_not_required" : preflight.Detail),
                SessionActivityRouteExitTeardownPreflightKind.Accepted => new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.Accepted,
                    string.IsNullOrWhiteSpace(preflight.Reason) ? "accepted" : preflight.Reason,
                    string.IsNullOrWhiteSpace(preflight.Detail) ? "session_activity_route_exit_teardown_preflight_accepted" : preflight.Detail),
                SessionActivityRouteExitTeardownPreflightKind.RejectedByPolicy => new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.RejectedByPolicy,
                    string.IsNullOrWhiteSpace(preflight.Reason) ? "handoff_exit_rejected_by_session_activity_policy" : preflight.Reason,
                    string.IsNullOrWhiteSpace(preflight.Detail) ? "session_activity_route_exit_teardown_preflight_rejected" : preflight.Detail),
                SessionActivityRouteExitTeardownPreflightKind.Failed => new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.Failed,
                    string.IsNullOrWhiteSpace(preflight.Reason) ? "handoff_exit_preflight_failed" : preflight.Reason,
                    string.IsNullOrWhiteSpace(preflight.Detail) ? "session_activity_route_exit_teardown_preflight_failed" : preflight.Detail),
                _ => new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.Failed,
                    "handoff_exit_preflight_unknown_result",
                    $"session_activity_route_exit_teardown_preflight_unknown_kind kind='{preflight.Kind}'")
            };
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
