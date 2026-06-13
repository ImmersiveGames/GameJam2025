using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteCameraReleasePreviousResultKind
    {
        Unknown = 0,
        Released = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalRouteCameraReleasePreviousCommand
    {
        public OperationalRouteCameraReleasePreviousCommand(
            SessionOperationalRouteCommand routeCommand,
            string previousRouteIdentity,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string PreviousRouteIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalRouteCameraReleasePreviousResult
    {
        public OperationalRouteCameraReleasePreviousResult(
            OperationalRouteCameraReleasePreviousResultKind kind,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string previousRouteIdentity,
            string releaseRouteIdentity,
            string reason,
            string detail)
        {
            Kind = kind;
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            ReleaseRouteIdentity = Normalize(releaseRouteIdentity);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalRouteCameraReleasePreviousResultKind Kind { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string PreviousRouteIdentity { get; }
        public string ReleaseRouteIdentity { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsReleased => Kind == OperationalRouteCameraReleasePreviousResultKind.Released;
        public bool IsSkipped => Kind == OperationalRouteCameraReleasePreviousResultKind.Skipped;
        public bool IsFailed => Kind == OperationalRouteCameraReleasePreviousResultKind.Failed;
        public bool IsAccepted => IsReleased || IsSkipped;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalRouteCameraReleasePreviousStage
    {
        private readonly ISessionOperationalRouteCameraAdapter _routeCameraAdapter;

        public OperationalRouteCameraReleasePreviousStage(ISessionOperationalRouteCameraAdapter routeCameraAdapter)
        {
            _routeCameraAdapter = routeCameraAdapter ?? throw new ArgumentNullException(nameof(routeCameraAdapter));
        }

        public OperationalRouteCameraReleasePreviousResult Execute(OperationalRouteCameraReleasePreviousCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalRouteCameraReleasePreviousCommand invalido.");
            }

            var routeCommand = command.RouteCommand;

            DebugUtility.LogVerbose(typeof(OperationalRouteCameraReleasePreviousStage),
                $"RouteCameraPresentationReleasePreviousStarted currentRouteIdentity='{routeCommand.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            SessionOperationalRouteCameraReleaseCommand releaseCommand = new(
                routeCommand.RouteIdentity,
                command.PreviousRouteIdentity,
                command.Source,
                command.Reason);

            if (!_routeCameraAdapter.TryReleaseRouteCamera(
                    releaseCommand,
                    out var releaseResult,
                    out string releaseReason))
            {
                string failureReason = string.IsNullOrWhiteSpace(releaseReason)
                    ? releaseResult.Reason
                    : releaseReason;

                DebugUtility.LogError(typeof(OperationalRouteCameraReleasePreviousStage),
                    $"RouteCameraPresentationReleasePreviousFailed currentRouteIdentity='{routeCommand.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' reason='{Normalize(failureReason)}' source='{command.Source}' reasonDetail='{command.Reason}'.");

                return Failed(command, failureReason, "route_camera_release_previous_try_release_failed");
            }

            if (releaseResult.IsSkipped)
            {
                DebugUtility.LogVerbose(typeof(OperationalRouteCameraReleasePreviousStage),
                    $"RouteCameraPresentationReleasePreviousSkipped currentRouteIdentity='{routeCommand.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' skipReason='{releaseResult.SkipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                return new OperationalRouteCameraReleasePreviousResult(
                    OperationalRouteCameraReleasePreviousResultKind.Skipped,
                    routeCommand.RouteIdentity,
                    routeCommand.RouteOperationId,
                    routeCommand.TransitionId,
                    routeCommand.RouteSequence,
                    command.PreviousRouteIdentity,
                    string.Empty,
                    releaseResult.SkipReason,
                    "route_camera_release_previous_skipped");
            }

            if (releaseResult.IsReleased)
            {
                string releaseRouteIdentity = releaseResult.ReleasedFact != null
                    ? releaseResult.ReleasedFact.RouteIdentity
                    : string.Empty;
                string releaseRequirementId = releaseResult.ReleasedFact != null
                    ? releaseResult.ReleasedFact.RequirementId
                    : string.Empty;

                DebugUtility.Log(typeof(OperationalRouteCameraReleasePreviousStage),
                    $"RouteCameraPresentationReleasePreviousCompleted currentRouteIdentity='{routeCommand.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' releaseRouteIdentity='{releaseRouteIdentity}' releaseRequirementId='{releaseRequirementId}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);

                return new OperationalRouteCameraReleasePreviousResult(
                    OperationalRouteCameraReleasePreviousResultKind.Released,
                    routeCommand.RouteIdentity,
                    routeCommand.RouteOperationId,
                    routeCommand.TransitionId,
                    routeCommand.RouteSequence,
                    command.PreviousRouteIdentity,
                    releaseRouteIdentity,
                    "released",
                    "route_camera_release_previous_completed");
            }

            string reason = string.IsNullOrWhiteSpace(releaseResult.Reason)
                ? "route_camera_release_previous_failed"
                : releaseResult.Reason;

            DebugUtility.LogError(typeof(OperationalRouteCameraReleasePreviousStage),
                $"RouteCameraPresentationReleasePreviousFailed currentRouteIdentity='{routeCommand.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' reason='{Normalize(reason)}' source='{command.Source}' reasonDetail='{command.Reason}'.");

            return Failed(command, reason, "route_camera_release_previous_failed");
        }

        private static OperationalRouteCameraReleasePreviousResult Failed(
            OperationalRouteCameraReleasePreviousCommand command,
            string reason,
            string detail)
        {
            var routeCommand = command.RouteCommand;
            return new OperationalRouteCameraReleasePreviousResult(
                OperationalRouteCameraReleasePreviousResultKind.Failed,
                routeCommand.RouteIdentity,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                command.PreviousRouteIdentity,
                string.Empty,
                string.IsNullOrWhiteSpace(reason) ? "route_camera_release_previous_failed" : reason,
                string.IsNullOrWhiteSpace(detail) ? "route_camera_release_previous_failed" : detail);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
