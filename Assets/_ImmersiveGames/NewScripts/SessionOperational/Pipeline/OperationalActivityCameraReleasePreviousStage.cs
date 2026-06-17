using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalActivityCameraReleasePreviousResultKind
    {
        Unknown = 0,
        Released = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalActivityCameraReleasePreviousCommand
    {
        public OperationalActivityCameraReleasePreviousCommand(
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

        public bool IsValid =>
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalActivityCameraReleasePreviousResult
    {
        public OperationalActivityCameraReleasePreviousResult(
            OperationalActivityCameraReleasePreviousResultKind kind,
            string currentRouteIdentity,
            string previousRouteIdentity,
            string previousActivityIdentity,
            string reason,
            string detail)
        {
            Kind = kind;
            CurrentRouteIdentity = Normalize(currentRouteIdentity);
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            PreviousActivityIdentity = Normalize(previousActivityIdentity);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalActivityCameraReleasePreviousResultKind Kind { get; }
        public string CurrentRouteIdentity { get; }
        public string PreviousRouteIdentity { get; }
        public string PreviousActivityIdentity { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsReleased => Kind == OperationalActivityCameraReleasePreviousResultKind.Released;
        public bool IsSkipped => Kind == OperationalActivityCameraReleasePreviousResultKind.Skipped;
        public bool IsFailed => Kind == OperationalActivityCameraReleasePreviousResultKind.Failed;
        public bool IsAccepted => IsReleased || IsSkipped;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalActivityCameraReleasePreviousStage
    {
        private readonly OperationalFactRecorder _factRecorder;
        private readonly ISessionOperationalActivityCameraAdapter _activityCameraAdapter;

        public OperationalActivityCameraReleasePreviousStage(OperationalFactRecorder factRecorder, ISessionOperationalActivityCameraAdapter activityCameraAdapter)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
            _activityCameraAdapter = activityCameraAdapter ?? throw new ArgumentNullException(nameof(activityCameraAdapter));
        }

        public OperationalActivityCameraReleasePreviousResult Execute(OperationalActivityCameraReleasePreviousCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalActivityCameraReleasePreviousCommand invalido.");
            }

            var routeCommand = command.RouteCommand;

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.ActivityCameraPresentation, command.Source, command.Reason, "activity_camera_release_previous_started");
            DebugUtility.LogVerbose(typeof(OperationalActivityCameraReleasePreviousStage),
                $"ActivityCameraPresentationReleasePreviousStarted currentRouteIdentity='{routeCommand.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            SessionOperationalActivityCameraReleaseCommand releaseCommand = new(
                routeCommand.RouteIdentity,
                command.PreviousRouteIdentity,
                command.Source,
                command.Reason);

            if (!_activityCameraAdapter.TryReleaseActivityCamera(
                    releaseCommand,
                    out var releaseResult,
                    out string releaseReason))
            {
                string failureReason = string.IsNullOrWhiteSpace(releaseReason)
                    ? releaseResult.Reason
                    : releaseReason;

                DebugUtility.LogError(typeof(OperationalActivityCameraReleasePreviousStage),
                    $"ActivityCameraPresentationReleasePreviousFailed currentRouteIdentity='{routeCommand.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' reason='{Normalize(failureReason)}' source='{command.Source}' reasonDetail='{command.Reason}'.");

                return Failed(command, Normalize(failureReason), "activity_camera_release_previous_failed");
            }

            if (releaseResult.IsSkipped)
            {
                string skipReason = string.IsNullOrWhiteSpace(releaseResult.SkipReason)
                    ? "activity_camera_release_previous_not_required"
                    : releaseResult.SkipReason;

                DebugUtility.LogVerbose(typeof(OperationalActivityCameraReleasePreviousStage),
                    $"ActivityCameraPresentationReleasePreviousSkipped currentRouteIdentity='{routeCommand.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' skipReason='{skipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                return new OperationalActivityCameraReleasePreviousResult(
                    OperationalActivityCameraReleasePreviousResultKind.Skipped,
                    routeCommand.RouteIdentity,
                    command.PreviousRouteIdentity,
                    command.PreviousActivityIdentity,
                    skipReason,
                    "activity_camera_release_previous_skipped");
            }

            string resultReason = string.IsNullOrWhiteSpace(releaseResult.Reason)
                ? "activity_camera_release_previous_completed"
                : releaseResult.Reason;

            DebugUtility.Log(typeof(OperationalActivityCameraReleasePreviousStage),
                $"ActivityCameraPresentationReleasePreviousCompleted currentRouteIdentity='{routeCommand.RouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' resultReason='{resultReason}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalActivityCameraReleasePreviousResult(
                OperationalActivityCameraReleasePreviousResultKind.Released,
                routeCommand.RouteIdentity,
                command.PreviousRouteIdentity,
                command.PreviousActivityIdentity,
                resultReason,
                "activity_camera_release_previous_completed");
        }

        private static OperationalActivityCameraReleasePreviousResult Failed(
            OperationalActivityCameraReleasePreviousCommand command,
            string reason,
            string detail)
        {
            var routeCommand = command.RouteCommand;
            return new OperationalActivityCameraReleasePreviousResult(
                OperationalActivityCameraReleasePreviousResultKind.Failed,
                routeCommand.RouteIdentity,
                command.PreviousRouteIdentity,
                command.PreviousActivityIdentity,
                string.IsNullOrWhiteSpace(reason) ? "activity_camera_release_previous_failed" : reason,
                string.IsNullOrWhiteSpace(detail) ? "activity_camera_release_previous_failed" : detail);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
