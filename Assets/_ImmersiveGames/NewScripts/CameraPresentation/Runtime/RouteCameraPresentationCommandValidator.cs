using _ImmersiveGames.NewScripts.CameraPresentation.Models;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class RouteCameraPresentationCommandValidator
    {
        public bool TryValidatePrepareCommand(
            RouteCameraPresentationCommand command,
            out string reason)
        {
            if (command == null)
            {
                reason = "route_camera_command_null";
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

            if (string.IsNullOrWhiteSpace(command.SurfaceKind))
            {
                reason = "surface_kind_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.Source))
            {
                reason = "source_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                reason = "reason_missing";
                return false;
            }

            var requirement = command.Requirement;

            if (requirement == null)
            {
                reason = "route_camera_requirement_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(requirement.RequirementId))
            {
                reason = "route_camera_requirement_id_missing";
                return false;
            }

            if (requirement.PresentationRigPrefab == null)
            {
                reason = "route_camera_presentation_rig_prefab_missing";
                return false;
            }

            if (requirement.TrackingTarget == null)
            {
                reason = "route_camera_tracking_target_missing";
                return false;
            }

            if (requirement.ActivationTiming != RouteCameraActivationTiming.BeforeReveal)
            {
                reason = "route_camera_activation_timing_unsupported";
                return false;
            }

            reason = "route_camera_prepare_command_valid";
            return true;
        }

        public bool TryValidateReleaseCommand(
            RouteCameraReleaseCommand command,
            out string reason)
        {
            if (command == null)
            {
                reason = "route_camera_release_command_null";
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

            if (string.IsNullOrWhiteSpace(command.SurfaceKind))
            {
                reason = "surface_kind_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.RequirementId))
            {
                reason = "route_camera_requirement_id_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.Source))
            {
                reason = "source_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                reason = "reason_missing";
                return false;
            }

            reason = "route_camera_release_command_valid";
            return true;
        }
    }
}
