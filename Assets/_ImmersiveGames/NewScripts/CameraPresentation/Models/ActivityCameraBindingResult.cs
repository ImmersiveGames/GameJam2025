using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraBindingResult
    {
        public bool Success { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public string RequirementId { get; }
        public ActivityCameraBindingHandle Handle { get; }
        public Camera UnityCamera => Handle?.UnityCamera;
        public GameObject CameraRigInstance => Handle?.CameraRigInstance;
        public string Reason { get; }

        private ActivityCameraBindingResult(
            bool success,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            string requirementId,
            ActivityCameraBindingHandle handle,
            string reason)
        {
            Success = success;
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            ActivityIdentity = activityIdentity;
            RequirementId = requirementId;
            Handle = handle;
            Reason = reason;
        }

        public static ActivityCameraBindingResult Ready(
            ActivityCameraBindingCommand command,
            Camera unityCamera,
            GameObject cameraRigInstance,
            string reason)
        {
            ActivityCameraBindingHandle handle = new ActivityCameraBindingHandle(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.ActivityIdentity,
                command.Requirement.RequirementId,
                unityCamera,
                cameraRigInstance);

            return new ActivityCameraBindingResult(
                true,
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.ActivityIdentity,
                command.Requirement.RequirementId,
                handle,
                reason);
        }

        public static ActivityCameraBindingResult Failed(
            ActivityCameraBindingCommand command,
            string reason)
        {
            return new ActivityCameraBindingResult(
                false,
                command != null ? command.RouteIdentity : string.Empty,
                command != null ? command.RouteOperationId : string.Empty,
                command != null ? command.TransitionId : string.Empty,
                command?.RouteSequence ?? 0,
                command != null ? command.ActivityIdentity : string.Empty,
                command is { Requirement: not null } ? command.Requirement.RequirementId : string.Empty,
                null,
                reason);
        }
    }
}
