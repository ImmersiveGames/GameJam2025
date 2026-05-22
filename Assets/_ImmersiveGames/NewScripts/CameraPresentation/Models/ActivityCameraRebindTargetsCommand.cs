using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraRebindTargetsCommand
    {
        public ActivityCameraRebindTargetsCommand(
            string activityIdentity,
            Transform trackingTarget,
            Transform lookAtTarget,
            string source,
            string reason)
        {
            ActivityIdentity = activityIdentity;
            TrackingTarget = trackingTarget;
            LookAtTarget = lookAtTarget;
            Source = source;
            Reason = reason;
        }

        public string ActivityIdentity { get; }
        public Transform TrackingTarget { get; }
        public Transform LookAtTarget { get; }
        public string Source { get; }
        public string Reason { get; }
    }
}
