using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class RouteCameraPreparationExecutor : IRouteCameraPreparationExecutor
    {
        private readonly IRouteCameraDirector _director;

        private RouteCameraBindingResult _activeBinding;

        public RouteCameraPreparationExecutor(IRouteCameraDirector director)
        {
            _director = director;
        }

        public bool TryPrepare(
            RouteCameraPresentationCommand command,
            out RouteCameraPresentationResult result,
            out string reason)
        {
            if (command == null)
            {
                reason = "route_camera_command_null";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (command.Requirement == null)
            {
                reason = "route_camera_requirement_missing";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (command.Requirement.PresentationRigPrefab == null)
            {
                reason = "route_camera_presentation_rig_prefab_missing";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (command.Requirement.TrackingTarget == null)
            {
                reason = "route_camera_tracking_target_missing";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (command.Requirement.ActivationTiming != RouteCameraActivationTiming.BeforeReveal)
            {
                reason = "route_camera_activation_timing_unsupported";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (_director == null)
            {
                reason = "route_camera_director_missing";
                result = BuildPrepareFailure(command, reason);
                return false;
            }

            if (!_director.TryPrepareRouteCamera(
                command,
                out var bindingResult,
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

            _activeBinding = bindingResult;

            var readyFact = RouteCameraReadyFact.FromResult(
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
            if (command == null)
            {
                reason = "route_camera_release_command_null";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (_director == null)
            {
                reason = "route_camera_director_missing";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (_activeBinding == null || _activeBinding.Handle == null)
            {
                reason = "no_active_route_camera_binding";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (!MatchesActiveBinding(command, _activeBinding.Handle))
            {
                reason = "foreign_or_stale_route_camera_release_command";
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            if (!_director.TryReleaseRouteCamera(
                _activeBinding,
                out string directorReason))
            {
                reason = directorReason;
                result = BuildReleaseFailure(command, reason);
                return false;
            }

            _activeBinding = null;

            var releasedFact = RouteCameraReleasedFact.FromCommand(
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
            var bindingResult = RouteCameraBindingResult.Failed(
                command,
                failureReason);

            var failureFact = RouteCameraFailureFact.FromResult(
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
            var failureFact = RouteCameraReleaseFailureFact.FromCommand(
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
