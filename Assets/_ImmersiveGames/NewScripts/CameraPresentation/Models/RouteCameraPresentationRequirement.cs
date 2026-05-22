using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraPresentationRequirement
    {
        public string RequirementId { get; }
        public GameObject PresentationRigPrefab { get; }
        public Transform TrackingTarget { get; }
        public Transform LookAtTarget { get; }
        public RouteCameraActivationTiming ActivationTiming { get; }
        public int Priority { get; }
        public bool Required { get; }

        public RouteCameraPresentationRequirement(
            string requirementId,
            GameObject presentationRigPrefab,
            Transform trackingTarget,
            Transform lookAtTarget,
            RouteCameraActivationTiming activationTiming,
            int priority,
            bool required)
        {
            RequirementId = requirementId;
            PresentationRigPrefab = presentationRigPrefab;
            TrackingTarget = trackingTarget;
            LookAtTarget = lookAtTarget;
            ActivationTiming = activationTiming;
            Priority = priority;
            Required = required;
        }
    }
}
