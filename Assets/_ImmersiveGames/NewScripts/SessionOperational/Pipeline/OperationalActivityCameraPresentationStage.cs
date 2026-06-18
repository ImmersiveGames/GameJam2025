using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalActivityCameraPresentationResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3
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
            ActiveSceneName = activeSceneName.TrimToEmpty();
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence;
            ActivityIdentity = activityIdentity.TrimToEmpty();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
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
            ActivityIdentity = activityIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
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
    }

    public sealed class OperationalActivityCameraPresentationStage
    {
        private readonly OperationalFactRecorder _factRecorder;
        private readonly ISessionOperationalActivityCameraAdapter _activityCameraAdapter;

        public OperationalActivityCameraPresentationStage(OperationalFactRecorder factRecorder, ISessionOperationalActivityCameraAdapter activityCameraAdapter)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
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

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.ActivityCameraPresentation, command.Source, command.Reason, "activity_camera_presentation_started");
            DebugUtility.LogVerbose(typeof(OperationalActivityCameraPresentationStage),
                $"ActivityCameraPresentationStageStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity.TrimToEmpty()}' completionHandoff='{command.RouteCommand.CompletionHandoff}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (command.RouteCommand.CompletionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                string skipReason = "activity_camera_not_required";
                DebugUtility.LogVerbose(typeof(OperationalActivityCameraPresentationStage),
                    $"ActivityCameraPresentationStageSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity.TrimToEmpty()}' completionHandoff='{command.RouteCommand.CompletionHandoff}' skipReason='{skipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                _factRecorder.TryRecordOperationStage(SessionOperationalStage.ActivityCameraPresentation, command.Source, command.Reason, "activity_camera_presentation_skipped");
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
                DebugUtility.LogVerbose(typeof(OperationalActivityCameraPresentationStage),
                    $"ActivityCameraPresentationStageSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity.TrimToEmpty()}' completionHandoff='{command.RouteCommand.CompletionHandoff}' skipReason='{skipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                _factRecorder.TryRecordOperationStage(SessionOperationalStage.ActivityCameraPresentation, command.Source, command.Reason, "activity_camera_presentation_skipped");
                return OperationalActivityCameraPresentationResult.Skipped(
                    command.ActivityIdentity,
                    command.RouteOperationId,
                    skipReason,
                    "activity_camera_prepare_skipped");
            }

            if (activityProfile.CameraRigPrefab == null && !activityProfile.Required)
            {
                string skipReason = "activity_presentation_camera_disabled";
                DebugUtility.LogVerbose(typeof(OperationalActivityCameraPresentationStage),
                    $"ActivityCameraPresentationStageSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity.TrimToEmpty()}' completionHandoff='{command.RouteCommand.CompletionHandoff}' skipReason='{skipReason}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                _factRecorder.TryRecordOperationStage(SessionOperationalStage.ActivityCameraPresentation, command.Source, command.Reason, "activity_camera_presentation_skipped");
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

                _factRecorder.TryRecordOperationStage(SessionOperationalStage.ActivityCameraPresentation, command.Source, command.Reason, "activity_camera_presentation_failed");
                DebugUtility.LogError(typeof(OperationalActivityCameraPresentationStage),
                    $"ActivityCameraPresentationStageFailed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity.TrimToEmpty()}' required='{required}' reason='{failureReason.TrimToEmpty()}' source='{command.Source}' reasonDetail='{command.Reason}'.");

                return OperationalActivityCameraPresentationResult.Failed(
                    command.ActivityIdentity,
                    command.RouteOperationId,
                    failureReason.TrimToEmpty(),
                    required ? "activity_camera_required_prepare_failed" : "activity_camera_prepare_failed");
            }

            if (prepareResult.IsSkipped)
            {
                string skipReason = string.IsNullOrWhiteSpace(prepareResult.SkipReason)
                    ? "activity_camera_prepare_skipped"
                    : prepareResult.SkipReason;

                DebugUtility.LogVerbose(typeof(OperationalActivityCameraPresentationStage),
                    $"ActivityCameraPresentationStageSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity.TrimToEmpty()}' skipReason='{skipReason}' source='{command.Source}' reason='{command.Reason}'.",
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

                _factRecorder.TryRecordOperationStage(SessionOperationalStage.ActivityCameraPresentation, command.Source, command.Reason, "activity_camera_presentation_prepared");
                DebugUtility.Log(typeof(OperationalActivityCameraPresentationStage),
                    $"ActivityCameraPresentationStagePrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity.TrimToEmpty()}' resultReason='{resultReason}' source='{command.Source}' reason='{command.Reason}'.",
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
    }
}
