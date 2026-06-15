using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    internal enum OperationalActivityPoolReleaseResultKind
    {
        Unknown = 0,
        NotRequired = 1,
        Completed = 2,
        Failed = 3,
    }

    internal readonly struct OperationalActivityPoolReleaseCommand
    {
        public OperationalActivityPoolReleaseCommand(
            SessionOperationalRouteCommand routeCommand,
            string previousRouteIdentity,
            string previousActivityIdentity,
            OperationalHandoffExitResult handoffExitResult,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            PreviousActivityIdentity = Normalize(previousActivityIdentity);
            HandoffExitResult = handoffExitResult;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string PreviousRouteIdentity { get; }
        public string PreviousActivityIdentity { get; }
        public OperationalHandoffExitResult HandoffExitResult { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => RouteCommand.IsValid;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    internal readonly struct OperationalActivityPoolReleaseResult
    {
        public OperationalActivityPoolReleaseResult(
            OperationalActivityPoolReleaseResultKind kind,
            PoolLifetimeScope scope,
            int releasedPoolCount,
            int activeObjectCountBeforeRelease,
            int inactiveObjectCountBeforeRelease,
            string reason,
            string detail)
        {
            Kind = kind;
            Scope = scope;
            ReleasedPoolCount = releasedPoolCount < 0 ? 0 : releasedPoolCount;
            ActiveObjectCountBeforeRelease = activeObjectCountBeforeRelease < 0 ? 0 : activeObjectCountBeforeRelease;
            InactiveObjectCountBeforeRelease = inactiveObjectCountBeforeRelease < 0 ? 0 : inactiveObjectCountBeforeRelease;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalActivityPoolReleaseResultKind Kind { get; }
        public PoolLifetimeScope Scope { get; }
        public int ReleasedPoolCount { get; }
        public int ActiveObjectCountBeforeRelease { get; }
        public int InactiveObjectCountBeforeRelease { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsAccepted => Kind == OperationalActivityPoolReleaseResultKind.NotRequired || Kind == OperationalActivityPoolReleaseResultKind.Completed;
        public bool IsCompleted => Kind == OperationalActivityPoolReleaseResultKind.Completed;
        public bool IsFailed => Kind == OperationalActivityPoolReleaseResultKind.Failed;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    internal sealed class OperationalActivityPoolReleaseStage
    {
        private readonly Func<IPoolService> _poolServiceResolver;

        public OperationalActivityPoolReleaseStage(Func<IPoolService> poolServiceResolver)
        {
            _poolServiceResolver = poolServiceResolver ?? throw new ArgumentNullException(nameof(poolServiceResolver));
        }

        public OperationalActivityPoolReleaseResult Execute(OperationalActivityPoolReleaseCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalActivityPoolReleaseCommand invalido.");
            }

            if (!ShouldReleaseActivityPools(command))
            {
                var skipped = new OperationalActivityPoolReleaseResult(
                    OperationalActivityPoolReleaseResultKind.NotRequired,
                    PoolLifetimeScope.Activity,
                    0,
                    0,
                    0,
                    "activity_pool_release_not_required",
                    "route_does_not_leave_session_activity_surface");

                DebugUtility.LogVerbose(typeof(OperationalActivityPoolReleaseStage),
                    $"event='OperationalActivityPoolReleaseSkipped' owner='OperationalActivityPoolReleaseStage' routeIdentity='{command.RouteCommand.RouteIdentity}' routeOperationId='{command.RouteCommand.RouteOperationId}' transitionId='{command.RouteCommand.TransitionId}' routeSequence='{command.RouteCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' destinationSurfaceKind='{command.RouteCommand.SurfaceKind}' completionHandoff='{command.RouteCommand.CompletionHandoff}' reason='{skipped.Reason}' detail='{skipped.Detail}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                return skipped;
            }

            var poolService = _poolServiceResolver();
            if (poolService == null)
            {
                return Failed(command, "pool_service_missing", "IPoolService obrigatorio ausente para liberar pools Activity.");
            }

            DebugUtility.LogVerbose(typeof(OperationalActivityPoolReleaseStage),
                $"event='OperationalActivityPoolReleaseStarted' owner='OperationalActivityPoolReleaseStage' routeIdentity='{command.RouteCommand.RouteIdentity}' routeOperationId='{command.RouteCommand.RouteOperationId}' transitionId='{command.RouteCommand.TransitionId}' routeSequence='{command.RouteCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' destinationSurfaceKind='{command.RouteCommand.SurfaceKind}' completionHandoff='{command.RouteCommand.CompletionHandoff}' scope='{PoolLifetimeScope.Activity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            PoolScopeReleaseResult releaseResult;
            try
            {
                releaseResult = poolService.ReleasePoolsForScope(PoolLifetimeScope.Activity);
            }
            catch (Exception exception)
            {
                return Failed(command, "activity_pool_release_exception", $"{exception.GetType().Name}:{exception.Message}");
            }

            var result = new OperationalActivityPoolReleaseResult(
                OperationalActivityPoolReleaseResultKind.Completed,
                releaseResult.Scope,
                releaseResult.ReleasedPoolCount,
                releaseResult.ActiveObjectCountBeforeRelease,
                releaseResult.InactiveObjectCountBeforeRelease,
                releaseResult.Reason,
                releaseResult.ReleasedAny ? "activity_pool_scope_released" : "activity_pool_scope_release_noop");

            DebugUtility.Log(typeof(OperationalActivityPoolReleaseStage),
                $"event='OperationalActivityPoolReleaseCompleted' owner='OperationalActivityPoolReleaseStage' routeIdentity='{command.RouteCommand.RouteIdentity}' routeOperationId='{command.RouteCommand.RouteOperationId}' transitionId='{command.RouteCommand.TransitionId}' routeSequence='{command.RouteCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' destinationSurfaceKind='{command.RouteCommand.SurfaceKind}' completionHandoff='{command.RouteCommand.CompletionHandoff}' scope='{result.Scope}' releasedPoolCount='{result.ReleasedPoolCount}' activeObjectCountBeforeRelease='{result.ActiveObjectCountBeforeRelease}' inactiveObjectCountBeforeRelease='{result.InactiveObjectCountBeforeRelease}' reason='{result.Reason}' detail='{result.Detail}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return result;
        }

        private static bool ShouldReleaseActivityPools(OperationalActivityPoolReleaseCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.PreviousActivityIdentity))
            {
                return false;
            }

            if (!command.HandoffExitResult.IsCompleted)
            {
                return false;
            }

            if (command.RouteCommand.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                return false;
            }

            return command.RouteCommand.SurfaceKind != OperationalSurfaceKind.SessionActivity;
        }

        private static OperationalActivityPoolReleaseResult Failed(
            OperationalActivityPoolReleaseCommand command,
            string reason,
            string detail)
        {
            string normalizedReason = Normalize(reason);
            string normalizedDetail = Normalize(detail);

            DebugUtility.LogWarning(typeof(OperationalActivityPoolReleaseStage),
                $"event='OperationalActivityPoolReleaseFailed' owner='OperationalActivityPoolReleaseStage' routeIdentity='{command.RouteCommand.RouteIdentity}' routeOperationId='{command.RouteCommand.RouteOperationId}' transitionId='{command.RouteCommand.TransitionId}' routeSequence='{command.RouteCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' destinationSurfaceKind='{command.RouteCommand.SurfaceKind}' completionHandoff='{command.RouteCommand.CompletionHandoff}' reason='{normalizedReason}' detail='{normalizedDetail}' source='{command.Source}' reasonDetail='{command.Reason}'.");

            return new OperationalActivityPoolReleaseResult(
                OperationalActivityPoolReleaseResultKind.Failed,
                PoolLifetimeScope.Activity,
                0,
                0,
                0,
                normalizedReason,
                normalizedDetail);
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
