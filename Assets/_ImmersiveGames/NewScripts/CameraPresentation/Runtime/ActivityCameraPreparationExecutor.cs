using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class ActivityCameraPreparationExecutor : IActivityCameraPreparationExecutor
    {
        private readonly IActivityCameraDirector _director;
        private ActivityCameraBindingResult _activeBinding;

        public ActivityCameraPreparationExecutor(IActivityCameraDirector director)
        {
            this._director = director;
        }

        public bool TryPrepare(
            ActivityCameraBindingCommand command,
            out ActivityCameraPreparationResult result,
            out string reason)
        {
            if (_director == null)
            {
                reason = "activity_camera_director_missing";

                var failedBinding = ActivityCameraBindingResult.Failed(command, reason);
                var failureFact = ActivityCameraFailureFact.FromResult(
                    failedBinding,
                    nameof(ActivityCameraPreparationExecutor),
                    "activity_camera_prepare_failed");

                result = ActivityCameraPreparationResult.Failed(failureFact, reason);
                return false;
            }

            if (_activeBinding != null)
            {
                reason = "active_camera_binding_already_exists";

                var failedBinding = ActivityCameraBindingResult.Failed(command, reason);
                var failureFact = ActivityCameraFailureFact.FromResult(
                    failedBinding,
                    nameof(ActivityCameraPreparationExecutor),
                    "activity_camera_prepare_failed");

                result = ActivityCameraPreparationResult.Failed(failureFact, reason);
                return false;
            }

            if (!_director.TryPrepareActivityCamera(command, out var bindingResult, out reason))
            {
                var failureFact = ActivityCameraFailureFact.FromResult(
                    bindingResult,
                    nameof(ActivityCameraPreparationExecutor),
                    "activity_camera_prepare_failed");

                result = ActivityCameraPreparationResult.Failed(failureFact, reason);
                return false;
            }

            _activeBinding = bindingResult;

            var readyFact = ActivityCameraReadyFact.FromResult(
                bindingResult,
                nameof(ActivityCameraPreparationExecutor),
                "activity_camera_prepare_ready");

            result = ActivityCameraPreparationResult.Ready(readyFact, reason);
            return true;
        }

        public bool TryRelease(
            ActivityCameraReleaseCommand command,
            out ActivityCameraReleaseResult result,
            out string reason)
        {
            if (_director == null)
            {
                reason = "activity_camera_director_missing";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (!TryValidateReleaseCommand(command, out reason))
            {
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (_activeBinding == null)
            {
                reason = "active_camera_binding_missing";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (_activeBinding.Handle == null)
            {
                reason = "active_camera_binding_handle_missing";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (!MatchesActiveBinding(command, _activeBinding.Handle))
            {
                reason = "foreign_or_stale_camera_release_command";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (!_director.TryReleaseActivityCamera(_activeBinding, out reason))
            {
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            _activeBinding = null;

            var releasedFact = ActivityCameraReleasedFact.FromCommand(
                command,
                nameof(ActivityCameraPreparationExecutor),
                "activity_camera_release_completed");

            result = ActivityCameraReleaseResult.Released(releasedFact, reason);
            return true;
        }

        public bool TryRebindTargets(
            ActivityCameraRebindTargetsCommand command,
            out ActivityCameraRebindTargetsResult result,
            out string reason)
        {
            if (_director == null)
            {
                reason = "activity_camera_director_missing";
                result = ActivityCameraRebindTargetsResult.Failed(command?.ActivityIdentity, reason);
                return false;
            }

            if (command == null)
            {
                reason = "rebind_command_null";
                result = ActivityCameraRebindTargetsResult.Failed(string.Empty, reason);
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.ActivityIdentity))
            {
                reason = "activity_identity_missing";
                result = ActivityCameraRebindTargetsResult.Failed(command.ActivityIdentity, reason);
                return false;
            }

            if (command.TrackingTarget == null)
            {
                reason = "tracking_target_missing";
                result = ActivityCameraRebindTargetsResult.Failed(command.ActivityIdentity, reason);
                return false;
            }

            if (_activeBinding == null || _activeBinding.Handle == null)
            {
                reason = "active_camera_binding_missing";
                result = ActivityCameraRebindTargetsResult.Failed(command.ActivityIdentity, reason);
                return false;
            }

            if (!string.Equals(_activeBinding.Handle.ActivityIdentity, command.ActivityIdentity, System.StringComparison.Ordinal))
            {
                reason = "foreign_or_stale_activity_identity";
                result = ActivityCameraRebindTargetsResult.Failed(command.ActivityIdentity, reason);
                return false;
            }

            if (!_director.TryRebindActivityCameraTargets(_activeBinding.Handle, command, out reason))
            {
                result = ActivityCameraRebindTargetsResult.Failed(command.ActivityIdentity, reason);
                return false;
            }

            result = ActivityCameraRebindTargetsResult.Bound(command.ActivityIdentity, reason);
            return true;
        }

        private static ActivityCameraReleaseResult BuildReleaseFailure(
            ActivityCameraReleaseCommand command,
            string failureReason)
        {
            var failureFact = ActivityCameraReleaseFailureFact.FromCommand(
                command,
                failureReason,
                nameof(ActivityCameraPreparationExecutor),
                "activity_camera_release_failed");

            return ActivityCameraReleaseResult.Failed(
                failureFact,
                failureReason);
        }

        private static bool TryValidateReleaseCommand(
            ActivityCameraReleaseCommand command,
            out string reason)
        {
            if (command == null)
            {
                reason = "release_command_null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.RouteIdentity))
            {
                reason = "route_identity_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.RouteOperationId))
            {
                reason = "route_operation_id_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.TransitionId))
            {
                reason = "transition_id_missing";
                return false;
            }

            if (command.RouteSequence <= 0)
            {
                reason = "route_sequence_invalid";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.ActivityIdentity))
            {
                reason = "activity_identity_missing";
                return false;
            }

            reason = "valid";
            return true;
        }

        private static bool MatchesActiveBinding(
            ActivityCameraReleaseCommand command,
            ActivityCameraBindingHandle handle)
        {
            return handle.RouteIdentity == command.RouteIdentity
                   && handle.RouteOperationId == command.RouteOperationId
                   && handle.TransitionId == command.TransitionId
                   && handle.RouteSequence == command.RouteSequence
                   && handle.ActivityIdentity == command.ActivityIdentity;
        }
    }
}
