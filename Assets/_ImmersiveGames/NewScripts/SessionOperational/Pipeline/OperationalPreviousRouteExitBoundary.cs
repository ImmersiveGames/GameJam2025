using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalPreviousRouteExitBoundaryResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2
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
            PreviousRouteIdentity = previousRouteIdentity.TrimToEmpty();
            PreviousActivityIdentity = previousActivityIdentity.TrimToEmpty();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string PreviousRouteIdentity { get; }
        public string PreviousActivityIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => RouteCommand.IsValid;
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
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
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
    }

    public sealed class OperationalPreviousRouteExitBoundary
    {
        private readonly OperationalFactRecorder _factRecorder;

        public OperationalPreviousRouteExitBoundary(OperationalFactRecorder factRecorder)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
        }

        public OperationalPreviousRouteExitBoundaryResult Begin(OperationalPreviousRouteExitBoundaryCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalPreviousRouteExitBoundaryCommand invalido.");
            }

            var routeCommand = command.RouteCommand;

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.PreviousRouteTeardownSkipped, command.Source, command.Reason, "previous_route_exit_started");
            DebugUtility.LogVerbose(typeof(OperationalPreviousRouteExitBoundary),
                $"OperationalPreviousRouteExitStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            return Completed(command, "started", "previous_route_exit_started");
        }

        public OperationalPreviousRouteExitBoundaryResult Complete(OperationalPreviousRouteExitBoundaryCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalPreviousRouteExitBoundaryCommand invalido.");
            }

            var routeCommand = command.RouteCommand;

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.PreviousRouteTeardownSkipped, command.Source, command.Reason, "previous_route_exit_completed");
            DebugUtility.Log(typeof(OperationalPreviousRouteExitBoundary),
                $"OperationalPreviousRouteExitCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
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
