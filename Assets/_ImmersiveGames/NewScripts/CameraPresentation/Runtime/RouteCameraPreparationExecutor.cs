using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class RouteCameraPreparationExecutor : IRouteCameraPreparationExecutor
    {
        private readonly IRouteCameraDirector director;
        private readonly RouteCameraPresentationCommandValidator validator;

        private RouteCameraBindingResult activeBinding;

        public RouteCameraPreparationExecutor(
            IRouteCameraDirector director,
            RouteCameraPresentationCommandValidator validator)
        {
            this.director = director;
            this.validator = validator;
        }

        public bool TryPrepare(
            RouteCameraPresentationCommand command,
            out RouteCameraPresentationResult result,
            out string reason)
        {
            if (validator == null)
            {
                reason = "route_camera_validator_missing";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (!validator.TryValidatePrepareCommand(command, out string validationReason))
            {
                reason = validationReason;
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (director == null)
            {
                reason = "route_camera_director_missing";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (!director.TryPrepareRouteCamera(
                    command,
                    out RouteCameraBindingResult bindingResult,
                    out string directorReason))
            {
                reason = directorReason;
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (bindingResult == null)
            {
                reason = "route_camera_binding_result_null";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (!bindingResult.Success)
            {
                reason = bindingResult.Reason;
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (bindingResult.Handle == null)
            {
                reason = "route_camera_binding_handle_null";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            activeBinding = bindingResult;

            RouteCameraReadyFact readyFact = RouteCameraReadyFact.FromResult(
                bindingResult,
                nameof(RouteCameraPreparationExecutor),
                "route_camera_prepare_ready");

            reason = "route_camera_ready";
            result = RouteCameraPresentationResult.Ready(
                readyFact,
                reason);

            return true;
        }

        public bool TryRelease(
            RouteCameraReleaseCommand command,
            out RouteCameraReleaseResult result,
            out string reason)
        {
            if (validator == null)
            {
                reason = "route_camera_validator_missing";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (!validator.TryValidateReleaseCommand(command, out string validationReason))
            {
                reason = validationReason;
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (director == null)
            {
                reason = "route_camera_director_missing";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (activeBinding == null || activeBinding.Handle == null)
            {
                reason = "no_active_route_camera_binding";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (!MatchesActiveBinding(command, activeBinding.Handle))
            {
                reason = "foreign_or_stale_route_camera_release_command";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (!director.TryReleaseRouteCamera(
                    activeBinding,
                    out string directorReason))
            {
                reason = directorReason;
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            activeBinding = null;

            RouteCameraReleasedFact releasedFact = RouteCameraReleasedFact.FromCommand(
                command,
                nameof(RouteCameraPreparationExecutor),
                "route_camera_release_completed");

            reason = "route_camera_released";
            result = RouteCameraReleaseResult.Released(
                releasedFact,
                reason);

            return true;
        }

        private static bool MatchesActiveBinding(
            RouteCameraReleaseCommand command,
            RouteCameraBindingHandle handle)
        {
            if (command == null || handle == null)
            {
                return false;
            }

            return command.RouteIdentity == handle.RouteIdentity
                && command.RouteOperationId == handle.RouteOperationId
                && command.TransitionId == handle.TransitionId
                && command.RouteSequence == handle.RouteSequence
                && command.SurfaceKind == handle.SurfaceKind
                && command.RequirementId == handle.RequirementId;
        }

        private static RouteCameraPresentationResult BuildPrepareFailure(
            RouteCameraPresentationCommand command,
            string failureReason)
        {
            RouteCameraBindingResult bindingResult = RouteCameraBindingResult.Failed(
                command,
                failureReason);

            RouteCameraFailureFact failureFact = RouteCameraFailureFact.FromResult(
                bindingResult,
                failureReason,
                nameof(RouteCameraPreparationExecutor),
                "route_camera_prepare_failed");

            return RouteCameraPresentationResult.Failed(
                failureFact,
                "route_camera_prepare_failed");
        }

        private static RouteCameraReleaseResult BuildReleaseFailure(
            RouteCameraReleaseCommand command,
            string failureReason)
        {
            RouteCameraReleaseFailureFact failureFact = RouteCameraReleaseFailureFact.FromCommand(
                command,
                failureReason,
                nameof(RouteCameraPreparationExecutor),
                "route_camera_release_failed");

            return RouteCameraReleaseResult.Failed(
                failureFact,
                "route_camera_release_failed");
        }
    }
}
