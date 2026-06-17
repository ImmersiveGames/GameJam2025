using System;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteCameraPresentationResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalRouteCameraPresentationResult
    {
        public OperationalRouteCameraPresentationResult(
            OperationalRouteCameraPresentationResultKind kind,
            string reason)
        {
            Kind = kind;
            Reason = Normalize(reason);
        }

        public OperationalRouteCameraPresentationResultKind Kind { get; }
        public string Reason { get; }
        public bool IsCompleted => Kind == OperationalRouteCameraPresentationResultKind.Completed;

        public static OperationalRouteCameraPresentationResult Completed(string reason)
        {
            return new OperationalRouteCameraPresentationResult(
                OperationalRouteCameraPresentationResultKind.Completed,
                reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalRouteCameraPresentationCommand
    {
        public OperationalRouteCameraPresentationCommand(
            SessionOperationalRouteCommand routeCommand,
            string activeSceneName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            ActiveSceneName = Normalize(activeSceneName);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string ActiveSceneName { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
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

    public sealed class OperationalRouteCameraPresentationStage
    {
        private readonly OperationalFactRecorder _factRecorder;
        private readonly ISessionOperationalRouteCameraAdapter _routeCameraAdapter;

        public OperationalRouteCameraPresentationStage(OperationalFactRecorder factRecorder, ISessionOperationalRouteCameraAdapter routeCameraAdapter)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
            _routeCameraAdapter = routeCameraAdapter ?? throw new ArgumentNullException(nameof(routeCameraAdapter));
        }

        public OperationalRouteCameraPresentationResult Execute(OperationalRouteCameraPresentationCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalRouteCameraPresentationCommand is invalid.");
            }

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.RouteCameraPresentation, command.Source, command.Reason, "route_camera_presentation_started");
            LogStageStarted(command);
            PrepareRouteCameraOrFail(command);

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.RouteCameraPresentation, command.Source, command.Reason, "route_camera_presentation_completed");
            return OperationalRouteCameraPresentationResult.Completed("completed");
        }

        private void PrepareRouteCameraOrFail(OperationalRouteCameraPresentationCommand command)
        {
            var routeCommand = command.RouteCommand;
            if (ShouldSkipRouteCameraByPolicy(command, out string skipReason))
            {
                DebugUtility.LogVerbose(typeof(OperationalRouteCameraPresentationStage),
                    $"RouteCameraPresentationSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' completionHandoff='{routeCommand.CompletionHandoff}' reason='{skipReason}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            SessionOperationalRouteCameraPrepareCommand prepareCommand = new(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                routeCommand.SurfaceKind.ToString(),
                routeCommand.CompletionHandoff,
                command.ActiveSceneName,
                routeCommand.SurfacePresentationProfile,
                routeCommand.ActivityPresentationProfile,
                command.Source,
                command.Reason);

            if (!_routeCameraAdapter.TryPrepareRouteCamera(prepareCommand, out var prepareResult, out string prepareReason))
            {
                bool required = routeCommand.SurfacePresentationProfile != null && routeCommand.SurfacePresentationProfile.Required;
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] prepare_failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' required='{required}' reason='{Normalize(prepareReason)}'.");
            }

            if (prepareResult.IsSkipped)
            {
                DebugUtility.LogVerbose(typeof(OperationalRouteCameraPresentationStage),
                    $"RouteCameraPresentationSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' completionHandoff='{routeCommand.CompletionHandoff}' reason='{prepareResult.SkipReason}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (prepareResult.IsPrepared)
            {
                DebugUtility.Log(typeof(OperationalRouteCameraPresentationStage),
                    $"RouteCameraPresentationStagePrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' requirementId='{prepareResult.ReadyFact.RequirementId}' outputCamera='{prepareResult.ReadyFact.OutputCameraName}' presentationRig='{prepareResult.ReadyFact.PresentationRigName}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            string failureReason = prepareResult.FailureFact != null
                ? prepareResult.FailureFact.FailureReason
                : prepareReason;
            bool profileRequired = routeCommand.SurfacePresentationProfile != null && routeCommand.SurfacePresentationProfile.Required;

            DebugUtility.LogError(typeof(OperationalRouteCameraPresentationStage),
                $"RouteCameraPresentationFailed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' profileRequired='{profileRequired}' reason='{Normalize(failureReason)}' source='{command.Source}' reasonDetail='{command.Reason}'.");

            throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] prepare_failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{routeCommand.SurfaceKind}' required='{profileRequired}' reason='{Normalize(failureReason)}'.");
        }

        private static bool ShouldSkipRouteCameraByPolicy(
            OperationalRouteCameraPresentationCommand command,
            out string skipReason)
        {
            var routeCommand = command.RouteCommand;
            var profile = routeCommand.SurfacePresentationProfile;
            var activityProfile = routeCommand.ActivityPresentationProfile;
            bool isSessionActivityEntry =
                routeCommand.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry;

            if (profile == null)
            {
                if (isSessionActivityEntry &&
                    activityProfile != null &&
                    activityProfile.TryValidate(out _))
                {
                    skipReason = "activity_camera_has_priority";
                    return true;
                }

                skipReason = "surface_presentation_profile_missing";
                return true;
            }

            if (profile.RouteCameraPresentationMode == RouteCameraPresentationMode.None)
            {
                skipReason = "surface_camera_presentation_mode_none";
                return true;
            }

            if (isSessionActivityEntry &&
                profile.RouteCameraPresentationMode == RouteCameraPresentationMode.SkipWhenActivityHandoff)
            {
                skipReason = "activity_camera_has_priority";
                return true;
            }

            skipReason = string.Empty;
            return false;
        }

        private static void LogStageStarted(OperationalRouteCameraPresentationCommand command)
        {
            DebugUtility.LogVerbose(typeof(OperationalRouteCameraPresentationStage),
                $"RouteCameraPresentationStageStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activeScene='{Normalize(command.ActiveSceneName)}' operationalSurfaceKind='{command.RouteCommand.SurfaceKind}' completionHandoff='{command.RouteCommand.CompletionHandoff}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
