using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalPreviousRouteExitBoundaryResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalPreviousRouteExitBoundaryCommand
    {
        public OperationalPreviousRouteExitBoundaryCommand(
            SessionOperationalRouteCommand routeCommand,
            string previousRouteIdentity,
            string previousActivityIdentity,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            PreviousActivityIdentity = Normalize(previousActivityIdentity);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string PreviousRouteIdentity { get; }
        public string PreviousActivityIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => RouteCommand.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalPreviousRouteExitBoundaryResult
    {
        public OperationalPreviousRouteExitBoundaryResult(
            OperationalPreviousRouteExitBoundaryResultKind kind,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string reason,
            string detail)
        {
            Kind = kind;
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalPreviousRouteExitBoundaryResultKind Kind { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalPreviousRouteExitBoundaryResultKind.Completed;
        public bool IsFailed => Kind == OperationalPreviousRouteExitBoundaryResultKind.Failed;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalPreviousRouteExitBoundary
    {
        public OperationalPreviousRouteExitBoundaryResult Begin(OperationalPreviousRouteExitBoundaryCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalPreviousRouteExitBoundaryCommand invalido.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            DebugUtility.Log(typeof(OperationalPreviousRouteExitBoundary),
                $"[OBS][SessionOperationalPipeline][PreviousRouteExit] OperationalPreviousRouteExitStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            return Completed(command, "started", "previous_route_exit_started");
        }

        public OperationalPreviousRouteExitBoundaryResult Complete(OperationalPreviousRouteExitBoundaryCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalPreviousRouteExitBoundaryCommand invalido.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            DebugUtility.Log(typeof(OperationalPreviousRouteExitBoundary),
                $"[OBS][SessionOperationalPipeline][PreviousRouteExit] OperationalPreviousRouteExitCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return Completed(command, "completed", "previous_route_exit_completed");
        }

        private static OperationalPreviousRouteExitBoundaryResult Completed(
            OperationalPreviousRouteExitBoundaryCommand command,
            string reason,
            string detail)
        {
            return new OperationalPreviousRouteExitBoundaryResult(
                OperationalPreviousRouteExitBoundaryResultKind.Completed,
                command.RouteCommand.RouteIdentity,
                command.RouteCommand.RouteOperationId,
                command.RouteCommand.TransitionId,
                command.RouteCommand.RouteSequence,
                reason,
                detail);
        }

        private static OperationalPreviousRouteExitBoundaryResult Failed(
            OperationalPreviousRouteExitBoundaryCommand command,
            string reason,
            string detail)
        {
            return new OperationalPreviousRouteExitBoundaryResult(
                OperationalPreviousRouteExitBoundaryResultKind.Failed,
                command.RouteCommand.RouteIdentity,
                command.RouteCommand.RouteOperationId,
                command.RouteCommand.TransitionId,
                command.RouteCommand.RouteSequence,
                reason,
                detail);
        }
    }
}
