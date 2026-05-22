using UnityEngine;
namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraRequirement
    {
        public string RequirementId { get; }
        public GameObject CameraRigPrefab { get; }
        public Transform TrackingTarget { get; }
        public Transform LookAtTarget { get; }
        public ActivityCameraActivationTiming ActivationTiming { get; }

        public ActivityCameraRequirement(
            string requirementId,
            GameObject cameraRigPrefab,
            Transform trackingTarget,
            Transform lookAtTarget,
            ActivityCameraActivationTiming activationTiming)
        {
            RequirementId = requirementId;
            CameraRigPrefab = cameraRigPrefab;
            TrackingTarget = trackingTarget;
            LookAtTarget = lookAtTarget;
            ActivationTiming = activationTiming;
        }
    }
}
