using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Authoring
{
    [CreateAssetMenu(
        fileName = "ActivityPresentationProfile",
        menuName = "ImmersiveGames/Camera Presentation/Activity Presentation Profile")]
    public sealed class ActivityPresentationProfileAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string profileId = "activity.presentation.default";

        [Header("Activity Camera Presentation")]
        [SerializeField] private GameObject cameraRigPrefab;

        [SerializeField] private ActivityCameraActivationTiming activationTiming =
            ActivityCameraActivationTiming.BeforeReveal;

        [Header("Anchors")]
        [SerializeField] private string trackingAnchorId = "activity.camera.tracking";
        [SerializeField] private string lookAtAnchorId = "activity.camera.lookAt";

        [Header("Cinemachine")]
        [SerializeField] private int priority = 100;

        [Header("Policy")]
        [SerializeField] private bool required = true;

        public string ProfileId => profileId;

        public GameObject CameraRigPrefab => cameraRigPrefab;

        public ActivityCameraActivationTiming ActivationTiming => activationTiming;

        public string TrackingAnchorId => trackingAnchorId;

        public string LookAtAnchorId => lookAtAnchorId;

        public int Priority => priority;

        public bool Required => required;

        public bool TryValidate(
            out string reason)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                reason = "activity_presentation_profile_id_missing";
                return false;
            }

            if (activationTiming != ActivityCameraActivationTiming.BeforeReveal)
            {
                reason = "activity_presentation_activation_timing_unsupported";
                return false;
            }

            if (priority < 0)
            {
                reason = "activity_presentation_priority_invalid";
                return false;
            }

            if (cameraRigPrefab == null)
            {
                if (required)
                {
                    reason = "activity_presentation_camera_rig_prefab_missing";
                    return false;
                }

                // Profile opcional sem rig: válido como declaração explícita de ausência.
                reason = "activity_presentation_profile_valid_optional_no_camera";
                return true;
            }

            if (string.IsNullOrWhiteSpace(trackingAnchorId))
            {
                reason = "activity_presentation_tracking_anchor_id_missing";
                return false;
            }

            reason = "activity_presentation_profile_valid";
            return true;
        }
    }
}
