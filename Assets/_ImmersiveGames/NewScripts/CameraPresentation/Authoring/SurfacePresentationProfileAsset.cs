using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Authoring
{
    [CreateAssetMenu(
        fileName = "SurfacePresentationProfile",
        menuName = "ImmersiveGames/Camera Presentation/Surface Presentation Profile")]
    public sealed class SurfacePresentationProfileAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string profileId = "surface.presentation.default";

        [Header("Route Camera Presentation")]
        [SerializeField] private RouteCameraPresentationMode routeCameraPresentationMode =
            RouteCameraPresentationMode.None;

        [SerializeField] private RouteCameraActivationTiming activationTiming =
            RouteCameraActivationTiming.BeforeReveal;

        [SerializeField] private GameObject presentationRigPrefab;

        [Header("Anchors")]
        [SerializeField] private string trackingAnchorId = "surface.camera.tracking";
        [SerializeField] private string lookAtAnchorId = "surface.camera.lookAt";

        [Header("Cinemachine")]
        [SerializeField] private int priority = 20;

        [Header("Policy")]
        [SerializeField] private bool required = true;

        public string ProfileId => profileId;

        public RouteCameraPresentationMode RouteCameraPresentationMode => routeCameraPresentationMode;

        public RouteCameraActivationTiming ActivationTiming => activationTiming;

        public GameObject PresentationRigPrefab => presentationRigPrefab;

        public string TrackingAnchorId => trackingAnchorId;

        public string LookAtAnchorId => lookAtAnchorId;

        public int Priority => priority;

        public bool Required => required;

        public bool TryValidate(
            out string reason)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                reason = "surface_presentation_profile_id_missing";
                return false;
            }

            if (routeCameraPresentationMode == RouteCameraPresentationMode.None)
            {
                reason = "surface_presentation_profile_valid_no_camera";
                return true;
            }

            if (activationTiming != RouteCameraActivationTiming.BeforeReveal)
            {
                reason = "surface_presentation_activation_timing_unsupported";
                return false;
            }

            if (presentationRigPrefab == null)
            {
                reason = "surface_presentation_rig_prefab_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(trackingAnchorId))
            {
                reason = "surface_presentation_tracking_anchor_id_missing";
                return false;
            }

            if (priority < 0)
            {
                reason = "surface_presentation_priority_invalid";
                return false;
            }

            reason = "surface_presentation_profile_valid";
            return true;
        }
    }
}
