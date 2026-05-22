using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.CameraPresentation.Runtime
{
    public sealed class ActivityCameraPresentationRequirementResolver
    {
        public bool TryResolve(
            ActivityPresentationProfileAsset profile,
            ActivityCameraAnchorHost anchorHost,
            out ActivityCameraRequirement requirement,
            out string reason)
        {
            requirement = null;

            if (profile == null)
            {
                reason = "activity_presentation_profile_missing";
                return false;
            }

            if (!profile.TryValidate(out string profileReason))
            {
                reason = profileReason;
                return false;
            }

            if (profile.CameraRigPrefab == null)
            {
                reason = "activity_presentation_camera_disabled";
                return false;
            }

            if (anchorHost == null)
            {
                reason = "activity_camera_anchor_host_missing";
                return false;
            }

            if (!anchorHost.TryValidate(out string anchorHostReason))
            {
                reason = anchorHostReason;
                return false;
            }

            if (!anchorHost.TryResolve(
                    profile.TrackingAnchorId,
                    out Transform trackingTarget,
                    out string trackingReason))
            {
                reason = trackingReason;
                return false;
            }

            Transform lookAtTarget = null;

            if (!string.IsNullOrWhiteSpace(profile.LookAtAnchorId))
            {
                if (!anchorHost.TryResolve(
                        profile.LookAtAnchorId,
                        out lookAtTarget,
                        out string lookAtReason))
                {
                    reason = lookAtReason;
                    return false;
                }
            }

            requirement = new ActivityCameraRequirement(
                BuildRequirementId(profile),
                profile.CameraRigPrefab,
                trackingTarget,
                lookAtTarget,
                profile.ActivationTiming);

            reason = "activity_camera_presentation_requirement_resolved";
            return true;
        }

        private static string BuildRequirementId(
            ActivityPresentationProfileAsset profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.ProfileId))
            {
                return "activity.presentation.camera";
            }

            return $"{profile.ProfileId}.camera";
        }
    }
}
