using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraBindingHandle
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string SurfaceKind { get; }
        public string RequirementId { get; }
        public Camera UnityCamera { get; }
        public GameObject PresentationRigInstance { get; }

        public RouteCameraBindingHandle(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string surfaceKind,
            string requirementId,
            Camera unityCamera,
            GameObject presentationRigInstance)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            SurfaceKind = surfaceKind;
            RequirementId = requirementId;
            UnityCamera = unityCamera;
            PresentationRigInstance = presentationRigInstance;
        }
    }
}
