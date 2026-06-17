using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.CameraPresentation.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class SessionOperationalActivityCameraAdapter : ISessionOperationalActivityCameraAdapter
    {
        private readonly IActivityCameraPreparationExecutor _activityCameraExecutor;
        private readonly ActivityCameraPresentationRequirementResolver _requirementResolver;
        private readonly IActivityCameraAnchorHostResolver _anchorHostResolver;

        private ActivityCameraReadyFact _activeReadyFact;

        public SessionOperationalActivityCameraAdapter(
            IActivityCameraPreparationExecutor activityCameraExecutor,
            ActivityCameraPresentationRequirementResolver requirementResolver,
            IActivityCameraAnchorHostResolver anchorHostResolver)
        {
            this._activityCameraExecutor = activityCameraExecutor;
            this._requirementResolver = requirementResolver;
            this._anchorHostResolver = anchorHostResolver;
        }

        public bool TryPrepareActivityCamera(
            SessionOperationalActivityCameraPrepareCommand command,
            out SessionOperationalActivityCameraPrepareResult result,
            out string reason)
        {
            if (!command.IsValid)
            {
                reason = "activity_camera_prepare_command_invalid";
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                return false;
            }

            var profile = command.ActivityPresentationProfile;

            if (_activityCameraExecutor == null)
            {
                reason = "activity_camera_preparation_executor_missing";
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (_requirementResolver == null)
            {
                reason = "activity_camera_requirement_resolver_missing";
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (!TryResolveAnchorHost(command, out var anchorHost, out reason))
            {
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            if (!_requirementResolver.TryResolve(
                    profile,
                    anchorHost,
                    out var requirement,
                    out reason))
            {
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            var bindingCommand = new ActivityCameraBindingCommand(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.ActivityIdentity,
                requirement,
                command.Source,
                command.Reason);

            DebugUtility.LogVerbose(
                typeof(SessionOperationalActivityCameraAdapter),
                $"ActivityCameraPresentationPrepareStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity}' profileId='{profile.ProfileId}' requirementId='{requirement.RequirementId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!_activityCameraExecutor.TryPrepare(
                    bindingCommand,
                    out var preparationResult,
                    out reason))
            {
                var failureFact = preparationResult?.FailureFact;

                result = SessionOperationalActivityCameraPrepareResult.Failed(failureFact, reason);
                LogFailed(command, failureFact, reason);
                return false;
            }

            var readyFact = preparationResult?.ReadyFact;

            if (readyFact == null)
            {
                reason = "activity_camera_ready_fact_missing";
                result = SessionOperationalActivityCameraPrepareResult.Failed(null, reason);
                LogFailed(command, null, reason);
                return false;
            }

            _activeReadyFact = readyFact;
            result = SessionOperationalActivityCameraPrepareResult.Prepared(readyFact, reason);

            DebugUtility.Log(
                typeof(SessionOperationalActivityCameraAdapter),
                $"ActivityCameraPresentationPrepared routeIdentity='{readyFact.RouteIdentity}' routeOperationId='{readyFact.RouteOperationId}' transitionId='{readyFact.TransitionId}' routeSequence='{readyFact.RouteSequence}' activityIdentity='{readyFact.ActivityIdentity}' requirementId='{readyFact.RequirementId}' outputCamera='{readyFact.Handle?.UnityCamera?.name}' presentationRig='{readyFact.Handle?.CameraRigInstance?.name}' source='{readyFact.Source}' reason='{readyFact.Reason}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        public bool TryReleaseActivityCamera(
            SessionOperationalActivityCameraReleaseCommand command,
            out SessionOperationalActivityCameraReleaseResult result,
            out string reason)
        {
            if (!command.IsValid)
            {
                reason = "activity_camera_release_command_invalid";
                result = SessionOperationalActivityCameraReleaseResult.Failed(null, reason);
                return false;
            }

            if (_activityCameraExecutor == null)
            {
                reason = "activity_camera_preparation_executor_missing";
                result = SessionOperationalActivityCameraReleaseResult.Failed(null, reason);
                return false;
            }

            if (_activeReadyFact == null)
            {
                reason = "no_active_activity_camera_binding";
                result = SessionOperationalActivityCameraReleaseResult.Skipped(reason);

                DebugUtility.LogVerbose(
                    typeof(SessionOperationalActivityCameraAdapter),
                    $"ActivityCameraPresentationReleaseSkipped currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' source='{command.Source}' reason='{command.Reason}' skipReason='{reason}'.",
                    DebugUtility.Colors.Info);

                return true;
            }

            var releaseCommand = new ActivityCameraReleaseCommand(
                _activeReadyFact.RouteIdentity,
                _activeReadyFact.RouteOperationId,
                _activeReadyFact.TransitionId,
                _activeReadyFact.RouteSequence,
                _activeReadyFact.ActivityIdentity,
                command.Source,
                command.Reason);

            DebugUtility.LogVerbose(
                typeof(SessionOperationalActivityCameraAdapter),
                $"ActivityCameraPresentationReleaseStarted currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' releaseRouteIdentity='{releaseCommand.RouteIdentity}' routeOperationId='{releaseCommand.RouteOperationId}' transitionId='{releaseCommand.TransitionId}' routeSequence='{releaseCommand.RouteSequence}' activityIdentity='{releaseCommand.ActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!_activityCameraExecutor.TryRelease(
                    releaseCommand,
                    out var releaseResult,
                    out reason))
            {
                var failureFact = releaseResult?.FailureFact;

                result = SessionOperationalActivityCameraReleaseResult.Failed(failureFact, reason);

                DebugUtility.Log(
                    typeof(SessionOperationalActivityCameraAdapter),
                    $"ActivityCameraPresentationReleaseFailed currentRouteIdentity='{command.CurrentRouteIdentity}' previousRouteIdentity='{command.PreviousRouteIdentity}' releaseRouteIdentity='{releaseCommand.RouteIdentity}' routeOperationId='{releaseCommand.RouteOperationId}' transitionId='{releaseCommand.TransitionId}' routeSequence='{releaseCommand.RouteSequence}' activityIdentity='{releaseCommand.ActivityIdentity}' source='{command.Source}' reason='{command.Reason}' failureReason='{reason}' factFailureReason='{failureFact?.FailureReason}'.",
                    DebugUtility.Colors.Error);

                return false;
            }

            var releasedFact = releaseResult?.ReleasedFact;

            _activeReadyFact = null;
            result = SessionOperationalActivityCameraReleaseResult.Released(releasedFact, reason);

            DebugUtility.Log(
                typeof(SessionOperationalActivityCameraAdapter),
                $"ActivityCameraPresentationReleased routeIdentity='{releasedFact?.RouteIdentity}' routeOperationId='{releasedFact?.RouteOperationId}' transitionId='{releasedFact?.TransitionId}' routeSequence='{releasedFact?.RouteSequence}' activityIdentity='{releasedFact?.ActivityIdentity}' source='{releasedFact?.Source}' reason='{releasedFact?.Reason}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        private bool TryResolveAnchorHost(
            SessionOperationalActivityCameraPrepareCommand command,
            out ActivityCameraAnchorHost anchorHost,
            out string reason)
        {
            anchorHost = null;
            if (_anchorHostResolver == null)
            {
                reason = "activity_camera_anchor_host_resolver_missing";
                return false;
            }

            string sceneName = string.IsNullOrWhiteSpace(command.ActiveSceneName)
                ? string.Empty
                : command.ActiveSceneName;

            if (!_anchorHostResolver.TryResolve(sceneName, out anchorHost, out reason))
            {
                return false;
            }

            return true;
        }

        private static void LogFailed(
            SessionOperationalActivityCameraPrepareCommand command,
            ActivityCameraFailureFact failureFact,
            string reason)
        {
            DebugUtility.Log(
                typeof(SessionOperationalActivityCameraAdapter),
                $"ActivityCameraPresentationFailed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' activityIdentity='{command.ActivityIdentity}' failureReason='{reason}' factFailureReason='{failureFact?.FailureReason}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                DebugUtility.Colors.Error);
        }
    }
}
