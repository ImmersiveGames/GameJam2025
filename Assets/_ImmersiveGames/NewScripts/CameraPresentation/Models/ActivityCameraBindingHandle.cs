using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraBindingHandle
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public string RequirementId { get; }
        public Camera UnityCamera { get; }
        public GameObject CameraRigInstance { get; }

        public ActivityCameraBindingHandle(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            string requirementId,
            Camera unityCamera,
            GameObject cameraRigInstance)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            ActivityIdentity = activityIdentity;
            RequirementId = requirementId;
            UnityCamera = unityCamera;
            CameraRigInstance = cameraRigInstance;
        }

        public bool Matches(ActivityCameraBindingCommand command)
        {
            if (command == null)
            {
                return false;
            }

            return RouteIdentity == command.RouteIdentity
                && RouteOperationId == command.RouteOperationId
                && TransitionId == command.TransitionId
                && RouteSequence == command.RouteSequence
                && ActivityIdentity == command.ActivityIdentity;
        }
    }
}
