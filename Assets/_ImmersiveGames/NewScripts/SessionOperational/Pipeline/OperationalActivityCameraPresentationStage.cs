using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalActivityCameraPresentationResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalActivityCameraPresentationCommand
    {
        public OperationalActivityCameraPresentationCommand(
            SessionOperationalRouteCommand routeCommand,
            string activeSceneName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            ActiveSceneName = Normalize(activeSceneName);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            ActivityIdentity = Normalize(activityIdentity);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string ActiveSceneName { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalActivityCameraPresentationResult
    {
        public OperationalActivityCameraPresentationResult(
            OperationalActivityCameraPresentationResultKind kind,
            string activityIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            Kind = kind;
            ActivityIdentity = Normalize(activityIdentity);
            RouteOperationId = Normalize(routeOperationId);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalActivityCameraPresentationResultKind Kind { get; }
        public string ActivityIdentity { get; }
        public string RouteOperationId { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalActivityCameraPresentationResultKind.Completed;
        public bool IsSkipped => Kind == OperationalActivityCameraPresentationResultKind.Skipped;
        public bool IsFailed => Kind == OperationalActivityCameraPresentationResultKind.Failed;
        public bool IsAccepted => IsCompleted || IsSkipped;

        public static OperationalActivityCameraPresentationResult Completed(
            string activityIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalActivityCameraPresentationResult(
                OperationalActivityCameraPresentationResultKind.Completed,
                activityIdentity,
                routeOperationId,
                reason,
                detail);
        }

        public static OperationalActivityCameraPresentationResult Skipped(
            string activityIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalActivityCameraPresentationResult(
                OperationalActivityCameraPresentationResultKind.Skipped,
                activityIdentity,
                routeOperationId,
                reason,
                detail);
        }

        public static OperationalActivityCameraPresentationResult Failed(
            string activityIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalActivityCameraPresentationResult(
                OperationalActivityCameraPresentationResultKind.Failed,
                activityIdentity,
                routeOperationId,
                reason,
                detail);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalActivityCameraPresentationStage
    {
        private readonly ISessionOperationalActivityCameraAdapter _activityCameraAdapter;

        public OperationalActivityCameraPresentationStage(ISessionOperationalActivityCameraAdapter activityCameraAdapter)
        {
            _activityCameraAdapter = activityCameraAdapter ?? throw new ArgumentNullException(nameof(activityCameraAdapter));
        }

        public OperationalActivityCameraPresentationResult Execute(OperationalActivityCameraPresentationCommand command)
        {
            if (!command.IsValid)
            {
                return OperationalActivityCameraPresentationResult.Failed(
                    command.ActivityIdentity,
                    command.RouteOperationId,
                    "invalid_command",
                    "OperationalActivityCameraPresentationCommand invalido.");
            }

            DebugUtility.Log(typeof(OperationalActivityCameraPresentationStage),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationStageStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{Normalize(command.ActivityIdentity)}' completionHandoff='{command.RouteCommand.CompletionHandoff}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (command.RouteCommand.CompletionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                string skipReason = "activity_camera_not_required";
                DebugUtility.Log(typeof(OperationalActivityCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationStageSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{Normalize(command.ActivityIdentity)}' completionHandoff='{command.RouteCommand.CompletionHandoff}' skipReason='{skipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                return OperationalActivityCameraPresentationResult.Skipped(
                    command.ActivityIdentity,
                    command.RouteOperationId,
                    skipReason,
                    "activity_camera_presentation_skipped");
            }

            var activityProfile = command.RouteCommand.ActivityPresentationProfile;
            if (activityProfile == null)
            {
                string skipReason = "activity_presentation_profile_missing";
                DebugUtility.Log(typeof(OperationalActivityCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationStageSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{Normalize(command.ActivityIdentity)}' completionHandoff='{command.RouteCommand.CompletionHandoff}' skipReason='{skipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                return OperationalActivityCameraPresentationResult.Skipped(
                    command.ActivityIdentity,
                    command.RouteOperationId,
                    skipReason,
                    "activity_camera_prepare_skipped");
            }

            if (activityProfile.CameraRigPrefab == null && !activityProfile.Required)
            {
                string skipReason = "activity_presentation_camera_disabled";
                DebugUtility.Log(typeof(OperationalActivityCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationStageSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{Normalize(command.ActivityIdentity)}' completionHandoff='{command.RouteCommand.CompletionHandoff}' skipReason='{skipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                return OperationalActivityCameraPresentationResult.Skipped(
                    command.ActivityIdentity,
                    command.RouteOperationId,
                    skipReason,
                    "activity_camera_prepare_skipped");
            }

            SessionOperationalActivityCameraPrepareCommand prepareCommand = new(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.ActivityIdentity,
                command.RouteCommand.CompletionHandoff,
                command.ActiveSceneName,
                activityProfile,
                command.Source,
                command.Reason);

            if (!_activityCameraAdapter.TryPrepareActivityCamera(
                    prepareCommand,
                    out var prepareResult,
                    out string prepareReason))
            {
                string failureReason = string.IsNullOrWhiteSpace(prepareReason)
                    ? prepareResult.Reason
                    : prepareReason;
                bool required = command.RouteCommand.ActivityPresentationProfile != null &&
                    command.RouteCommand.ActivityPresentationProfile.Required;

                DebugUtility.LogError(typeof(OperationalActivityCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationStageFailed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{Normalize(command.ActivityIdentity)}' required='{required}' reason='{Normalize(failureReason)}' source='{command.Source}' reasonDetail='{command.Reason}'.");

                return OperationalActivityCameraPresentationResult.Failed(
                    command.ActivityIdentity,
                    command.RouteOperationId,
                    Normalize(failureReason),
                    required ? "activity_camera_required_prepare_failed" : "activity_camera_prepare_failed");
            }

            if (prepareResult.IsSkipped)
            {
                string skipReason = string.IsNullOrWhiteSpace(prepareResult.SkipReason)
                    ? "activity_camera_prepare_skipped"
                    : prepareResult.SkipReason;

                DebugUtility.Log(typeof(OperationalActivityCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationStageSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{Normalize(command.ActivityIdentity)}' skipReason='{skipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                return OperationalActivityCameraPresentationResult.Skipped(
                    command.ActivityIdentity,
                    command.RouteOperationId,
                    skipReason,
                    "activity_camera_prepare_skipped");
            }

            if (prepareResult.IsPrepared)
            {
                string resultReason = string.IsNullOrWhiteSpace(prepareResult.Reason)
                    ? "activity_camera_prepare_completed"
                    : prepareResult.Reason;

                DebugUtility.Log(typeof(OperationalActivityCameraPresentationStage),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationStagePrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{Normalize(command.ActivityIdentity)}' resultReason='{resultReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);

                return OperationalActivityCameraPresentationResult.Completed(
                    command.ActivityIdentity,
                    command.RouteOperationId,
                    resultReason,
                    "activity_camera_prepare_completed");
            }

            return OperationalActivityCameraPresentationResult.Failed(
                command.ActivityIdentity,
                command.RouteOperationId,
                "activity_camera_prepare_failed",
                "activity_camera_prepare_failed");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
