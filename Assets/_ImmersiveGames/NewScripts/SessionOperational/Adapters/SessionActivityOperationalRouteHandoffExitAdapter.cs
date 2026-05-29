using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;

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

            ISessionActivityRouteExitTeardownBoundary boundary = ResolveBoundaryOrFail();
            if (boundary.HasPendingOperation)
            {
                LogPreflightDetail("handoff_exit_pending_operation_active", boundary);
                return Rejected(
                    "handoff_exit_pending_operation_active",
                    "pending_operation_active");
            }

            if (IsActivationWindowStage(boundary.CurrentStage))
            {
                LogPreflightDetail("handoff_exit_activation_not_completed", boundary);
                return Rejected(
                    "handoff_exit_activation_not_completed",
                    "activation_window_not_completed");
            }

            if (IsDeactivationWindowStage(boundary.CurrentStage) && boundary.CurrentRailKind != SessionActivityRailKind.ActivityRouteExitRail)
            {
                LogPreflightDetail("handoff_exit_target_transition_in_progress", boundary);
                return Rejected(
                    "handoff_exit_target_transition_in_progress",
                    "target_transition_in_progress");
            }

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

            ISessionActivityRouteExitTeardownBoundary boundary = ResolveBoundaryOrFail();
            SessionActivityRouteExitTeardownResult result = await boundary.AwaitRouteExitTeardownAsync(
                request.HandoffIdentity,
                request.Source,
                request.Reason,
                cancellationToken);

            if (!result.IsValid)
            {
                LogExitDetail("handoff_exit_invalid_result", result);
                return new OperationalRouteHandoffExitResult(
                    OperationalRouteHandoffExitKind.Failed,
                    request.HandoffIdentity,
                    "handoff_exit_invalid_result",
                    "consumer_exit_result_invalid");
            }

            if (result.Kind == SessionActivityRouteExitTeardownKind.NotRequired)
            {
                LogExitDetail("handoff_exit_not_required", result);
                return new OperationalRouteHandoffExitResult(
                    OperationalRouteHandoffExitKind.NotRequired,
                    request.HandoffIdentity,
                    string.IsNullOrWhiteSpace(result.Reason) ? "handoff_exit_not_required" : result.Reason,
                    "consumer_exit_not_required");
            }

            if (result.Kind == SessionActivityRouteExitTeardownKind.Completed)
            {
                LogExitDetail("handoff_exit_completed", result);
                return new OperationalRouteHandoffExitResult(
                    OperationalRouteHandoffExitKind.Completed,
                    request.HandoffIdentity,
                    string.IsNullOrWhiteSpace(result.Reason) ? "handoff_exit_completed" : result.Reason,
                    "consumer_exit_completed");
            }

            if (result.Kind == SessionActivityRouteExitTeardownKind.Failed)
            {
                LogExitDetail("handoff_exit_failed", result);
                return new OperationalRouteHandoffExitResult(
                    OperationalRouteHandoffExitKind.Failed,
                    request.HandoffIdentity,
                    string.IsNullOrWhiteSpace(result.Reason) ? "handoff_exit_failed" : result.Reason,
                    "consumer_exit_failed");
            }

            LogExitDetail("handoff_exit_unexpected_in_progress_result", result);
            return new OperationalRouteHandoffExitResult(
                OperationalRouteHandoffExitKind.RejectedByPolicy,
                request.HandoffIdentity,
                "handoff_exit_unexpected_in_progress_result",
                "consumer_exit_in_progress");
        }

        private ISessionActivityRouteExitTeardownBoundary ResolveBoundaryOrFail()
        {
            ISessionActivityRouteExitTeardownBoundary boundary = _boundaryResolver();
            if (boundary == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] SessionActivity route-exit boundary ausente para adapter de handoff exit operacional.");
            }

            return boundary;
        }

        private static OperationalRouteHandoffExitPreflightResult Rejected(string reason, string detail)
        {
            return new OperationalRouteHandoffExitPreflightResult(
                OperationalRouteHandoffExitPreflightKind.RejectedByPolicy,
                reason,
                detail);
        }

        private static void LogPreflightDetail(string reason, ISessionActivityRouteExitTeardownBoundary boundary)
        {
            DebugUtility.Log(typeof(SessionActivityOperationalRouteHandoffExitAdapter),
                $"[OBS][SessionOperationalPipeline][OperationalHandoffExitAdapter] PreflightRejected reason='{reason}' stage='{boundary.CurrentStage}' railKind='{boundary.CurrentRailKind}' pendingOperation='{boundary.HasPendingOperation}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogExitDetail(string reason, SessionActivityRouteExitTeardownResult result)
        {
            DebugUtility.Log(typeof(SessionActivityOperationalRouteHandoffExitAdapter),
                $"[OBS][SessionOperationalPipeline][OperationalHandoffExitAdapter] ExitResult reason='{reason}' result='{result}'.",
                DebugUtility.Colors.Info);
        }

        private static bool IsActivationWindowStage(SessionActivityStage stage)
        {
            return stage == SessionActivityStage.ActivationWindowStarted ||
                   stage == SessionActivityStage.ActivationWindowSceneLoading ||
                   stage == SessionActivityStage.ActivationWindowAdditiveSceneLoadStarted ||
                   stage == SessionActivityStage.ActivationWindowAdditiveSceneLoaded ||
                   stage == SessionActivityStage.ActivationWindowReady;
        }

        private static bool IsDeactivationWindowStage(SessionActivityStage stage)
        {
            return stage == SessionActivityStage.DeactivationWindowStarted ||
                   stage == SessionActivityStage.DeactivationWindowSceneLoading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoaded ||
                   stage == SessionActivityStage.DeactivationWindowReady ||
                   stage == SessionActivityStage.DeactivationWindowCompleted ||
                   stage == SessionActivityStage.DeactivationWindowSceneUnloading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded ||
                   stage == SessionActivityStage.DeactivationWindowSkippedNoContent;
        }
    }
}
