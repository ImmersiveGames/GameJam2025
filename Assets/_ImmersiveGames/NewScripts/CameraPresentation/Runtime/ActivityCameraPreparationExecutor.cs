using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class ActivityCameraPreparationExecutor : IActivityCameraPreparationExecutor
    {
        private readonly IActivityCameraDirector director;
        private ActivityCameraBindingResult activeBinding;

        public ActivityCameraPreparationExecutor(IActivityCameraDirector director)
        {
            this.director = director;
        }

        public bool TryPrepare(
            ActivityCameraBindingCommand command,
            out ActivityCameraPreparationResult result,
            out string reason)
        {
            if (director == null)
            {
                reason = "activity_camera_director_missing";

                ActivityCameraBindingResult failedBinding = ActivityCameraBindingResult.Failed(command, reason);
                ActivityCameraFailureFact failureFact = ActivityCameraFailureFact.FromResult(
                    failedBinding,
                    nameof(ActivityCameraPreparationExecutor),
                    "activity_camera_prepare_failed");

                result = ActivityCameraPreparationResult.Failed(failureFact, reason);
                return false;
            }

            if (activeBinding != null)
            {
                reason = "active_camera_binding_already_exists";

                ActivityCameraBindingResult failedBinding = ActivityCameraBindingResult.Failed(command, reason);
                ActivityCameraFailureFact failureFact = ActivityCameraFailureFact.FromResult(
                    failedBinding,
                    nameof(ActivityCameraPreparationExecutor),
                    "activity_camera_prepare_failed");

                result = ActivityCameraPreparationResult.Failed(failureFact, reason);
                return false;
            }

            if (!director.TryPrepareActivityCamera(command, out ActivityCameraBindingResult bindingResult, out reason))
            {
                ActivityCameraFailureFact failureFact = ActivityCameraFailureFact.FromResult(
                    bindingResult,
                    nameof(ActivityCameraPreparationExecutor),
                    "activity_camera_prepare_failed");

                result = ActivityCameraPreparationResult.Failed(failureFact, reason);
                return false;
            }

            activeBinding = bindingResult;

            ActivityCameraReadyFact readyFact = ActivityCameraReadyFact.FromResult(
                bindingResult,
                nameof(ActivityCameraPreparationExecutor),
                "activity_camera_prepare_ready");

            result = ActivityCameraPreparationResult.Ready(readyFact, reason);
            return true;
        }

        public bool TryRelease(
            ActivityCameraReleaseCommand command,
            out string reason)
        {
            if (director == null)
            {
                reason = "activity_camera_director_missing";
                return false;
            }

            if (!TryValidateReleaseCommand(command, out reason))
            {
                return false;
            }

            if (activeBinding == null)
            {
                reason = "active_camera_binding_missing";
                return false;
            }

            if (activeBinding.Handle == null)
            {
                reason = "active_camera_binding_handle_missing";
                return false;
            }

            if (!MatchesActiveBinding(command, activeBinding.Handle))
            {
                reason = "foreign_or_stale_camera_release_command";
                return false;
            }

            if (!director.TryReleaseActivityCamera(activeBinding, out reason))
            {
                return false;
            }

            activeBinding = null;
            return true;
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
