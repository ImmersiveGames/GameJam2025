using _ImmersiveGames.NewScripts.CameraPresentation.Models;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public static class ActivityCameraBindingCommandValidator
    {
        public static bool TryValidate(
            ActivityCameraBindingCommand command,
            out string reason)
        {
            if (command == null)
            {
                reason = "command_null";
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

            if (command.Requirement == null)
            {
                reason = "requirement_null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.Requirement.RequirementId))
            {
                reason = "requirement_id_missing";
                return false;
            }

            if (command.Requirement.CameraRigPrefab == null)
            {
                reason = "camera_rig_prefab_missing";
                return false;
            }

            if (command.Requirement.TrackingTarget == null)
            {
                reason = "tracking_target_missing";
                return false;
            }

            if (command.Requirement.ActivationTiming != ActivityCameraActivationTiming.BeforeReveal)
            {
                reason = "activation_timing_unsupported";
                return false;
            }

            reason = "valid";
            return true;
        }
    }
}
